# Bildirim Sistemi — Tasarım Spesifikasyonu

**Tarih:** 2026-04-20
**Yazar:** Baturhan Kahraman (+ Claude)
**Durum:** Brainstorming tamamlandı, implementasyon plan aşamasına hazır
**Hedef sürüm:** Entegrasyon v(sonraki)

---

## 1. Amaç ve Kapsam

Mevcut bildirim altyapısı (Notification entity, SignalR hub, bir adet sender) üç kritik eksikliği barındırıyor:

1. **Event yayın kapsamı çok dar.** `ProductAddedEvent`, `ProductUpdatedEvent`, `CategoryUpdatedEvent` gibi bir avuç event dışında domain olayları publish edilmiyor; CRUD'ların büyük kısmı hiçbir event üretmiyor. Marketplace inbound (sipariş, red, soru) ve storefront eventleri yok.
2. **Real-time teslim kanalı SignalR ile thread baskısı yaratıyor.** `NotificationHub` çoğu senaryoda boş-loop; SignalR'ın hub invocation overhead'i + keep-alive + (ileride) Redis backplane gereksinimi, sadece tek-yönlü bildirim için abartı.
3. **Multi-device okundu durumu yok.** `Notification.IsRead` global tutulduğu için bir kullanıcının birden çok alıcılı bildirimde "okundu" yapması diğer alıcıları da etkiler. Aynı kullanıcı iki sekmede aynı bildirimi iki kere okur.

Bu spec, üç ihtiyacı tek bir tutarlı mimari altında çözer:

- **Domain event yayın katmanı** — her CRUD/iş olayı standart bir `IEventBus`'a publish edilir, 0-N handler subscribe olur.
- **Kriter-bazlı Outbox pattern** — kritik event'ler DB outbox tablosuna aynı transaction'da yazılır; `IHostedService` + PostgreSQL `LISTEN/NOTIFY` ile crash-safe dispatch edilir.
- **.NET 10 native Server-Sent Events** — `NotificationHub` kaldırılır, tek yönlü bildirim akışı SSE endpoint'inden gider; SignalR sadece `ChatHub` için kalır.
- **Admin tarafı Web Push** — VAPID tabanlı, storefront'unkinden bağımsız ayrı entity ve servis; admin'in tarayıcısı kapalıyken OS-level bildirim.
- **Multi-device read sync** — `NotificationsUsers` junction'a `IsRead/ReadAt` taşınır, bir cihazda `MarkAsRead` → diğer cihazlara SSE üzerinden `NotificationReadEvent` broadcast.
- **Kullanılabilir UI** — Bildirimler sayfası + zil dropdown + anlık badge; `ActionUrl` ile tıklayınca ilgili sayfaya yönlendirme.

### Non-Goals

- Kafka / RabbitMQ / NATS gibi external message broker'lara geçiş (gelecek mimari kararı; Faz 3).
- Mobile push (APNs/FCM) implementasyonu (sadece genişletilebilirlik yüzeyi hazırlanır).
- Slack/Teams entegrasyonu (aynı şekilde yüzey hazırlanır, implement edilmez).
- Bildirim tercih paneli re-tasarımı (mevcut `NotificationSetting` minimal genişleme).
- Storefront Web Push değişikliği (ayrı bir sistemdir; dokunulmaz).

---

## 2. Mimari İlkeler

1. **Modular monolith pusulası.** Tek deploy edilebilir süreç ama internal modüller interface'lerle ayrı. Her yeni soyutlama ileride bir modülü microservice'e çıkarmayı kolaylaştırmalı.
2. **Sorumluluk kaydırılmaz.** `NotificationManager`, `INotificationSender`, `IHubContext` gibi mevcut bileşenlerin public API'si değişmez; sadece altındaki transport değişir. (Bkz. memory: [SignalR→SSE Migration Rules](../../../../.claude/projects/-home-baturhan-Projeler-Entegrasyon/memory/feedback_no_responsibility_change_no_blocking.md).)
3. **Non-blocking her yerde.** `.Result`, `.Wait()`, `Task.Run(() => ...)` kullanımı yasak. Tüm dispatch IHostedService + `Channel<T>` ile async pipe'tir. ThreadPool ASP.NET Core request'leri için açık kalır.
4. **Durability ayrı, delivery ayrı.** Outbox "event'i kaydettim" demektir; delivery (SSE/WebPush/Email/SMS) bunun sonrasında best-effort paralel yapılır, herhangi birinin hatası diğerini düşürmez.
5. **Pub-sub, tek-handler değil.** Bir event N handler'a gidebilir. Yeni handler eklemek publisher'ı değiştirmez. Handler isimlendirme: `{Event}{Concern}Handler` (örn. `ProductAddedNotificationHandler`, `ProductAddedSearchIndexHandler`).
6. **Multi-tenant güvenli.** Her event ve bildirim `TenantId` taşır. Singleton state (connection registry) `ConcurrentDictionary<int, ...>` tenant-başına izole olmalı.
7. **Test edilebilir.** `IEventBus`, `IOutboxStore`, `INotificationSender` mock'lanabilir. Her handler ayrı test sınıfına sahip. Transport değişiklikleri önce-sonra test ile doğrulanır.
8. **Faz sıralaması.** Mimari bugün basit, yarın büyümeye açık. `IEventBus`'ın implementasyonu Faz 1'de in-memory, Faz 2'de PostgreSQL NOTIFY, Faz 3'te Redis Streams olabilir; iş kodu değişmez.

---

## 3. Yüksek Seviyeli Mimari

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  PUBLISHER (Business Manager)                                                │
│  ────────────────────────────                                                │
│  ProductManager.AddProductAsync():                                           │
│    dbContext.Products.Add(product);                                          │
│    await _eventBus.PublishAsync(new ProductAddedEvent(...), tx);             │
│    await dbContext.SaveChangesAsync();   // tx commit = outbox commit        │
└──────────────────────────┬───────────────────────────────────────────────────┘
                           │
              ┌────────────▼─────────────┐
              │  IEventBus (Faz 1 impl)  │
              │  InMemoryEventBus        │
              │  — NotificationOutbox'a  │
              │    aynı tx'te INSERT     │
              │  — in-memory Channel'a   │
              │    da WriteAsync         │
              └────────────┬─────────────┘
                           │
         ┌─────────────────┴──────────────────┐
         │ (hızlı yol — ölümlü)               │ (güvenli yol — crash-safe)
