# Multi-Tenant Design

Sistem ilerde multi-tenant olacak; her yeni geliştirmede bu varsayımla tasarla.

## Claims Akışı

1. Login -> Cookie auth claims içine `TenantId` yazılır
2. `TenantResolutionMiddleware` (Infrastructure/Middleware/) -> `User.FindFirst("TenantId")` okuyup `ITenantContext.Initialize(tenant)` çağırır
3. `TenantActionFilter` (Infrastructure/Filters/) -> defense-in-depth, middleware atlanırsa filter set eder
4. Business katmanı `ITenantContext.CurrentTenantId` üzerinden filtre yapar

## ITenantContext

```csharp
public interface ITenantContext
{
    bool IsInitialized { get; }
    int CurrentTenantId { get; }
    Tenant CurrentTenant { get; }
    void Initialize(Tenant tenant);
}
```

Scoped lifetime — request başına bir instance.

## Singleton State Kuralı

❌ YANLIŞ:
```csharp
public class TrendyolTokenCache
{
    private string? _token;
    private DateTime _expiresAt;
    private readonly SemaphoreSlim _lock = new(1, 1);
}
```

Tüm tenant'lar aynı token'ı paylaşır, lock kavgası global.

✅ DOĞRU:
```csharp
public class TrendyolTokenCache
{
    private readonly ConcurrentDictionary<int, (string Token, DateTime ExpiresAt)> _cache = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public async Task<string> GetTokenAsync(int tenantId, Func<Task<string>> factory)
    {
        if (_cache.TryGetValue(tenantId, out var c) && c.ExpiresAt > DateTime.UtcNow)
            return c.Token;

        var sem = _locks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            if (_cache.TryGetValue(tenantId, out c) && c.ExpiresAt > DateTime.UtcNow)
                return c.Token;
            var t = await factory();
            _cache[tenantId] = (t, DateTime.UtcNow.AddMinutes(50));
            return t;
        }
        finally { sem.Release(); }
    }

    public void Invalidate(int tenantId) => _cache.TryRemove(tenantId, out _);
}
```

Her tenant kendi token'ı, kendi SemaphoreSlim'i — paralelizm korunur.

## DB Query Tenant Filtresi

DbContext'te global query filter:
```csharp
modelBuilder.Entity<Product>()
    .HasQueryFilter(p => p.TenantId == tenantContext.CurrentTenantId && !p.IsDeleted);
```

Tenant cross-cutting concern olduğu için `ITenantContext` `DbContext` constructor'a inject edilir. Tenant değişmez request başına -> guvenli.

Cross-tenant query lazımsa `.IgnoreQueryFilters()` — sadece admin işlemlerinde.

## Configuration

❌ `appsettings.json`'da hardcoded `TrendyolApiKey`
✅ `Tenant.Settings` JSON column veya `TenantSetting` entity -> DB'den oku

```csharp
var setting = await dbContext.TenantSettings
    .Where(s => s.TenantId == tenantContext.CurrentTenantId && s.Key == "TrendyolApiKey")
    .Select(s => s.Value)
    .FirstOrDefaultAsync();
```

## BackgroundService'lerde Tenant

`BackgroundService` `IHostedService`'den türer; HTTP context yok, scope manuel açılır:

```csharp
public class ProductPublishBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var evt in eventChannel.Reader.ReadAllAsync(ct))
        {
            using var scope = scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
            var tenant = await tenantRegistry.GetByIdAsync(evt.TenantId);
            tenantContext.Initialize(tenant!);

            var handler = scope.ServiceProvider.GetRequiredService<IProductPublishHandler>();
            await handler.HandleAsync(evt, ct);
        }
    }
}
```

Event payload'ında `TenantId` zorunlu — yoksa tenant context kurulamaz.

## "Bu N tenant ile çalışır mı?" Soruları

- Tek field yerine `ConcurrentDictionary<int, T>` mu?
- SemaphoreSlim per-tenant mı?
- Cache key'inde `TenantId` var mı?
- DB query'sinde tenant filter var mı? (HasQueryFilter veya manuel)
- Hardcoded config yerine DB'den mi okuyor?
- BackgroundService scope tenant context kuruyor mu?
- API endpoint'i route'unda veya header'da tenant taşıyor mu?

Hepsi "evet" değilse design bug var.