┌────────▼─────────────────────┐    ┌─────────▼────────────────────────┐
│ InProcessEventDispatcher     │    │ OutboxDispatcher                 │
│ (IHostedService)             │    │ (IHostedService)                 │
│ — Channel<DomainEvent>       │    │ — LISTEN notification_outbox_new │
│ — DI'dan IDomainEventHandler │    │ — SELECT FOR UPDATE SKIP LOCKED  │
│   <T> tüm implementasyonları │    │ — retry + exp. backoff           │
│   paralel çağırır            │    │ — dead-letter tablosu            │
│ — bounded, DropOldest        │    │ — idempotent dispatch            │
└────────┬─────────────────────┘    └──────────────────────────────────┘
         │                                     │
         └──────────────────┬──────────────────┘
                            │
         ┌──────────────────▼──────────────────┐
         │  IDomainEventHandler<TEvent>        │
         │  — {Event}{Concern}Handler pattern  │
         │  — Recipient çözümle (permission)   │
         │  — Template üret (title/body/url)   │
         │  — INotificationManager çağır       │
         └──────────────────┬──────────────────┘
                            │
         ┌──────────────────▼──────────────────┐
         │  INotificationManager               │
         │  — DB: Notification + Users junction│
         │  — TÜM INotificationSender paralel  │
         │  — Her sender try-catch izole       │
         └──────────────────┬──────────────────┘
                            │
     ┌──────────────────────┼───────────────────────┬────────────────┐
     │                      │                       │                │
┌────▼──────┐        ┌──────▼────────┐     ┌───────▼──────┐    ┌────▼────────┐
│ SseSender │        │ AdminWebPush  │     │ EmailSender  │    │ (future:    │
│ (.NET 10  │        │ Sender        │     │ SmsSender    │    │  Slack/     │
│  native)  │        │ (VAPID)       │     │ (opt-in)     │    │  Teams/     │
└────┬──────┘        └──────┬────────┘     └──────────────┘    │  Mobile)    │
     │                      │                                   └─────────────┘
     │                      │
     ▼                      ▼
 /events/               Browser
notifications        Service Worker
(HTTP/2 stream)     (push event → OS)
```

---

## 4. Faz Yol Haritası

| Faz | Ne | Ne zaman | Bu spec'in kapsamı |
|-----|----|----|---|
| **1** | Tek instance, InMemoryEventBus (outbox ile combined), PostgreSQL LISTEN/NOTIFY, SSE, AdminWebPush | **Bugün** | ✅ Tam kapsam |
| 2 | Multi-instance deployment; aynı kod, `IEventBus` implementasyonu değişmez (Postgres NOTIFY zaten multi-instance-friendly) | 10+ müşteri, instance sayısı > 1 | Soyutlama bugün hazır; yalnızca advisory lock + leader election ekleme |
| 3 | `RedisEventBus` / `NatsEventBus` drop-in, throughput patlaması durumunda | 50+ müşteri / saniyede 10k+ event | Soyutlama bugün hazır; tek dosya ekleme + tek DI satırı |
| 4 | Modül çıkarma (Storefront, Marketplace modüllerinin kendi process'lerine ayrılması) | Tamamen farklı scale durumu | Bu spec'in kapsamı dışı |

Faz 1'in çıktısı: **aynı kod Faz 2'de de kusursuz çalışır.** Faz geçişi kod değişikliği değil, DI/deployment değişikliğidir.

---

## 5. Event Catalog

Faz 1'de eklenecek/olgunlaştırılacak event'ler. Hepsi `BaseEvent`'den türer, `TenantId` taşır.

### 5.1 Core Domain Events

| Event | Payload | Kritiklik | Kaynak |
|-------|---------|-----------|--------|
| `ProductAddedEvent` | ProductId, Title, CategoryId, BrandId, CreatedByUserId | Info | `ProductManager.AddProductAsync` (mevcut) |
| `ProductUpdatedEvent` | ProductId, Title, ChangedFields[], UpdatedByUserId | Info | `ProductManager.UpdateProductAsync` (mevcut) |
| `ProductDeletedEvent` | **(yeni)** ProductId, Title, DeletedByUserId | Warning | `ProductManager.DeleteProductAsync` |
| `CategoryAddedEvent` | **(yeni)** CategoryId, Name, ParentId | Info | `CategoryManager.AddCategoryAsync` |
| `CategoryUpdatedEvent` | CategoryId, Name, ChangedFields[] | Info | `CategoryManager.UpdateCategoryAsync` (mevcut) |
| `CategoryDeletedEvent` | **(yeni)** CategoryId, Name | Warning | `CategoryManager.DeleteCategoryAsync` |
| `BrandAddedEvent` | **(yeni)** BrandId, Name | Info | `BrandManager.AddBrandAsync` |
| `BrandUpdatedEvent` | **(yeni)** BrandId, Name, ChangedFields[] | Info | `BrandManager.UpdateBrandAsync` |
| `BrandDeletedEvent` | **(yeni)** BrandId, Name | Warning | `BrandManager.DeleteBrandAsync` |

### 5.2 Marketplace Inbound Events

Mevcut marketplace polling/webhook servislerine eklenir. Her biri ilgili marketplace servisinden üretilir; servis event bus'a publish eder, notification handler ilgilenir.

| Event | Payload | Kritiklik | Kaynak |
|-------|---------|-----------|--------|
| `MarketplaceOrderReceivedEvent` | MarketPlaceId, OrderId, OrderNumber, CustomerName, Amount | Error (dikkat çeker) | Her marketplace'in OrderSyncBackgroundService'i |
| `MarketplaceProductApprovedEvent` | MarketPlaceId, ProductId, MarketplaceProductCode | Info | ProductStatusSyncBackgroundService |
| `MarketplaceProductRejectedEvent` | MarketPlaceId, ProductId, RejectionReason | Error | ProductStatusSyncBackgroundService |
| `MarketplaceQuestionAskedEvent` | MarketPlaceId, QuestionId, ProductId, QuestionText | Warning | QuestionSyncBackgroundService |
| `MarketplaceStockSyncFailedEvent` | MarketPlaceId, ProductId, Error | Warning | StockSyncBackgroundService |
| `MarketplacePriceUpdateFailedEvent` | MarketPlaceId, ProductId, Error | Warning | PriceSyncBackgroundService |
| `MarketplaceReturnReceivedEvent` | MarketPlaceId, ReturnId, OrderId, Reason | Warning | ReturnSyncBackgroundService |

### 5.3 Storefront Events

| Event | Payload | Kritiklik | Kaynak |
|-------|---------|-----------|--------|
| `StorefrontOrderPlacedEvent` | OrderId, CustomerId, Total | Error | StorefrontOrderController |
| `StorefrontNewCustomerEvent` | CustomerId, Email | Info | StorefrontAuthController |
| `StorefrontProductQuestionEvent` | QuestionId, ProductId, CustomerId | Warning | StorefrontQuestionController |
| `StorefrontReviewSubmittedEvent` | ReviewId, ProductId, Rating | Info | StorefrontReviewController |
| `StorefrontAbandonedCartEvent` | CartId, CustomerId, ValueAmount | Info | AbandonedCartBackgroundService |
| `StorefrontWalletWithdrawRequestEvent` | CustomerId, Amount | Warning | StorefrontWalletController |

### 5.4 System Events

| Event | Payload | Kritiklik | Kaynak |
|-------|---------|-----------|--------|
| `BackgroundJobFailedEvent` | JobName, Error, RetryCount | Warning | Ortak `JobExecutionWrapper` |
| `SyncErrorThresholdExceededEvent` | ServiceName, ErrorCount, WindowMinutes | Error | Sync health monitor (yeni) |
| `BranchOfficeApprovalRequestedEvent` | ApprovalId, BranchOfficeId, RequestedByUserId | Warning | BranchOfficeApprovalManager |
| `BranchOfficeApprovalApprovedEvent` | ApprovalId, BranchOfficeId, ApprovedByUserId | Info | BranchOfficeApprovalManager |
| `BranchOfficeApprovalRejectedEvent` | ApprovalId, BranchOfficeId, RejectedByUserId, Reason | Warning | BranchOfficeApprovalManager |

### 5.5 Internal Read-Sync Event

| Event | Payload | Not |
|-------|---------|----|
| `NotificationReadEvent` | NotificationId, UserId, ReadAt | **Sadece SSE broadcast** — outbox'a yazılmaz, ölümlü olması kabul (client sekme refresh olursa zaten yeniden çeker) |
| `NotificationDismissedEvent` | NotificationId, UserId | Aynı pattern |

**Toplam:** ~28 yeni event (mevcut 3'ün üstüne). Büyük kısmı küçük sınıflar (10-20 satır).

---

## 6. Veri Modeli Değişiklikleri

### 6.1 `NotificationsUsers` junction'a kolon ekleme

```csharp
public sealed class NotificationsUsers : BaseEntity
{
    public long NotificationId { get; set; }
    public Guid ApplicationUserId { get; set; }
    // mevcut:
    public bool IsDismissed { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
    // yeni:
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
```

### 6.2 `Notification` entity'sinden kolon çıkarma

`IsRead`, `ReadAt` kaldırılır. Mevcut kodun bu kolonları okuyan tüm yerleri junction'a taşınır.

**Two-phase migration (production güvenliği):**
- **Faz 1a:** `NotificationsUsers.IsRead`/`ReadAt` eklenir; `ProductManager` okuma akışları junction'a taşınır; `Notification.IsRead`/`ReadAt` yazımı durdurulur, ama kolonlar DB'de kalır.
- **Faz 1b (ayrı migration, 1-2 gün sonra):** `Notification.IsRead`/`ReadAt` kolonları drop edilir.

**Backfill:** `Faz 1a` migration'ının `Up()` içinde:
```sql
UPDATE notifications_users nu
SET is_read = n.is_read, read_at = n.read_at
FROM notifications n
WHERE nu.notification_id = n.id;
```

### 6.3 `NotificationOutbox` tablosu (yeni)

```csharp
public sealed class NotificationOutbox : BaseEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string EventType { get; set; } = null!;  // "ProductAddedEvent"
    public string PayloadJson { get; set; } = null!;
    public OutboxStatus Status { get; set; }  // Pending, Processing, Completed, Failed
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

public enum OutboxStatus { Pending, Processing, Completed, Failed }
```

**İndeksler:**
- `(Status, NextRetryAt)` — worker'ın pending satırları bulması için
- `(TenantId, CreatedAt)` — tenant başına gözlemlenebilirlik

**Trigger (NOTIFY):**
```sql
CREATE OR REPLACE FUNCTION notify_outbox_insert()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('notification_outbox_new', NEW.id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER outbox_insert_notify
AFTER INSERT ON notification_outbox
FOR EACH ROW EXECUTE FUNCTION notify_outbox_insert();
```

### 6.4 `DeadLetterOutbox` tablosu (yeni)

Maksimum retry aşıldığında (örn. 5) satır buraya taşınır. Admin UI'dan görüntülenir, manual retry butonu vardır.

### 6.5 `AdminPushSubscription` tablosu (yeni)

Storefront'un `StorefrontPushSubscription` entity'siyle karıştırılmaz (farklı tablo, farklı manager, farklı VAPID anahtarı seçeneği).

```csharp
public sealed class AdminPushSubscription : BaseEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dhKey { get; set; } = null!;
    public string AuthKey { get; set; } = null!;
    public string? UserAgent { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
```

Unique constraint: `(UserId, Endpoint)`.

### 6.6 `NotificationSetting` genişletmesi

Mevcut alanlara ek:

```csharp
public bool EnableWebPushNotifications { get; set; } = true;
public bool EnableInAppNotifications { get; set; } = true;
public NotificationDeliveryMode DeliveryMode { get; set; } = NotificationDeliveryMode.Always;
// Always, OnlyWhenOffline (ileride kullanılacak setting)
```

---

## 7. Event Bus + Outbox Tasarımı

### 7.1 `IEventBus` arayüzü

```csharp
public interface IEventBus
{
    /// <summary>
    /// Event'i publish eder. Eğer <paramref name="persistent"/> true ise outbox'a
    /// aynı transaction içinde yazılır; false ise sadece in-memory dispatch.
    /// </summary>
    ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = true, CancellationToken ct = default)
        where TEvent : BaseEvent;
}
```

**`persistent: true` default.** Kullanıcı-kritik event'ler (sipariş, rejection) zaten outbox'tan gitmeli. Sadece `NotificationReadEvent` gibi ölümlü/UI-sync event'leri `persistent: false` ile gönderilir.

### 7.2 Publish modelleri — iki ayrık yol

Event'ler iki tipte olabilir:

- **Persistent (kritik, domain):** `ProductAddedEvent`, `MarketplaceOrderReceivedEvent`, vb. → **outbox tablosuna, business transaction içinde** yazılır. In-memory channel'a yazılmaz. Handler'lar sadece `OutboxDispatcher` üzerinden çalışır. Crash-safe, idempotent.
- **Ephemeral (ölümlü, UI-sync):** `NotificationReadEvent`, `NotificationDismissedEvent` → **sadece in-memory channel'a** yazılır. Outbox'a yazılmaz. `InProcessEventDispatcher` üzerinden SSE broadcast handler'ı çalıştırır. Crash'te kayıp kabul (client reconnect'te DB'den yeniden çeker).

Bu ayrım **iki dispatch path'i arasında handler çağrısı dublikasyonunu önler.** Her event türü tek bir path'ten geçer.

### 7.3 `InMemoryEventBus` (Faz 1 implementation) — DbContext entegrasyonu

Outbox yazımının business transaction ile atomik olması için **caller'ın mevcut `DbContext` örneğine event eklenmesi** tercih edilir. Dış bir `DbContextFactory.CreateDbContextAsync` kullanmak ayrı bir transaction yaratır ve "business commit oldu ama outbox yazımı fail etti" senaryosunda event kaybı yaratır.

**Seçilen yaklaşım:** `IntegrationDbContext` içine pending event buffer + `SaveChangesAsync` override.

```csharp
public partial class IntegrationDbContext
{
    private readonly List<BaseEvent> _pendingEvents = [];

    public void AddDomainEvent(BaseEvent evt)
        => _pendingEvents.Add(evt);

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Outbox satırlarını aynı transaction'a ekle
        foreach (var evt in _pendingEvents)
        {
            Set<NotificationOutbox>().Add(new NotificationOutbox
            {
                EventType = evt.GetType().Name,
                PayloadJson = JsonSerializer.Serialize(evt, evt.GetType()),
                Status = OutboxStatus.Pending,
                TenantId = evt.TenantId
            });
        }
        var result = await base.SaveChangesAsync(ct);
        _pendingEvents.Clear();
        return result;
    }
}
```

**Business manager kullanımı:**

```csharp
// ProductManager.AddProductAsync:
dbContext.Products.Add(product);
dbContext.AddDomainEvent(new ProductAddedEvent(product.Id, product.Title, ...));
await dbContext.SaveChangesAsync();  // product + outbox row atomik commit
```

**`IEventBus` sadece ephemeral event'ler için:**

```csharp
public sealed class InMemoryEventBus(
    Channel<BaseEvent> ephemeralChannel,
    ITenantContext tenantContext) : IEventBus
{
    public async ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = true, CancellationToken ct = default)
        where TEvent : BaseEvent
    {
        @event.TenantId = tenantContext.TenantId;

        if (persistent)
        {
            throw new InvalidOperationException(
                "Persistent event'ler IEventBus yerine dbContext.AddDomainEvent() ile publish edilmelidir " +
                "(business transaction ile atomiklik için).");
        }

        await ephemeralChannel.Writer.WriteAsync(@event, ct);
    }
}
```

**API disiplini:**
- Persistent event → **`dbContext.AddDomainEvent(evt)` + `SaveChanges`**
- Ephemeral event → **`await _eventBus.PublishAsync(evt, persistent: false)`**

Bu iki yol birbirinin yerine kullanılmaz. Yanlış API kullanımı exception fırlatır, sessizce hata olmaz.

### 7.4 `OutboxDispatcher` (IHostedService)

```csharp
public sealed class OutboxDispatcher(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var listenConn = ...;   // Npgsql connection with LISTEN
        await listenConn.ExecuteAsync("LISTEN notification_outbox_new;");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Either a NOTIFY comes or a timeout (safety polling every 5s)
            await listenConn.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken);
            await DispatchPendingAsync(stoppingToken);
        }
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        // FOR UPDATE SKIP LOCKED — multi-instance güvenli
        var pending = await dbContext.Set<NotificationOutbox>()
            .FromSql($@"SELECT * FROM notification_outbox
                        WHERE status = {(int)OutboxStatus.Pending}
                          AND (next_retry_at IS NULL OR next_retry_at <= NOW())
                        ORDER BY created_at
                        LIMIT 50
                        FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);

        foreach (var row in pending)
        {
            row.Status = OutboxStatus.Processing;
        }
        await dbContext.SaveChangesAsync(ct);

        // Dispatch (outside of lock) — scoped scope, handler çalıştırma
        foreach (var row in pending)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            try
            {
                await DispatchSingleAsync(scope, row, ct);
                row.Status = OutboxStatus.Completed;
                row.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                row.RetryCount++;
                row.LastError = ex.Message;
                if (row.RetryCount >= 5)
                {
                    await MoveToDeadLetterAsync(row, ct);
                    row.Status = OutboxStatus.Failed;
                }
                else
                {
                    row.Status = OutboxStatus.Pending;
                    row.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, row.RetryCount));  // exp backoff
                }
                logger.LogError(ex, "Outbox dispatch failed for row {OutboxId}", row.Id);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
```

### 7.5 `InProcessEventDispatcher` (IHostedService) — sadece ephemeral

Ephemeral channel'dan okur. Yalnızca "ephemeral" olarak işaretlenmiş event tiplerinin handler'larını çağırır (örn. `NotificationReadSyncHandler`, `NotificationDismissedSyncHandler`). Bu handler'lar **yalnızca SSE broadcast** yapar, DB'ye yazmaz, email göndermez.

Durability sağlamaz. Crash'te kaybolabilir — ama kayıp olsa bile kritik değil (client reconnect'te DB'den doğru state'i çeker).

Handler dublikasyonu riski yok çünkü:
- Persistent event'ler **sadece** outbox dispatcher'a gider.
- Ephemeral event'ler **sadece** in-process dispatcher'a gider.
- Bir event tipi iki kategoriden birine aittir, ikisine birden değil.

---

## 8. Domain Event Handler'lar

### 8.1 Handler interface'i

```csharp
public interface IDomainEventHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

DI registration: `services.Scan(scan => scan.FromAssemblyOf<ProductAddedNotificationHandler>()
    .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());`

### 8.2 Recipient Policy Tablosu

Her handler `INotificationRecipientResolver`'ı kullanır. Permission bazlı çözüm tercih edilir (role-bazlı yerine); çünkü permission'lar daha granular.

| Event | Recipients |
|-------|-----------|
| `ProductAddedEvent` | `ResolveByPermissionAsync("products.view")` hariç olay aktörü |
| `ProductUpdatedEvent` | Aynı |
| `ProductDeletedEvent` | Aynı |
| `CategoryAddedEvent` | `ResolveByPermissionAsync("categories.view")` hariç aktör |
| `MarketplaceOrderReceivedEvent` | `ResolveByPermissionAsync("orders.view")` |
| `MarketplaceProductRejectedEvent` | Aktör + `ResolveByPermissionAsync("marketplace.manage")` |
| `StorefrontOrderPlacedEvent` | `ResolveByPermissionAsync("storefront.orders.view")` |
| `BranchOfficeApprovalRequestedEvent` | `ResolveByPermissionAsync("branchoffice.approve")` |
| `BackgroundJobFailedEvent` | `ResolveByPermissionAsync("admin.system.monitor")` |

**Özel kural:** Aktör kendi yaptığı eylem için bildirim almasın. Her event `ActorUserId` taşır, handler bu ID'yi recipient listesinden filtreler. Bu UX için çok önemli.

### 8.3 Template convention

Her handler `Header`/`Content`/`ActionUrl`/`Severity`/`Category` değerini üretir. Şimdilik kod içinde hard-coded Türkçe string'ler (gelecekte i18n gerekirse `IStringLocalizer` eklenebilir).

Örnek:

```csharp
public sealed class ProductAddedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<ProductAddedEvent>
{
    public async Task HandleAsync(ProductAddedEvent @event, CancellationToken ct)
    {
        var allRecipients = await resolver.ResolveByPermissionAsync("products.view", ct);
        var recipients = allRecipients.Where(id => id != @event.ActorUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni ürün eklendi",
            content: $"'{@event.ProductTitle}' adlı ürün eklendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
```

### 8.4 Handler testleri

Her handler için ayrı xUnit test sınıfı:
- Doğru recipient'a gönderiyor mu?
- Aktör kendini recipient'lardan çıkarıyor mu?
- Doğru severity/category/actionUrl üretiyor mu?
- Resolver hata fırlatırsa graceful behavior?

---

## 9. SSE Transport Tasarımı

### 9.1 Endpoint

.NET 10 native SSE: `TypedResults.ServerSentEvents<SseNotificationPayload>()`.

```csharp
[Authorize]
[HttpGet("/events/notifications")]
public IResult StreamNotifications(
    [FromServices] ISseConnectionRegistry registry,
    HttpContext httpContext)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    return TypedResults.ServerSentEvents(CreateStream(userId, httpContext.RequestAborted));

    async IAsyncEnumerable<SseItem<SseNotificationPayload>> CreateStream(
        Guid userId, [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<SseNotificationPayload>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        var connectionId = registry.Register(userId, channel);
        try
        {
            // Heartbeat her 25 saniyede bir (proxy keep-alive)
            using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(25));
            var heartbeatTask = SendHeartbeats(heartbeat, channel, ct);

            await foreach (var msg in channel.Reader.ReadAllAsync(ct))
            {
                yield return new SseItem<SseNotificationPayload>(msg)
                {
                    EventType = msg.EventType,
                    ReconnectionInterval = TimeSpan.FromSeconds(5)
                };
            }
        }
        finally
        {
            registry.Unregister(userId, connectionId);
        }
    }
}
```

### 9.2 `ISseConnectionRegistry`

Singleton. Multi-tenant için `ConcurrentDictionary<int, ConcurrentDictionary<Guid, List<Channel<...>>>>` — tenant → userId → connection list. (Bir kullanıcı birden fazla sekmede bağlanır.)

```csharp
public interface ISseConnectionRegistry
{
    Guid Register(Guid userId, Channel<SseNotificationPayload> channel);
    void Unregister(Guid userId, Guid connectionId);
    IReadOnlyList<Channel<SseNotificationPayload>> GetChannels(Guid userId);
    int GetActiveConnectionCount(Guid userId);  // "online" tespiti için
}
```

### 9.3 `SseNotificationSender`

`INotificationSender` implementation. Her recipient için `ISseConnectionRegistry.GetChannels(userId)` çağırır, tüm channel'lara `TryWrite` yapar. Bağlantı yoksa ses çıkarmaz (web push devreye girer).

### 9.4 Event tipleri (SSE üzerinden)

- `event: notification` — yeni bildirim geldi, payload tam `Notification` objesi + `ActionUrl`.
- `event: notification.read` — başka bir cihaz okudu, client kendi badge/toast'unu düşürür. Payload: `{ notificationId, readAt }`.
- `event: notification.dismissed` — başka bir cihaz dismiss etti. Client kendi UI'ından kaldırır.
- `event: heartbeat` — 25s interval, boş payload, connection keep-alive.

### 9.5 Reconnection & Last-Event-ID

Client `EventSource` browser-native reconnection yapar. `Last-Event-ID` header'ı gönderir. Server: bu ID'den büyük bildirim/okundu kayıtlarını DB'den çeker ve replay eder. Connection kaybında kaybolan event'leri recover eder.

**Sadeleştirilmiş ilk sürüm:** Reconnect'te client `/notifications?onlyUnread=true` çağırıp unread listesini yenileyebilir (bir HTTP roundtrip). Last-Event-ID replay karmaşık, Faz 1 için YAGNI.

### 9.6 Thread modeli

- `Channel.CreateBounded(100)` per connection — kullanıcının bildirim üretiminden daha yavaş okuması durumunda `DropOldest` politikası.
- `PeriodicTimer` — thread tutmaz, async bekler.
- Connection kapandığında `finally` bloğu registry'den çıkarır; GC channel'ı topar.
- Bir dispatch 10k kullanıcıya aynı anda giderse: `TryWrite` non-blocking, sender ms altında tamamlanır.
- ThreadPool'a hiçbir blocking çağrı gitmez.

---

## 10. Admin Web Push

### 10.1 VAPID anahtarları

`appsettings.json`:
```json
{
  "WebPush": {
    "VapidSubject": "mailto:admin@entegrasyon.tr",
    "VapidPublicKey": "BG...",
    "VapidPrivateKey": "xxx"
  }
}
```

İlk kurulumda `Lib.Net.Http.WebPush` (Tomasz Pęczek) veya `WebPush` NuGet paketi ile generate edilir. Storefront'unki farklı anahtar — karışmasınlar.

### 10.2 Subscription flow

**Client (sayfa yüklenirken):**
1. `navigator.serviceWorker.register('/sw-admin.js')`
2. `Notification.permission` kontrolü; `default` ise kullanıcıya "Bildirim izni ister misiniz?" sor (Notyf toast + buton).
3. Kabul → `registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: vapidPublicKey })` → POST `/api/admin-push/subscribe`.
4. Server `AdminPushSubscription` tablosuna kaydeder.

### 10.3 Service worker (`/sw-admin.js`)

```js
self.addEventListener('push', event => {
  const data = event.data.json();
  event.waitUntil(self.registration.showNotification(data.title, {
    body: data.body,
    icon: '/img/logo-192.png',
    badge: '/img/badge-72.png',
    data: { url: data.actionUrl, notificationId: data.notificationId },
    tag: 'notification-' + data.notificationId,  // aynı ID'li bildirimler birleşir
    renotify: false
  }));
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data.url || '/notifications';
  event.waitUntil(clients.matchAll({ type: 'window' }).then(windowClients => {
    for (const client of windowClients) {
      if (client.url === url && 'focus' in client) return client.focus();
    }
    return clients.openWindow(url);
  }));
});
```

### 10.4 `AdminWebPushSender`

`INotificationSender` implementation. Her recipient için `AdminPushSubscription` satırlarını çeker, her endpoint'e paralel push gönderir. 410 Gone → abonelik ölmüş, tabloyu sil.

---

## 11. Multi-Device Read Sync

### 11.1 Akış

1. Kullanıcı Device A'da `POST /notifications/{id}/read` çağırır.
2. `NotificationManager.MarkAsRead` DB junction'ı günceller.
3. Publish: `NotificationReadEvent(notificationId, userId, readAt)` ile `persistent: false` (SSE-only).
4. In-process channel → `SseReadSyncHandler` → `ISseConnectionRegistry.GetChannels(userId)` → **Device A hariç** tüm aktif sekmelere `event: notification.read` gönderir (Device A zaten UI'da lokal olarak güncelledi; gereksiz double-render).

   Aslında "Device A hariç" implementasyonu karmaşık (hangi channel Device A'nındı?). Basitleştirme: **tüm cihazlara push**, Device A client-side idempotent (zaten okundu görünüyorsa no-op).

5. Her client SSE event'i alır → badge sayacını düşürür → toast'u varsa kaldırır → "Bildirimler" sayfasındaysa ilgili satırı "okundu" olarak görsel olarak günceller.

### 11.2 Dismiss sync

Aynı pattern `NotificationDismissedEvent` ile.

---

## 12. Bildirim UI & Navigasyon

Kullanıcının talebi: "bildirimler sayfası, gelen bildirimler ve okundu olarak işaretle tam çalışsın", "bazı bildirimlere tıklayınca gerekli sayfaya gitmemiz gerekebilir".

### 12.1 Bileşenler

**(A) Zil Dropdown (Top Nav)** — `_NotificationBell.cshtml` partial, layout'un üstüne eklenir.

- Zil ikonu + unread sayacı badge (Tabler `badge bg-red badge-notification`)
- Tıklayınca dropdown: son 10 okunmamış bildirim
- Her satır: severity'e göre renk ikonu, header, content snippet, relative time ("2 dk önce")
- Alt: "Tümünü gör" → `/notifications`
- Her satır tıklanabilir → `ActionUrl` varsa oraya git + `MarkAsRead` HTMX post
- `ActionUrl` yoksa sadece `MarkAsRead` (toast ile bilgi)
- Boş state: "Henüz bildiriminiz yok"

**(B) Bildirimler Sayfası (`/notifications`)** — mevcut sayfa, polish.

- Tab'lar: `Tümü` | `Okunmamış` | `Okundu`
- Filtre: kategori (dropdown), severity (dropdown), tarih aralığı
- Liste: her satır `ActionUrl` varsa link
- Sağ üstte: "Tümünü okundu işaretle", "Okunmuşları temizle"
- Empty state: Tabler `empty` component
- Pagination veya infinite scroll

**(C) Anlık Güncelleme (SSE client)**

- Tüm sayfalarda yüklenen `/js/notifications-client.js`:
  - SSE bağlantısını açar (`new EventSource('/events/notifications')`)
  - `notification` event'i → zil dropdown'ı güncelle, badge +1, Notyf toast göster (opsiyonel, `NotificationSetting.ShowSnackbar` kontrolü)
  - `notification.read` event'i → badge -1, toast varsa kapat, liste sayfasındaysa satırı güncelle
  - Reconnect logic otomatik (browser native)

### 12.2 ActionUrl policy

Her event handler `ActionUrl` doldurur:

| Event | ActionUrl |
|-------|-----------|
| `ProductAddedEvent` | `/products/{productId}` |
| `ProductUpdatedEvent` | `/products/{productId}` |
| `ProductDeletedEvent` | `null` (silindi, gidilecek yer yok) |
| `CategoryAddedEvent` | `/categories/{categoryId}` |
| `MarketplaceOrderReceivedEvent` | `/orders/{orderId}` |
| `MarketplaceProductRejectedEvent` | `/products/{productId}` |
| `MarketplaceQuestionAskedEvent` | `/marketplace/{mpId}/questions/{questionId}` |
| `StorefrontOrderPlacedEvent` | `/storefront/orders/{orderId}` |
| `BranchOfficeApprovalRequestedEvent` | `/admin/branch-offices/approvals/{approvalId}` |
| `BackgroundJobFailedEvent` | `/admin/system/logs?jobName={jobName}` |

### 12.3 Tabler UI referansları (dokümantasyona bakılacak)

- `dropdown` (zil dropdown)
- `badge` (unread sayacı)
- `timeline` (bildirim listesi?)
- `empty` (boş state)
- `status-dot` (severity renklendirmesi)

Her component kullanılmadan önce https://tabler.io/docs/ui/<component> kontrol edilecek (bkz. memory: Tabler Component Docs First).

---

## 13. Threading & Non-Blocking Garantileri

Kullanıcı geri bildirimi: "thread sıkıntımız oluyor, uygulama çok geç cevap vermeye başlıyor". Bu önlem listesi o incidenti önlemek içindir.

### 13.1 Yasaklar

- `Task.Result`, `Task.Wait()`, `.GetAwaiter().GetResult()` — hiçbir yerde.
- `Task.Run(async () => ...)` — request path'inde **asla**. (Background servisler `IHostedService` olmalı, `Task.Run` değil.)
- Senkron `Stream.Write`, `Stream.Read` — SSE response yazımında `WriteAsync` kullanılır.
- DbContext'i aynı anda iki `await` arasında paylaşmak (bkz. memory: No Task.WhenAll with DbContext).
- SemaphoreSlim global lock — tenant-bazlı olmalı.

### 13.2 Zorunlu pattern'ler

- Tüm BackgroundService'ler `Channel<T>` + async loop pattern'inde.
- SSE endpoint'inde per-connection `BoundedChannel<T>` + `DropOldest` — slow consumer diğerlerini etkilemez.
- Outbox dispatcher `FOR UPDATE SKIP LOCKED` — multi-instance'ta da blokaj yok.
- Handler'lar paralel çalışır (`Task.WhenAll`) ama aralarında shared DbContext yok (her scope kendi factory'sinden DbContext yaratır).
- CPU-bound iş yoksa `Task.Run`'a gerek yok; her şey I/O-bound async.

### 13.3 Doğrulama

- Test: yüklenen test senaryosu (100 eş zamanlı SSE connection + saniyede 50 event) altında ThreadPool available thread sayısı sabit kalmalı (unit olmaz; integration/loadtest).
- Dev'de `dotnet-counters monitor Microsoft.AspNetCore.Hosting` ile "Current Requests" sabit, "Threadpool Queue Length" <10.
- Prod'da OpenTelemetry metrics: `threadpool.queue.length`, `sse.active_connections`, `outbox.dispatch.duration`.

---

## 14. Gelecek Platform Genişletilmesi

Faz 1'de **implement edilmez** ama arayüz hazırlanır:

```csharp
// Faz 1'de yeterli:
public interface INotificationSender
{
    SenderType Type { get; }
    Task SendNotification(Notification message, IEnumerable<Guid> userIds);
}

public enum SenderType
{
    SignalR,      // [Obsolete]
    Sse,          // yeni
    Email,
    Sms,
    WebPushAdmin, // yeni
    // Aşağıdakiler Faz 1'de yok:
    Slack,
    Teams,
    MobilePushAndroid,
    MobilePushIos
}
```

Gelecekte yeni sender eklemek:
1. `I{X}Sender : INotificationSender` implementasyonu yaz.
2. `SenderType` enum'a ekle.
3. DI'a kaydet (`services.AddScoped<INotificationSender, SlackSender>()`).
4. Opsiyonel: `NotificationSetting`'e `EnableSlackNotifications` toggle.
5. Kullanıcı başına konfigürasyon gerekiyorsa (Slack webhook URL) yeni entity (`UserSlackIntegration`).

**İş kodu hiçbir yerde değişmez.**

---

## 15. Test Stratejisi

### 15.1 Unit Tests (`Test/Entegrasyon.Test/`)

- Her domain event handler için test sınıfı
- `NotificationManager.SendNotification` genişletilmiş testler (mevcut zaten var)
- `InMemoryEventBus` testi: persistent=true → outbox'a yazar; false → yazmaz
- `OutboxDispatcher.DispatchSingleAsync` testi (mock handler)
- `ISseConnectionRegistry` testi: register/unregister, multi-connection, multi-tenant
- Retry/backoff mantığı

### 15.2 Integration Tests (`Test/Entegrasyon.IntegrationTest/`)

- **Full event flow:** `ProductManager.AddProductAsync` → outbox satırı yazıldı → dispatcher handler çağırdı → Notification DB'ye yazıldı → NotificationsUsers junction dolu.
- **Outbox durability:** Handler ilk denemede fail → row `Pending` kaldı → retry count arttı → sonraki dispatch'te başarılı oldu.
- **Dead letter:** 5 retry sonrası `DeadLetterOutbox`'a taşındı.
- **PostgreSQL NOTIFY:** test ortamında trigger gerçekten mesaj gönderiyor mu.
- **FOR UPDATE SKIP LOCKED:** iki paralel dispatcher aynı satırı almıyor.
- **Multi-device read sync:** junction update sonrası `NotificationReadEvent` in-memory channel'a yazılmış mı.

### 15.3 MVC/Controller Tests (`Test/Entegrasyon.MVC.Test/`)

- SSE endpoint: auth olmadan 401; auth ile stream başlıyor.
- `NotificationController.MarkAsRead` → junction güncellendi.
- Zil dropdown partial render.

### 15.4 E2E Tests (Playwright, `Test/Entegrasyon.E2E/`)

- Login → ürün ekle → notification bell badge 1 artışı (başka kullanıcı olarak login'de)
- Notification'a tıkla → `/products/{id}` sayfasına yönlen
- "Tümünü okundu işaretle" → badge 0
- İki sekme açık → bir sekmede okunmuşsa diğer sekmede de badge güncellendi (SSE çalışıyor testi)

### 15.5 Manuel Duman Testi (Chrome DevTools MCP)

Kullanıcı açıkça istedi: "chrome ile test edebilirsin". Her fazdan sonra:

- `mcp__chrome-devtools-mcp__new_page` → uygulamaya git
- Login → ürün ekle
- Yeni sekme aç, farklı kullanıcı ile login
- İlk sekmede ürün eklenince → ikinci sekmede SSE ile zil badge artışı network request'te görünmeli (`list_network_requests` ile `/events/notifications` stream kontrolü)
- Tıklama: bildirim tıkla → `/products/{id}` navigasyonu
- Web push izni: `Notification.permission` kontrolü (JS console), subscribe akışı
- Multi-tab read sync: iki sekmede aynı kullanıcı → birinde okundu → diğerinde badge düşüşü
- Console error'ları: `list_console_messages` ile boş olmalı (no uncaught exceptions)

### 15.6 Önce-Sonra Test Disiplini (Strict Rule)

Her handler/transport değişiklik adımında:
1. **Öncesi:** Tam test suite çalıştır → yeşil mi? Baseline.
2. Değişikliği yap.
3. **Sonrası:** Aynı suite + yeni testler → yeşil mi?
4. Regression varsa önce düzelt, sonra ilerle.

(Bkz. memory: SignalR→SSE Migration Rules, Run Tests After Changes.)

---

## 16. Migration & Rollout

### 16.1 Faz planı (implementasyon sırası)

| Adım | İçerik | Risk | Rollback |
|------|--------|------|----------|
| **0** | `IEventBus` + outbox altyapısı (sadece iskelet, publish edilmiyor) | Düşük | Commit revert |
| **1** | `NotificationsUsers` IsRead/ReadAt eklenmesi + backfill + yazma akışları taşınması | Orta (migration) | Migration revert |
| **2** | Handler'lar yazılır (25-30), ama publish hiçbir yerde yapılmıyor; integration test'lerle doğrulanır | Düşük | Kodlar `#if NOTIFICATIONS_ENABLED` flag altında |
| **3** | Publish noktaları eklenir (ProductManager, CategoryManager, BrandManager, Marketplace bg services, Storefront controllers) — feature flag arkasında | Orta (production etki) | Feature flag OFF |
| **4** | SSE endpoint + client JS + zil dropdown partial + bildirimler sayfası polish | Orta (UX) | Feature flag OFF, eski SignalR endpoint'i paralelde kalır |
| **5** | AdminWebPush + service worker | Düşük (opt-in) | Feature flag OFF |
| **6** | SignalR NotificationHub kaldırılır (sadece ChatHub kalır) | Orta | Git revert |
| **7** | `Notification.IsRead`/`ReadAt` kolonları drop edilir (ayrı migration, 1-2 gün gözlem sonrası) | Düşük | Migration revert |

### 16.2 Feature flag'ler

`appsettings.json`:
```json
{
  "Features": {
    "NotificationsV2Publish": false,   // yeni event publish noktaları aktif mi
    "NotificationsV2Sse": false,       // SSE endpoint aktif mi
    "NotificationsV2WebPush": false    // Web push aktif mi
  }
}
```

Her feature bağımsız toggle edilebilir. Production'da aşama aşama açılır.

### 16.3 Rollback senaryoları

- **Handler hata veriyor:** Outbox retry 5 deneme, sonra dead letter → kullanıcı akışı etkilenmez (Product ekleme hala çalışır, sadece bildirim gitmez). Admin dead letter UI'dan görür, manual retry eder.
- **SSE endpoint thread tüketiyor:** Feature flag OFF → eski NotificationHub'a dön (paralel bırakılır Adım 6 öncesinde).
- **Migration kırılırsa:** İki-fazlı migration ile risk azaltıldı; ilk faz geri alınabilir.

---

## 17. Observability

### 17.1 Metrikler (OpenTelemetry)

| Metrik | Tip | Açıklama |
|--------|-----|----------|
| `notifications.outbox.pending_count` | Gauge | Bekleyen outbox satır sayısı (her 10s toplanır) |
| `notifications.outbox.dispatch.duration` | Histogram | Bir satırın dispatch süresi (ms) |
| `notifications.outbox.dispatch.success_count` | Counter | Başarılı dispatch sayısı |
| `notifications.outbox.dispatch.failure_count` | Counter | Başarısız dispatch sayısı |
| `notifications.outbox.dead_letter_count` | Gauge | Dead letter tablosundaki satır sayısı |
| `notifications.sse.active_connections` | Gauge | Aktif SSE bağlantı sayısı (tenant başına tag) |
| `notifications.sse.messages_sent` | Counter | SSE üzerinden gönderilen mesaj sayısı |
| `notifications.webpush.success_count` | Counter | Başarılı web push sayısı |
| `notifications.webpush.failure_count` | Counter | 410 Gone dahil her fail |
| `notifications.delivery.latency` | Histogram | Event publish'ten ilk teslimata kadar geçen süre |

### 17.2 Dashboard

Grafana'da yeni bir "Notifications" dashboard:
- Outbox lag (pending count + oldest pending age)
- Dispatch başarı oranı (son 1 saat)
- SSE connection count (tenant başına)
- Dead letter alerts (sayı > 0 ise Slack/e-posta alert)

### 17.3 Logging

`IApplicationLogManager.AddLog` (kullanıcı-facing, admin dashboard'da görünür):
- "Bildirim gönderildi: {count} kullanıcıya" (LogType.Notification, LogAction.Send)
- "Dead letter: {eventType} ({retryCount} deneme başarısız)"

`ILogger<T>` (developer-facing, Serilog + Loki):
- Dispatch detayları, exception stack trace, timing

(Bkz. memory: ApplicationLog vs ILogger — ikisi de kullanılacak.)

---

## 18. Riskler & Açık Sorular

### 18.1 Riskler

- **Outbox büyüme:** `Completed` satırları büyürse tablo şişer. Çözüm: archival job — 30 gün sonra `Completed` satırları `notification_outbox_archive`'a taşır (veya sadece drop — audit gereği yoksa).
- **PostgreSQL `LISTEN` connection kaybı:** Npgsql reconnect mantığı var ama event kaçarsa? Çözüm: safety polling her 5 saniyede de çalışır (NOTIFY'a bağlı değil).
- **SSE uzun-ömürlü bağlantılar:** Load balancer timeout'u (nginx default 60s) + HTTP/2. Nginx config: `proxy_read_timeout 24h;`. Heartbeat (25s) de bunu sağlar.
- **Çok fazla handler race:** 30 handler paralel aynı event'i işlerse DB pool tükenebilir. Çözüm: DbContext factory + bounded parallelism (`SemaphoreSlim` outbox dispatcher içinde, max 10 concurrent handler).
- **Tenant explosion:** Multi-tenant'ta tenant başına ayrı LISTEN/NOTIFY olmaz; tek kanal `notification_outbox_new`. Dispatcher payload'dan `TenantId`'yi okuyup doğru tenant scope'una geçer. (Multi-tenant DB-per-tenant ise LISTEN her tenant-DB'de ayrı ayrı açılır — ayrı tartışma.)

### 18.2 Açık Sorular (spec sonrası netleşir)

- **Bildirim süresi (TTL):** Eski bildirimler ne kadar süre DB'de tutulacak? 90 gün? Config'e bağlanır.
- **Bulk event publish:** "1000 ürün CSV import" senaryosunda her ürün için bildirim gönderilmemeli — tek "1000 ürün eklendi" batch bildirimi. İmport yolunda `IEventBus.PublishBatchAsync` gibi bir API gerekebilir (veya import-level ayrı event tipi: `BulkProductImportCompletedEvent`).
- **Kullanıcı sessize alma:** Bir kullanıcı belirli bir kategoriye ait bildirimleri tamamen kapatmak isterse? `NotificationSetting`'e per-category mute eklenecek mi? Faz 1 dışı.
- **Digest mode:** Saatlik/günlük özet e-posta. Faz 1 dışı.

---

## 19. Referanslar

- [Server-Sent Events in ASP.NET Core and .NET 10 (Milan Jovanović)](https://www.milanjovanovic.tech/blog/server-sent-events-in-aspnetcore-and-dotnet-10)
- [You Probably Don't Need SignalR in .NET 10](https://systemshogun.com/p/you-probably-dont-need-signalr-in)
- [Debug ThreadPool Starvation — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-threadpool-starvation)
- [Push notifications for ASP.NET Core PWAs — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app/push-notifications?view=aspnetcore-10.0)
- [Demystifying Web Push Notifications (PQVST)](https://pqvst.com/2023/11/21/web-push-notifications/)
- [Outbox pattern — microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [PostgreSQL LISTEN/NOTIFY — Npgsql docs](https://www.npgsql.org/doc/wait.html)
- [Tabler UI Docs](https://tabler.io/docs/)

---

## 20. Onay ve Sonraki Adımlar

Bu spec onaylandığında:
1. `writing-plans` skill'ine geçilecek.
2. Implementation plan spec'in 16. bölümündeki faz listesine göre çıkarılacak.
3. Her faz bağımsız bir subtask olacak; her biri kendi testleriyle gelir.
4. Geliştirme sırasında her adımdan önce ve sonra test çalıştırılacak (Strict Rule).
