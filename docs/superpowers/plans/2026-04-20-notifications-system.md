# Bildirim Sistemi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Event yayın + outbox (durability) + SSE transport + admin web push + multi-device read sync + bildirim UI'nı üretime hazır şekilde inşa etmek.

**Architecture:** Modular monolith; `dbContext.AddDomainEvent(evt)` + `SaveChanges` ile business tx-atomik outbox yazımı (persistent); ephemeral UI-sync için `IEventBus.PublishAsync(evt, persistent:false)` ile in-memory `Channel<T>`. `OutboxDispatcher` IHostedService PostgreSQL `LISTEN/NOTIFY` ile crash-safe dispatch; handler'lar (`IDomainEventHandler<T>`) `INotificationManager` üzerinden DB'ye yazar + tüm `INotificationSender`'ları tetikler. Transport olarak `.NET 10 TypedResults.ServerSentEvents` + VAPID Web Push. SignalR sadece `ChatHub` için kalır.

**Tech Stack:** .NET 10, C# 13, EF Core 10 (PostgreSQL + Npgsql), xUnit + Moq + FluentAssertions, Testcontainers.PostgreSql + Respawn, Playwright (NUnit) E2E, Lib.Net.Http.WebPush, Tabler UI, HTMX, Vanilla JS (safe DOM methods — innerHTML yasak, XSS koruması).

**Referans spec:** `docs/superpowers/specs/2026-04-20-notifications-design.md`

---

## File Structure Özeti

### Yeni dosyalar

**Entity katmanı (`Entegrasyon.Entity/Notifications/`):**
- `NotificationOutbox.cs` — outbox entity
- `OutboxStatus.cs` — enum
- `DeadLetterOutbox.cs` — dead letter entity
- `AdminPushSubscription.cs` — VAPID subscription entity

**DataAccess katmanı:**
- `EntityConfigurations/NotificationOutboxEntityConfiguration.cs`
- `EntityConfigurations/DeadLetterOutboxEntityConfiguration.cs`
- `EntityConfigurations/AdminPushSubscriptionEntityConfiguration.cs`
- 3 yeni migration (outbox tabloları + NOTIFY trigger, junction IsRead/ReadAt, legacy column drop)

**Business katmanı (`Entegrasyon.Business/`):**
- `Channels/IEventBus.cs` + `InMemoryEventBus.cs` + `OutboxDispatchOptions.cs`
- `Channels/OutboxDispatcher.cs` + `InProcessEventDispatcher.cs`
- `Channels/Events/Products/ProductDeletedEvent.cs`
- `Channels/Events/Categories/{Added,Deleted}Event.cs`
- `Channels/Events/Brands/{Added,Updated,Deleted}Event.cs`
- `Channels/Events/Marketplace/*Event.cs` (7 adet)
- `Channels/Events/Storefront/*Event.cs` (6 adet)
- `Channels/Events/System/*Event.cs` (5 adet)
- `Channels/Events/Notifications/{NotificationReadEvent,NotificationDismissedEvent}.cs`
- `Notifications/Handlers/IDomainEventHandler.cs` + 26 handler dosyası
- `Notifications/Sse/{ISseConnectionRegistry,SseConnectionRegistry,SseNotificationSender,SseNotificationPayload}.cs`
- `Notifications/WebPush/{IAdminPushSubscriptionManager,AdminPushSubscriptionManager,AdminWebPushSender,WebPushOptions}.cs`
- `Options/NotificationFeatureFlags.cs`
- `Abstract/ICurrentUserContext.cs`

**MVC katmanı (`Entegrasyon.MVC/`):**
- `Features/Notifications/SseController.cs`
- `Features/Notifications/AdminPushController.cs`
- `Features/Notifications/Views/Partials/_NotificationBell.cshtml`
- `Infrastructure/CurrentUserContext.cs`
- `wwwroot/js/notifications-client.js`
- `wwwroot/sw-admin.js`

**Test katmanı:**
- `Test/Entegrasyon.Test/Business/Notifications/InMemoryEventBusTests.cs`
- `Test/Entegrasyon.Test/Business/Notifications/InProcessEventDispatcherTests.cs`
- `Test/Entegrasyon.Test/Business/Notifications/SseConnectionRegistryTests.cs`
- `Test/Entegrasyon.Test/Business/Notifications/Handlers/{EventName}HandlerTests.cs` (26 dosya)
- `Test/Entegrasyon.IntegrationTest/Notifications/DomainEventBufferTests.cs`
- `Test/Entegrasyon.IntegrationTest/Notifications/OutboxDispatcherTests.cs`
- `Test/Entegrasyon.IntegrationTest/Notifications/DomainEventPipelineTests.cs`
- `Test/Entegrasyon.IntegrationTest/Notifications/NotificationManagerTests.cs`
- `Test/Entegrasyon.E2E/Notifications/NotificationE2ETests.cs`

### Değiştirilecek dosyalar

- `Entity/Notifications/NotificationsUsers.cs` — IsRead/ReadAt eklenir
- `Entity/Notifications/Notification.cs` — IsRead/ReadAt kaldırılır (Phase 7)
- `DataAccess/.../Contexts/IntegrationDbContext.cs` — DbSets + AddDomainEvent + SaveChanges override
- `DataAccess/.../EntityConfigurations/NotificationsUsersEntityConfiguration.cs` — yeni kolonlar
- `Business/Concrete/NotificationManager.cs` — junction okuma/yazma + ephemeral publish
- `Business/Concrete/{ProductManager,CategoryManager,BrandManager}.cs` — publish points
- `Business/Concrete/Marketplaces/*BackgroundService.cs` — publish points
- `Business/Concrete/Storefront/*.cs` — publish points
- `ApplicationBootstrap/ApplicationDependencyExtension.cs` — yeni DI
- `MVC/Features/Notifications/NotificationController.cs` — filter/tab
- `MVC/Features/Notifications/Views/Index.cshtml` — polish
- `MVC/Views/Shared/_Layout.cshtml` — zil partial + client JS
- `MVC/Program.cs` — gerekli middleware
- `MVC/appsettings.json` — feature flags + VAPID + outbox config

### Silinecek dosyalar (Phase 6)

- `Business/Notifications/SignalR/NotificationHub.cs`
- `Business/Notifications/SignalR/SignalRSender.cs`
- `Business/Notifications/SignalR/ISignalRNotificationSender.cs`
- `Application/Entegrasyon.Blazor/Utility/Notifications/*` — Blazor kalıntısı

---

## Strict Rules (Tüm fazlar için)

- **Her task'tan önce:** `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` → yeşil olduğunu doğrula (baseline).
- **Her task'tan sonra:** aynı komut + varsa integration/E2E → yeşil olmalı.
- **Frequent commits:** her task tek commit. Commit mesajı: `feat(notifications): [task açıklaması]` veya `test(notifications): ...` / `refactor(notifications): ...`.
- **No blocking calls:** `.Result`, `.Wait()`, `Task.Run(...)` hiçbir yerde. Review'da yakalanır.
- **No innerHTML:** Vanilla JS'te `element.innerHTML = ...` yasak; `textContent`, `createElement`, `setAttribute` kullan (XSS koruması).
- **Migration disiplini:** Her entity değişiminden sonra `dotnet ef migrations add ...` + manuel incele + `update` + `has-pending-model-changes` → False olmalı.
- **Türkçe karakterler:** ş/ğ/ü/ö/ç/ı/İ proper yazılır, ASCII mirror yapılmaz.

---

# Phase 0: Outbox Infrastructure Foundation

## Task 0.1: NotificationOutbox Entity ve OutboxStatus Enum

**Files:**
- Create: `Application/Entegrasyon.Entity/Notifications/OutboxStatus.cs`
- Create: `Application/Entegrasyon.Entity/Notifications/NotificationOutbox.cs`

- [ ] **Step 1: OutboxStatus enum**

```csharp
namespace Entegrasyon.Entity.Notifications;

public enum OutboxStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
```

- [ ] **Step 2: NotificationOutbox entity**

```csharp
namespace Entegrasyon.Entity.Notifications;

public sealed class NotificationOutbox : BaseEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Notifications/OutboxStatus.cs \
        Application/Entegrasyon.Entity/Notifications/NotificationOutbox.cs
git commit -m "feat(notifications): NotificationOutbox entity ve OutboxStatus enum"
```

---

## Task 0.2: DeadLetterOutbox Entity

**Files:**
- Create: `Application/Entegrasyon.Entity/Notifications/DeadLetterOutbox.cs`

- [ ] **Step 1: Dosya**

```csharp
namespace Entegrasyon.Entity.Notifications;

public sealed class DeadLetterOutbox : BaseEntity
{
    public long Id { get; set; }
    public long OriginalOutboxId { get; set; }
    public int TenantId { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string FinalError { get; set; } = null!;
    public int TotalRetryCount { get; set; }
    public DateTimeOffset MovedAt { get; set; }
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj
git add Application/Entegrasyon.Entity/Notifications/DeadLetterOutbox.cs
git commit -m "feat(notifications): DeadLetterOutbox entity"
```

---

## Task 0.3: AdminPushSubscription Entity

**Files:**
- Create: `Application/Entegrasyon.Entity/Notifications/AdminPushSubscription.cs`

- [ ] **Step 1: Dosya**

```csharp
namespace Entegrasyon.Entity.Notifications;

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

- [ ] **Step 2: Build + commit**

```bash
dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj
git add Application/Entegrasyon.Entity/Notifications/AdminPushSubscription.cs
git commit -m "feat(notifications): AdminPushSubscription entity"
```

---

## Task 0.4: Entity Configuration'lar

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/NotificationOutboxEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/DeadLetterOutboxEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/AdminPushSubscriptionEntityConfiguration.cs`

- [ ] **Step 1: NotificationOutboxEntityConfiguration**

```csharp
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class NotificationOutboxEntityConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> b)
    {
        b.ToTable("notification_outbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.LastError).HasMaxLength(4000);
        b.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("ix_notification_outbox_status_next_retry_at");
        b.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("ix_notification_outbox_tenant_created_at");
    }
}
```

- [ ] **Step 2: DeadLetterOutboxEntityConfiguration**

```csharp
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class DeadLetterOutboxEntityConfiguration : IEntityTypeConfiguration<DeadLetterOutbox>
{
    public void Configure(EntityTypeBuilder<DeadLetterOutbox> b)
    {
        b.ToTable("dead_letter_outbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.FinalError).HasColumnType("text").IsRequired();
        b.HasIndex(x => new { x.TenantId, x.MovedAt })
            .HasDatabaseName("ix_dead_letter_outbox_tenant_moved_at");
    }
}
```

- [ ] **Step 3: AdminPushSubscriptionEntityConfiguration**

```csharp
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class AdminPushSubscriptionEntityConfiguration : IEntityTypeConfiguration<AdminPushSubscription>
{
    public void Configure(EntityTypeBuilder<AdminPushSubscription> b)
    {
        b.ToTable("admin_push_subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Endpoint).HasMaxLength(2000).IsRequired();
        b.Property(x => x.P256dhKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.AuthKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.Endpoint }).IsUnique()
            .HasDatabaseName("ux_admin_push_subscriptions_user_endpoint");
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/Notification*.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/DeadLetter*.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/AdminPush*.cs
git commit -m "feat(notifications): entity configuration'lar (outbox, dead letter, admin push)"
```

---

## Task 0.5: DbContext'e DbSets ekle

**Files:**
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

- [ ] **Step 1: Dosyayı aç, mevcut DbSet listesine ekle**

```csharp
public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
public DbSet<DeadLetterOutbox> DeadLetterOutbox => Set<DeadLetterOutbox>();
public DbSet<AdminPushSubscription> AdminPushSubscriptions => Set<AdminPushSubscription>();
```

Gereken using: `using Entegrasyon.Entity.Notifications;`

- [ ] **Step 2: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs
git commit -m "feat(notifications): DbContext'e outbox/dead-letter/admin-push DbSets"
```

---

## Task 0.6: Migration — yeni tablolar + PostgreSQL NOTIFY trigger

- [ ] **Step 1: Migration oluştur**

```bash
dotnet ef migrations add AddNotificationOutboxAndAdminPush \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 2: Migration dosyasını aç; `Up` metodunun sonuna trigger SQL'i ekle**

```csharp
migrationBuilder.Sql(@"
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
");
```

- [ ] **Step 3: `Down` metodunun BAŞINA drop SQL'i**

```csharp
migrationBuilder.Sql("DROP TRIGGER IF EXISTS outbox_insert_notify ON notification_outbox;");
migrationBuilder.Sql("DROP FUNCTION IF EXISTS notify_outbox_insert();");
```

- [ ] **Step 4: Migration'ı uygula**

```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 5: Senkron doğrula**

```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(notifications): migration — outbox tabloları + NOTIFY trigger"
```

---

## Task 0.7: IEventBus interface

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/IEventBus.cs`

- [ ] **Step 1: Dosya**

```csharp
using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Channels;

public interface IEventBus
{
    /// <summary>
    /// Ephemeral (UI-sync, non-durable) event'leri in-memory channel'a publish eder.
    /// Persistent event'ler için IntegrationDbContext.AddDomainEvent kullanın.
    /// </summary>
    ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = false, CancellationToken ct = default)
        where TEvent : BaseEvent;
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj
git add Application/Entegrasyon.Business/Channels/IEventBus.cs
git commit -m "feat(notifications): IEventBus interface (ephemeral publish)"
```

---

## Task 0.8: NotificationReadEvent + InMemoryEventBus + Unit Test

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationReadEvent.cs`
- Create: `Application/Entegrasyon.Business/Channels/InMemoryEventBus.cs`
- Create: `Test/Entegrasyon.Test/Business/Notifications/InMemoryEventBusTests.cs`

- [ ] **Step 1: NotificationReadEvent**

```csharp
namespace Entegrasyon.Business.Channels.Events.Notifications;

public sealed class NotificationReadEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }

    public NotificationReadEvent() { }
    public NotificationReadEvent(long notificationId, Guid userId, DateTimeOffset readAt)
    {
        NotificationId = notificationId;
        UserId = userId;
        ReadAt = readAt;
    }
}
```

- [ ] **Step 2: Test (TDD — önce test)**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Channels.Events.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.Test.Business.Notifications;

public class InMemoryEventBusTests
{
    private readonly Channel<BaseEvent> _channel =
        Channel.CreateBounded<BaseEvent>(new BoundedChannelOptions(100));
    private readonly Mock<ITenantContext> _tenantContext = new();

    public InMemoryEventBusTests() => _tenantContext.Setup(t => t.TenantId).Returns(42);

    [Fact]
    public async Task PublishAsync_Ephemeral_WritesToChannel()
    {
        var bus = new InMemoryEventBus(_channel, _tenantContext.Object);
        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await bus.PublishAsync(evt, persistent: false);

        var read = await _channel.Reader.ReadAsync();
        read.Should().BeSameAs(evt);
        read.TenantId.Should().Be(42);
    }

    [Fact]
    public async Task PublishAsync_Persistent_Throws()
    {
        var bus = new InMemoryEventBus(_channel, _tenantContext.Object);
        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Func<Task> act = async () => await bus.PublishAsync(evt, persistent: true).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AddDomainEvent*");
    }
}
```

- [ ] **Step 3: Test FAIL doğrula**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~InMemoryEventBusTests"`
Expected: FAIL (`InMemoryEventBus` yok).

- [ ] **Step 4: InMemoryEventBus yaz**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Channels;

public sealed class InMemoryEventBus(
    Channel<BaseEvent> ephemeralChannel,
    ITenantContext tenantContext) : IEventBus
{
    public async ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = false, CancellationToken ct = default)
        where TEvent : BaseEvent
    {
        if (persistent)
        {
            throw new InvalidOperationException(
                "Persistent event'ler IEventBus yerine IntegrationDbContext.AddDomainEvent() ile " +
                "publish edilmelidir (business transaction ile atomiklik için).");
        }

        @event.TenantId = tenantContext.TenantId;
        await ephemeralChannel.Writer.WriteAsync(@event, ct);
    }
}
```

- [ ] **Step 5: Test PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~InMemoryEventBusTests"`
Expected: PASS (2 test).

- [ ] **Step 6: Full baseline**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All green.

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationReadEvent.cs \
        Application/Entegrasyon.Business/Channels/InMemoryEventBus.cs \
        Test/Entegrasyon.Test/Business/Notifications/InMemoryEventBusTests.cs
git commit -m "feat(notifications): NotificationReadEvent + InMemoryEventBus + unit test"
```

---

## Task 0.9: IntegrationDbContext AddDomainEvent buffer

**Files:**
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Notifications/DomainEventBufferTests.cs`

- [ ] **Step 1: Integration test (TDD)**

```csharp
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Notifications;

[Collection(nameof(WebFactoryCollection))]
public class DomainEventBufferTests(WebFactoryFixture fixture)
{
    [Fact]
    public async Task AddDomainEvent_ThenSaveChanges_WritesOutboxRow()
    {
        await using var scope = fixture.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var evt = new ProductAddedEvent(Guid.NewGuid(), "Test Ürün", Guid.NewGuid()) { TenantId = 1 };
        db.AddDomainEvent(evt);

        await db.SaveChangesAsync();

        var row = await db.NotificationOutbox.SingleAsync();
        row.EventType.Should().Be("ProductAddedEvent");
        row.Status.Should().Be(OutboxStatus.Pending);
        row.TenantId.Should().Be(1);
        row.PayloadJson.Should().Contain("Test Ürün");
    }

    [Fact]
    public async Task AddDomainEvent_BufferClearedAfterSave()
    {
        await using var scope = fixture.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        db.AddDomainEvent(new ProductAddedEvent(Guid.NewGuid(), "A", Guid.NewGuid()) { TenantId = 1 });
        await db.SaveChangesAsync();
        await db.SaveChangesAsync();  // ikinci çağrıda yeni outbox satırı yazılmamalı

        (await db.NotificationOutbox.CountAsync()).Should().Be(1);
    }
}
```

- [ ] **Step 2: Test FAIL doğrula** (AddDomainEvent metodu yok)

- [ ] **Step 3: IntegrationDbContext'i aç — buffer field + method**

Sınıfın gövdesine (`DbSet<T>`'lerin yanına):
```csharp
private readonly List<Entegrasyon.Business.Channels.Events.BaseEvent> _pendingEvents = new();

public void AddDomainEvent(Entegrasyon.Business.Channels.Events.BaseEvent evt)
{
    ArgumentNullException.ThrowIfNull(evt);
    _pendingEvents.Add(evt);
}
```

- [ ] **Step 4: SaveChangesAsync override**

Sınıfta zaten UTC dönüşümü yapan override varsa onun **başına** (base.SaveChangesAsync'den ÖNCE) aşağıdaki bloğu ekle. Yoksa yeni override:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    if (_pendingEvents.Count > 0)
    {
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        foreach (var evt in _pendingEvents)
        {
            Set<Entegrasyon.Entity.Notifications.NotificationOutbox>().Add(
                new Entegrasyon.Entity.Notifications.NotificationOutbox
                {
                    EventType = evt.GetType().Name,
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(evt, evt.GetType(), jsonOpts),
                    Status = Entegrasyon.Entity.Notifications.OutboxStatus.Pending,
                    TenantId = evt.TenantId
                });
        }
        _pendingEvents.Clear();
    }

    return await base.SaveChangesAsync(ct);
}
```

- [ ] **Step 5: `ProductAddedEvent`'e `ActorUserId` (Guid) alanı ekle** (test bu şekilde çağırıyor)

```csharp
// Application/Entegrasyon.Business/Channels/Events/Products/ProductAddedEvent.cs
public class ProductAddedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }

    public ProductAddedEvent() { }
    public ProductAddedEvent(Guid productId, string productTitle, Guid actorUserId)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        ActorUserId = actorUserId;
    }
}
```

(Mevcut `ProductAddedEvent` çağrıldığı yerleri de güncelle veya iki constructor bırak.)

- [ ] **Step 6: Test PASS**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~DomainEventBufferTests"`

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs \
        Application/Entegrasyon.Business/Channels/Events/Products/ProductAddedEvent.cs \
        Test/Entegrasyon.IntegrationTest/Notifications/DomainEventBufferTests.cs
git commit -m "feat(notifications): DbContext AddDomainEvent buffer + SaveChanges outbox yazımı"
```

---

## Task 0.10: OutboxDispatchOptions

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/OutboxDispatchOptions.cs`

- [ ] **Step 1: Dosya**

```csharp
namespace Entegrasyon.Business.Channels;

public sealed class OutboxDispatchOptions
{
    public const string SectionName = "Notifications:Outbox";

    public TimeSpan SafetyPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 50;
    public int MaxRetryCount { get; set; } = 5;
    public int MaxConcurrency { get; set; } = 10;
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.Business/Channels/OutboxDispatchOptions.cs
git commit -m "feat(notifications): OutboxDispatchOptions configuration"
```

---

## Task 0.11: IDomainEventHandler interface

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Handlers/IDomainEventHandler.cs`

- [ ] **Step 1: Dosya**

```csharp
using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Notifications.Handlers;

public interface IDomainEventHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.Business/Notifications/Handlers/IDomainEventHandler.cs
git commit -m "feat(notifications): IDomainEventHandler<T> interface"
```

---

## Task 0.12: OutboxDispatcher + Integration Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/OutboxDispatcher.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Notifications/OutboxDispatcherTests.cs`

- [ ] **Step 1: Integration test dosyası (TDD)**

```csharp
using System.Text.Json;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Notifications;

[Collection(nameof(WebFactoryCollection))]
public class OutboxDispatcherTests(WebFactoryFixture fixture)
{
    [Fact]
    public async Task PendingRow_DispatchedToHandler_StatusCompleted()
    {
        var testHandler = new TestProductAddedHandler();
        await using var scope = fixture.CreateScope(services =>
            services.AddSingleton<IDomainEventHandler<ProductAddedEvent>>(testHandler));

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var evt = new ProductAddedEvent(Guid.NewGuid(), "X", Guid.NewGuid()) { TenantId = 1 };
        db.NotificationOutbox.Add(new NotificationOutbox
        {
            EventType = "ProductAddedEvent",
            PayloadJson = JsonSerializer.Serialize(evt),
            Status = OutboxStatus.Pending,
            TenantId = 1
        });
        await db.SaveChangesAsync();

        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await dispatcher.DispatchPendingAsync(cts.Token);

        var row = await db.NotificationOutbox.AsNoTracking().FirstAsync();
        row.Status.Should().Be(OutboxStatus.Completed);
        row.ProcessedAt.Should().NotBeNull();
        testHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task HandlerThrows_StatusBackToPending_RetryCountIncremented()
    {
        var testHandler = new ThrowingProductAddedHandler();
        await using var scope = fixture.CreateScope(services =>
            services.AddSingleton<IDomainEventHandler<ProductAddedEvent>>(testHandler));

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var evt = new ProductAddedEvent(Guid.NewGuid(), "X", Guid.NewGuid()) { TenantId = 1 };
        db.NotificationOutbox.Add(new NotificationOutbox
        {
            EventType = "ProductAddedEvent",
            PayloadJson = JsonSerializer.Serialize(evt),
            Status = OutboxStatus.Pending,
            TenantId = 1
        });
        await db.SaveChangesAsync();

        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        var row = await db.NotificationOutbox.AsNoTracking().FirstAsync();
        row.Status.Should().Be(OutboxStatus.Pending);
        row.RetryCount.Should().Be(1);
        row.NextRetryAt.Should().NotBeNull();
        row.LastError.Should().Contain("test failure");
    }

    [Fact]
    public async Task FifthFailure_MovedToDeadLetter()
    {
        var testHandler = new ThrowingProductAddedHandler();
        await using var scope = fixture.CreateScope(services =>
            services.AddSingleton<IDomainEventHandler<ProductAddedEvent>>(testHandler));

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var evt = new ProductAddedEvent(Guid.NewGuid(), "X", Guid.NewGuid()) { TenantId = 1 };
        db.NotificationOutbox.Add(new NotificationOutbox
        {
            EventType = "ProductAddedEvent",
            PayloadJson = JsonSerializer.Serialize(evt),
            Status = OutboxStatus.Pending,
            TenantId = 1,
            RetryCount = 4
        });
        await db.SaveChangesAsync();

        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        (await db.DeadLetterOutbox.CountAsync()).Should().Be(1);
    }

    private sealed class TestProductAddedHandler : IDomainEventHandler<ProductAddedEvent>
    {
        public int CallCount { get; private set; }
        public Task HandleAsync(ProductAddedEvent @event, CancellationToken ct = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingProductAddedHandler : IDomainEventHandler<ProductAddedEvent>
    {
        public Task HandleAsync(ProductAddedEvent @event, CancellationToken ct = default)
            => throw new InvalidOperationException("test failure");
    }
}
```

- [ ] **Step 2: Test FAIL doğrula** (`OutboxDispatcher` yok)

- [ ] **Step 3: OutboxDispatcher implementation**

```csharp
using System.Text.Json;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Entegrasyon.Business.Channels;

public sealed class OutboxDispatcher(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxDispatchOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxDispatchOptions _opts = options.Value;
    private static readonly Dictionary<string, Type> _eventTypeCache = BuildEventTypeCache();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listen = ListenLoopAsync(stoppingToken);
        var poll = PollLoopAsync(stoppingToken);
        await Task.WhenAny(listen, poll);
    }

    private async Task ListenLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var db = await contextFactory.CreateDbContextAsync(stoppingToken);
                var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                await conn.OpenAsync(stoppingToken);
                conn.Notification += async (_, _) =>
                {
                    try { await DispatchPendingAsync(stoppingToken); }
                    catch (Exception ex) { logger.LogError(ex, "Outbox dispatch (on NOTIFY) failed"); }
                };
                await using (var cmd = new NpgsqlCommand("LISTEN notification_outbox_new;", conn))
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                    await conn.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxDispatcher LISTEN loop failed; restarting in 2s");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task PollLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_opts.SafetyPollingInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await DispatchPendingAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Outbox dispatch (polling) failed"); }
        }
    }

    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var pending = await db.NotificationOutbox
            .FromSqlRaw($@"
                SELECT * FROM notification_outbox
                WHERE status = {(int)OutboxStatus.Pending}
                  AND (next_retry_at IS NULL OR next_retry_at <= NOW())
                ORDER BY created_at
                LIMIT {_opts.BatchSize}
                FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        foreach (var row in pending)
            row.Status = OutboxStatus.Processing;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        using var sem = new SemaphoreSlim(_opts.MaxConcurrency);
        var tasks = pending.Select(async row =>
        {
            await sem.WaitAsync(ct);
            try { await DispatchSingleAsync(row, ct); }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    private async Task DispatchSingleAsync(NotificationOutbox row, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<IntegrationDbContext>>()
            .CreateDbContextAsync(ct);

        try
        {
            if (!_eventTypeCache.TryGetValue(row.EventType, out var evtType))
                throw new InvalidOperationException($"Unknown event type: {row.EventType}");

            var evt = JsonSerializer.Deserialize(row.PayloadJson, evtType)
                ?? throw new InvalidOperationException("Payload deserialize failed");

            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(evtType);
            var handlers = scope.ServiceProvider.GetServices(handlerInterface);
            var method = handlerInterface.GetMethod("HandleAsync")!;

            foreach (var handler in handlers)
                await (Task)method.Invoke(handler, [evt, ct])!;

            var fresh = await db.NotificationOutbox.FindAsync([row.Id], ct)
                ?? throw new InvalidOperationException("Outbox row disappeared");
            fresh.Status = OutboxStatus.Completed;
            fresh.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Outbox dispatch failed for {EventType} row {RowId}", row.EventType, row.Id);
            var fresh = await db.NotificationOutbox.FindAsync([row.Id], ct);
            if (fresh is null) return;

            fresh.RetryCount++;
            fresh.LastError = ex.Message;
            if (fresh.RetryCount >= _opts.MaxRetryCount)
            {
                db.DeadLetterOutbox.Add(new DeadLetterOutbox
                {
                    OriginalOutboxId = fresh.Id,
                    TenantId = fresh.TenantId,
                    EventType = fresh.EventType,
                    PayloadJson = fresh.PayloadJson,
                    FinalError = ex.ToString(),
                    TotalRetryCount = fresh.RetryCount,
                    MovedAt = DateTimeOffset.UtcNow
                });
                fresh.Status = OutboxStatus.Failed;
            }
            else
            {
                fresh.Status = OutboxStatus.Pending;
                fresh.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, fresh.RetryCount));
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static Dictionary<string, Type> BuildEventTypeCache()
    {
        var baseType = typeof(BaseEvent);
        return baseType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t);
    }
}
```

- [ ] **Step 4: DI geçici kayıt**

`ApplicationDependencyExtension.cs` içinde `AddBackgroundServices` (veya uygun metod):
```csharp
services.AddSingleton<OutboxDispatcher>();
services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcher>());
services.Configure<OutboxDispatchOptions>(config.GetSection(OutboxDispatchOptions.SectionName));
```

- [ ] **Step 5: Test PASS**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~OutboxDispatcherTests"
```

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/OutboxDispatcher.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
        Test/Entegrasyon.IntegrationTest/Notifications/OutboxDispatcherTests.cs
git commit -m "feat(notifications): OutboxDispatcher — LISTEN/NOTIFY + retry + dead letter"
```

---

## Task 0.13: InProcessEventDispatcher

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/InProcessEventDispatcher.cs`
- Create: `Test/Entegrasyon.Test/Business/Notifications/InProcessEventDispatcherTests.cs`

- [ ] **Step 1: Unit test**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Entegrasyon.Test.Business.Notifications;

public class InProcessEventDispatcherTests
{
    [Fact]
    public async Task Dispatcher_ReadsChannel_InvokesHandler()
    {
        var channel = Channel.CreateBounded<BaseEvent>(10);
        var handler = new SpyHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<NotificationReadEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var sut = new InProcessEventDispatcher(channel, scopeFactory, NullLogger<InProcessEventDispatcher>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);

        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        await channel.Writer.WriteAsync(evt, cts.Token);
        await Task.Delay(200, cts.Token);
        await sut.StopAsync(CancellationToken.None);

        handler.CallCount.Should().Be(1);
    }

    private sealed class SpyHandler : IDomainEventHandler<NotificationReadEvent>
    {
        public int CallCount { get; private set; }
        public Task HandleAsync(NotificationReadEvent @event, CancellationToken ct = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Test FAIL doğrula**

- [ ] **Step 3: InProcessEventDispatcher**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Notifications.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Channels;

public sealed class InProcessEventDispatcher(
    Channel<BaseEvent> ephemeralChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<InProcessEventDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in ephemeralChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var evtType = evt.GetType();
                var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(evtType);
                var handlers = scope.ServiceProvider.GetServices(handlerInterface);
                var method = handlerInterface.GetMethod("HandleAsync")!;
                foreach (var handler in handlers)
                    await (Task)method.Invoke(handler, [evt, stoppingToken])!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ephemeral dispatch failed for {EventType}", evt.GetType().Name);
            }
        }
    }
}
```

- [ ] **Step 4: Test PASS**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/InProcessEventDispatcher.cs \
        Test/Entegrasyon.Test/Business/Notifications/InProcessEventDispatcherTests.cs
git commit -m "feat(notifications): InProcessEventDispatcher (ephemeral dispatch)"
```

---

## Task 0.14: Full DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- Modify: `Application/Entegrasyon.MVC/appsettings.json`

- [ ] **Step 1: DI — ephemeral channel + bus + InProcessDispatcher**

`AddApplicationDependencies` içinde:
```csharp
services.AddSingleton(_ => System.Threading.Channels.Channel.CreateBounded<Entegrasyon.Business.Channels.Events.BaseEvent>(
    new System.Threading.Channels.BoundedChannelOptions(1000)
    {
        FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    }));

services.AddSingleton<Entegrasyon.Business.Channels.IEventBus, Entegrasyon.Business.Channels.InMemoryEventBus>();
services.AddSingleton<Entegrasyon.Business.Channels.InProcessEventDispatcher>();
services.AddHostedService(sp => sp.GetRequiredService<Entegrasyon.Business.Channels.InProcessEventDispatcher>());
```

- [ ] **Step 2: appsettings.json**

```json
{
  "Notifications": {
    "Outbox": {
      "SafetyPollingInterval": "00:00:05",
      "BatchSize": 50,
      "MaxRetryCount": 5,
      "MaxConcurrency": 10
    }
  }
}
```

- [ ] **Step 3: Uygulama ayağa kalkıyor mu**

```bash
dotnet build
(cd Application/Entegrasyon.MVC && timeout 5 dotnet run || true)
```
Beklenen: `Now listening on: http://localhost:5100`. DI hata yoksa OK.

- [ ] **Step 4: Full test**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```
Beklenen: Tümü yeşil.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
        Application/Entegrasyon.MVC/appsettings.json
git commit -m "feat(notifications): DI — IEventBus, ephemeral channel, dispatcher'lar + config"
```

---

# Phase 1: NotificationsUsers Junction — IsRead/ReadAt

## Task 1.1: NotificationsUsers Entity'ye IsRead/ReadAt

**Files:**
- Modify: `Application/Entegrasyon.Entity/Notifications/NotificationsUsers.cs`

- [ ] **Step 1: Alanları ekle**

```csharp
public sealed class NotificationsUsers : BaseEntity
{
    public long NotificationId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public bool IsDismissed { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.Entity/Notifications/NotificationsUsers.cs
git commit -m "feat(notifications): NotificationsUsers junction'a IsRead/ReadAt"
```

---

## Task 1.2: Migration — Junction kolonları + backfill

- [ ] **Step 1: Migration oluştur**

```bash
dotnet ef migrations add AddReadStateToNotificationsUsers \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 2: `Up` metodunun sonuna backfill SQL**

```csharp
migrationBuilder.Sql(@"
    UPDATE notifications_users nu
    SET is_read = n.is_read, read_at = n.read_at
    FROM notifications n
    WHERE nu.notification_id = n.id;
");
```

- [ ] **Step 3: Uygula + senkron doğrula**

```bash
dotnet ef database update ...
dotnet ef migrations has-pending-model-changes ...
```

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(notifications): migration — junction IsRead/ReadAt + backfill"
```

---

## Task 1.3: NotificationManager — MarkAsRead/MarkAllAsRead/GetNotificationsForUser junction'a taşı

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/NotificationManager.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Notifications/NotificationManagerMultiRecipientTests.cs`

- [ ] **Step 1: Integration test (TDD)**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Notifications;

[Collection(nameof(WebFactoryCollection))]
public class NotificationManagerMultiRecipientTests(WebFactoryFixture fixture)
{
    [Fact]
    public async Task MarkAsRead_OnlyAffectsGivenUser()
    {
        await using var scope = fixture.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<INotificationManager>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.Users.AddRange(
            new ApplicationUser { Id = userA, IsActive = true, UserName = "a" },
            new ApplicationUser { Id = userB, IsActive = true, UserName = "b" });
        await db.SaveChangesAsync();

        await sut.SendNotification("t", "c", NotificationSeverity.Info, NotificationCategory.Sistem,
            [userA, userB]);

        var notif = await db.Notifications.FirstAsync();
        await sut.MarkAsRead(notif.Id, userA);

        var junctions = await db.Set<NotificationsUsers>()
            .Where(x => x.NotificationId == notif.Id).ToListAsync();
        junctions.Single(x => x.ApplicationUserId == userA).IsRead.Should().BeTrue();
        junctions.Single(x => x.ApplicationUserId == userB).IsRead.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Test FAIL doğrula**

- [ ] **Step 3: NotificationManager ctor'una ephemeral channel ekle**

```csharp
public sealed class NotificationManager(
    IEnumerable<INotificationSender> notificationSenders,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    Channel<BaseEvent> ephemeralChannel,
    ITenantContext tenantContext,
    ILogger<NotificationManager> logger) : INotificationManager
```

> Not: Eski `EventChannel<NotificationEvent>` field'ı varsa bu task'ta kaldırılır (mevcut yapıda `eventChannel` ile `NotificationEvent` yazılıyordu; yerine ephemeral channel + `NotificationReadEvent` gelecek).

- [ ] **Step 4: MarkAsRead güncelle**

```csharp
public async Task MarkAsRead(long notificationId, Guid userId)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    var affected = await dbContext.Set<NotificationsUsers>()
        .Where(nu => nu.NotificationId == notificationId && nu.ApplicationUserId == userId && !nu.IsRead)
        .ExecuteUpdateAsync(s => s
            .SetProperty(nu => nu.IsRead, true)
            .SetProperty(nu => nu.ReadAt, DateTimeOffset.UtcNow));

    if (affected > 0)
    {
        await ephemeralChannel.Writer.WriteAsync(
            new Entegrasyon.Business.Channels.Events.Notifications.NotificationReadEvent(
                notificationId, userId, DateTimeOffset.UtcNow)
            {
                TenantId = tenantContext.TenantId
            });
    }
}
```

- [ ] **Step 5: MarkAllAsRead güncelle**

```csharp
public async Task MarkAllAsRead(Guid userId)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    await dbContext.Set<NotificationsUsers>()
        .Where(nu => nu.ApplicationUserId == userId && !nu.IsRead && !nu.IsDismissed)
        .ExecuteUpdateAsync(s => s
            .SetProperty(nu => nu.IsRead, true)
            .SetProperty(nu => nu.ReadAt, DateTimeOffset.UtcNow));
}
```

- [ ] **Step 6: GetNotificationsForUser — `onlyUnread` junction'dan**

```csharp
public async Task<IEnumerable<Notification>> GetNotificationsForUser(
    Guid userId, bool onlyUnread = false, int? take = null)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    var query = dbContext.Notifications
        .Where(n => n.NotificationsUsers.Any(nu => nu.ApplicationUserId == userId && !nu.IsDismissed));

    if (onlyUnread)
        query = query.Where(n => n.NotificationsUsers.Any(
            nu => nu.ApplicationUserId == userId && !nu.IsRead));

    query = query.OrderByDescending(n => n.CreatedAt);
    if (take.HasValue) query = query.Take(take.Value);

    return await query.ToListAsync();
}
```

- [ ] **Step 7: DismissNotification'a ephemeral event publish (NotificationDismissedEvent)**

`NotificationDismissedEvent`'i `Events/Notifications/` altına ekle:
```csharp
public sealed class NotificationDismissedEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public Guid UserId { get; set; }
    public NotificationDismissedEvent() { }
    public NotificationDismissedEvent(long nId, Guid uId) { NotificationId = nId; UserId = uId; }
}
```

Dismiss:
```csharp
public async Task DismissNotification(long notificationId, Guid userId)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    var affected = await dbContext.Set<NotificationsUsers>()
        .Where(nu => nu.NotificationId == notificationId && nu.ApplicationUserId == userId && !nu.IsDismissed)
        .ExecuteUpdateAsync(s => s
            .SetProperty(nu => nu.IsDismissed, true)
            .SetProperty(nu => nu.DismissedAt, DateTimeOffset.UtcNow));

    if (affected > 0)
    {
        await ephemeralChannel.Writer.WriteAsync(
            new Entegrasyon.Business.Channels.Events.Notifications.NotificationDismissedEvent(notificationId, userId)
            {
                TenantId = tenantContext.TenantId
            });
    }
}
```

- [ ] **Step 8: Test PASS**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~NotificationManagerMultiRecipientTests"
```

- [ ] **Step 9: Full test**

```bash
dotnet test
```

- [ ] **Step 10: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/NotificationManager.cs \
        Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationDismissedEvent.cs \
        Test/Entegrasyon.IntegrationTest/Notifications/NotificationManagerMultiRecipientTests.cs
git commit -m "feat(notifications): NotificationManager junction tabanlı read/dismiss + ephemeral publish"
```

---

# Phase 2: Event Tipleri + Handler'lar

## Task 2.1: Yeni event sınıfları — tek commit

**Files:** Her biri ayrı dosya, tek commit.

**Şablon (örn. ProductDeletedEvent):**
```csharp
namespace Entegrasyon.Business.Channels.Events.Products;

public sealed class ProductDeletedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public Guid DeletedByUserId { get; set; }

    public ProductDeletedEvent() { }
    public ProductDeletedEvent(Guid productId, string productTitle, Guid deletedByUserId)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        DeletedByUserId = deletedByUserId;
    }
}
```

**Şablona göre aşağıdaki dosyaları oluştur:**

| Dosya | Alanlar |
|---|---|
| `Events/Products/ProductDeletedEvent.cs` | ProductId(Guid), ProductTitle(string), DeletedByUserId(Guid) |
| `Events/Products/ProductUpdatedEvent.cs` (varsa ActorUserId ekle) | ProductId(Guid), Title(string), ChangedFields(string[]), UpdatedByUserId(Guid) |
| `Events/Categories/CategoryAddedEvent.cs` | CategoryId(int), Name(string), ParentId(int?), CreatedByUserId(Guid) |
| `Events/Categories/CategoryDeletedEvent.cs` | CategoryId(int), Name(string), DeletedByUserId(Guid) |
| `Events/Brands/BrandAddedEvent.cs` | BrandId(int), Name(string), CreatedByUserId(Guid) |
| `Events/Brands/BrandUpdatedEvent.cs` | BrandId(int), Name(string), ChangedFields(string[]), UpdatedByUserId(Guid) |
| `Events/Brands/BrandDeletedEvent.cs` | BrandId(int), Name(string), DeletedByUserId(Guid) |
| `Events/Marketplace/MarketplaceOrderReceivedEvent.cs` | MarketPlaceId(int), OrderId(long), OrderNumber(string), CustomerName(string), Amount(decimal) |
| `Events/Marketplace/MarketplaceProductApprovedEvent.cs` | MarketPlaceId(int), ProductId(Guid), MarketplaceProductCode(string) |
| `Events/Marketplace/MarketplaceProductRejectedEvent.cs` | MarketPlaceId(int), ProductId(Guid), RejectionReason(string) |
| `Events/Marketplace/MarketplaceQuestionAskedEvent.cs` | MarketPlaceId(int), QuestionId(long), ProductId(Guid), QuestionText(string) |
| `Events/Marketplace/MarketplaceStockSyncFailedEvent.cs` | MarketPlaceId(int), ProductId(Guid), Error(string) |
| `Events/Marketplace/MarketplacePriceUpdateFailedEvent.cs` | MarketPlaceId(int), ProductId(Guid), Error(string) |
| `Events/Marketplace/MarketplaceReturnReceivedEvent.cs` | MarketPlaceId(int), ReturnId(long), OrderId(long), Reason(string) |
| `Events/Storefront/StorefrontOrderPlacedEvent.cs` | OrderId(long), CustomerId(int), Total(decimal) |
| `Events/Storefront/StorefrontNewCustomerEvent.cs` | CustomerId(int), Email(string) |
| `Events/Storefront/StorefrontProductQuestionEvent.cs` | QuestionId(long), ProductId(Guid), CustomerId(int) |
| `Events/Storefront/StorefrontReviewSubmittedEvent.cs` | ReviewId(long), ProductId(Guid), Rating(int) |
| `Events/Storefront/StorefrontAbandonedCartEvent.cs` | CartId(long), CustomerId(int), ValueAmount(decimal) |
| `Events/Storefront/StorefrontWalletWithdrawRequestEvent.cs` | CustomerId(int), Amount(decimal) |
| `Events/System/BackgroundJobFailedEvent.cs` | JobName(string), Error(string), RetryCount(int) |
| `Events/System/SyncErrorThresholdExceededEvent.cs` | ServiceName(string), ErrorCount(int), WindowMinutes(int) |
| `Events/System/BranchOfficeApprovalRequestedEvent.cs` | ApprovalId(int), BranchOfficeId(int), RequestedByUserId(Guid) |
| `Events/System/BranchOfficeApprovalApprovedEvent.cs` | ApprovalId(int), BranchOfficeId(int), ApprovedByUserId(Guid) |
| `Events/System/BranchOfficeApprovalRejectedEvent.cs` | ApprovalId(int), BranchOfficeId(int), RejectedByUserId(Guid), Reason(string) |

Her sınıf: `sealed`, `BaseEvent`'ten türer, parameterless + param ctor.

- [ ] **Step 1: Dosyaları yaz**

Tüm event sınıflarını yukarıdaki şablona göre oluştur.

- [ ] **Step 2: `CategoryUpdatedEvent`'e `UpdatedByUserId` alanı ekle** (varsa)

Mevcut sınıfa `Guid UpdatedByUserId` ekle, aktör filtrelemesi için.

- [ ] **Step 3: `ChannelExtensions.cs` — legacy `AddSingleton<EventChannel<T>>` kayıtlarını temizle**

Eski `EventChannel<NotificationEvent>`, `EventChannel<ProductAddedEvent>` vs. **kayıtları kaldırılır** — artık `dbContext.AddDomainEvent` ve `Channel<BaseEvent>` kullanıyoruz. Ama mevcut publisher'lar hala çalışıyor olabilir; bu geçişi kırmamak için `ChannelExtensions`'ı boş bırak veya tamamen sil (`AddEventChannels` çağrısını `ApplicationDependencyExtension`'dan da kaldır).

- [ ] **Step 4: Build**

```bash
dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/Events/ \
        Application/Entegrasyon.Business/Channels/ChannelExtensions.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(notifications): yeni domain event tipleri (24 event) + legacy channel kayıtları temizlendi"
```

---

## Task 2.2: ProductAddedNotificationHandler (şablon) + test

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Handlers/ProductAddedNotificationHandler.cs`
- Create: `Test/Entegrasyon.Test/Business/Notifications/Handlers/ProductAddedNotificationHandlerTests.cs`

- [ ] **Step 1: Unit test**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.Test.Business.Notifications.Handlers;

public class ProductAddedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_ExcludesActor_CallsNotificationManager()
    {
        var actor = Guid.NewGuid();
        var other1 = Guid.NewGuid();
        var other2 = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync([actor, other1, other2]);

        var sut = new ProductAddedNotificationHandler(_notifManager.Object, _resolver.Object);
        var productId = Guid.NewGuid();
        var evt = new ProductAddedEvent(productId, "Test Ürün", actor);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni ürün eklendi",
            It.Is<string>(s => s.Contains("Test Ürün")),
            NotificationSeverity.Info,
            NotificationCategory.Urun,
            It.Is<IEnumerable<Guid>>(ids =>
                ids.Count() == 2 && !ids.Contains(actor) && ids.Contains(other1) && ids.Contains(other2)),
            $"/products/{productId}"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync([actor]);

        var sut = new ProductAddedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new ProductAddedEvent(Guid.NewGuid(), "X", actor));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
```

- [ ] **Step 2: Test FAIL**

- [ ] **Step 3: Handler**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class ProductAddedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<ProductAddedEvent>
{
    public async Task HandleAsync(ProductAddedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("products.view", ct);
        var recipients = all.Where(id => id != @event.ActorUserId).ToList();
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

- [ ] **Step 4: Test PASS + commit**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "ProductAddedNotificationHandler"
git add Application/Entegrasyon.Business/Notifications/Handlers/ProductAddedNotificationHandler.cs \
        Test/Entegrasyon.Test/Business/Notifications/Handlers/ProductAddedNotificationHandlerTests.cs
git commit -m "feat(notifications): ProductAddedNotificationHandler + test (şablon handler)"
```

---

## Task 2.3: Kalan 25 handler — tablo tabanlı

**Files:** Her handler için:
- `Application/Entegrasyon.Business/Notifications/Handlers/{Name}Handler.cs`
- `Test/Entegrasyon.Test/Business/Notifications/Handlers/{Name}HandlerTests.cs`

**Şablon:** Task 2.2'deki handler. Her handler kendi event'i için aynı şablonu uygular; farklılıklar tabloda.

**Handler katalogu:**

| Handler Adı | Event Tipi | Permission | Header | Content Template | Severity | ActionUrl Template | ActorId Property |
|---|---|---|---|---|---|---|---|
| `ProductUpdatedNotificationHandler` | `ProductUpdatedEvent` | `products.view` | "Ürün güncellendi" | `$"'{@event.Title}' güncellendi."` | Info | `$"/products/{@event.ProductId}"` | `UpdatedByUserId` |
| `ProductDeletedNotificationHandler` | `ProductDeletedEvent` | `products.view` | "Ürün silindi" | `$"'{@event.ProductTitle}' silindi."` | Warning | `null` | `DeletedByUserId` |
| `CategoryAddedNotificationHandler` | `CategoryAddedEvent` | `categories.view` | "Yeni kategori" | `$"'{@event.Name}' kategorisi eklendi."` | Info | `$"/categories/{@event.CategoryId}"` | `CreatedByUserId` |
| `CategoryUpdatedNotificationHandler` | `CategoryUpdatedEvent` | `categories.view` | "Kategori güncellendi" | `$"'{@event.Name}' güncellendi."` | Info | `$"/categories/{@event.CategoryId}"` | `UpdatedByUserId` |
| `CategoryDeletedNotificationHandler` | `CategoryDeletedEvent` | `categories.view` | "Kategori silindi" | `$"'{@event.Name}' silindi."` | Warning | `null` | `DeletedByUserId` |
| `BrandAddedNotificationHandler` | `BrandAddedEvent` | `brands.view` | "Yeni marka" | `$"'{@event.Name}' markası eklendi."` | Info | `$"/brands/{@event.BrandId}"` | `CreatedByUserId` |
| `BrandUpdatedNotificationHandler` | `BrandUpdatedEvent` | `brands.view` | "Marka güncellendi" | `$"'{@event.Name}' güncellendi."` | Info | `$"/brands/{@event.BrandId}"` | `UpdatedByUserId` |
| `BrandDeletedNotificationHandler` | `BrandDeletedEvent` | `brands.view` | "Marka silindi" | `$"'{@event.Name}' silindi."` | Warning | `null` | `DeletedByUserId` |
| `MarketplaceOrderReceivedNotificationHandler` | `MarketplaceOrderReceivedEvent` | `orders.view` | "Yeni sipariş" | `$"Sipariş #{@event.OrderNumber} — {@event.CustomerName} — {@event.Amount:C}"` | Error | `$"/orders/{@event.OrderId}"` | — (yok) |
| `MarketplaceProductApprovedNotificationHandler` | `MarketplaceProductApprovedEvent` | `marketplace.manage` | "Ürün onaylandı" | `"Pazaryerinde ürün onaylandı."` | Info | `$"/products/{@event.ProductId}"` | — |
| `MarketplaceProductRejectedNotificationHandler` | `MarketplaceProductRejectedEvent` | `marketplace.manage` | "Ürün reddedildi" | `$"Ürün reddedildi: {@event.RejectionReason}"` | Error | `$"/products/{@event.ProductId}"` | — |
| `MarketplaceQuestionAskedNotificationHandler` | `MarketplaceQuestionAskedEvent` | `marketplace.manage` | "Yeni müşteri sorusu" | `@event.QuestionText.Length > 100 ? @event.QuestionText[..100] + "..." : @event.QuestionText` | Warning | `$"/marketplace/{@event.MarketPlaceId}/questions/{@event.QuestionId}"` | — |
| `MarketplaceStockSyncFailedNotificationHandler` | `MarketplaceStockSyncFailedEvent` | `marketplace.manage` | "Stok sync başarısız" | `$"Ürün stok güncellenemedi: {@event.Error}"` | Warning | `$"/products/{@event.ProductId}"` | — |
| `MarketplacePriceUpdateFailedNotificationHandler` | `MarketplacePriceUpdateFailedEvent` | `marketplace.manage` | "Fiyat güncellemesi başarısız" | `$"Ürün fiyatı güncellenemedi: {@event.Error}"` | Warning | `$"/products/{@event.ProductId}"` | — |
| `MarketplaceReturnReceivedNotificationHandler` | `MarketplaceReturnReceivedEvent` | `orders.view` | "İade talebi" | `$"Sipariş #{@event.OrderId} için iade: {@event.Reason}"` | Warning | `$"/orders/{@event.OrderId}"` | — |
| `StorefrontOrderPlacedNotificationHandler` | `StorefrontOrderPlacedEvent` | `storefront.orders.view` | "Mağaza siparişi" | `$"Yeni sipariş: #{@event.OrderId} — {@event.Total:C}"` | Error | `$"/storefront/orders/{@event.OrderId}"` | — |
| `StorefrontNewCustomerNotificationHandler` | `StorefrontNewCustomerEvent` | `storefront.customers.view` | "Yeni müşteri" | `$"Yeni kayıt: {@event.Email}"` | Info | `$"/storefront/customers/{@event.CustomerId}"` | — |
| `StorefrontProductQuestionNotificationHandler` | `StorefrontProductQuestionEvent` | `storefront.questions.view` | "Mağaza sorusu" | `"Ürüne soru soruldu."` | Warning | `$"/storefront/questions/{@event.QuestionId}"` | — |
| `StorefrontReviewSubmittedNotificationHandler` | `StorefrontReviewSubmittedEvent` | `storefront.reviews.view` | "Yeni ürün değerlendirmesi" | `$"{@event.Rating} yıldızlı değerlendirme geldi."` | Info | `$"/storefront/reviews/{@event.ReviewId}"` | — |
| `StorefrontAbandonedCartNotificationHandler` | `StorefrontAbandonedCartEvent` | `storefront.orders.view` | "Terk edilmiş sepet" | `$"Müşteri sepeti bıraktı: {@event.ValueAmount:C}"` | Info | `$"/storefront/carts/{@event.CartId}"` | — |
| `StorefrontWalletWithdrawRequestNotificationHandler` | `StorefrontWalletWithdrawRequestEvent` | `storefront.wallet.manage` | "Cüzdan çekim talebi" | `$"Müşteri {@event.Amount:C} çekim talebinde bulundu."` | Warning | `$"/storefront/wallet/requests/{@event.CustomerId}"` | — |
| `BackgroundJobFailedNotificationHandler` | `BackgroundJobFailedEvent` | `admin.system.monitor` | "Arka plan işi başarısız" | `$"{@event.JobName} işi {@event.RetryCount}. denemede başarısız: {@event.Error}"` | Warning | `$"/admin/system/logs?jobName={@event.JobName}"` | — |
| `SyncErrorThresholdExceededNotificationHandler` | `SyncErrorThresholdExceededEvent` | `admin.system.monitor` | "Sync hata eşiği aşıldı" | `$"{@event.ServiceName} son {@event.WindowMinutes} dk'da {@event.ErrorCount} hata verdi."` | Error | `"/admin/system/sync"` | — |
| `BranchOfficeApprovalRequestedNotificationHandler` | `BranchOfficeApprovalRequestedEvent` | `branchoffice.approve` | "Şube onayı talebi" | `"Yeni şube onayı bekliyor."` | Warning | `$"/admin/branch-offices/approvals/{@event.ApprovalId}"` | `RequestedByUserId` |
| `BranchOfficeApprovalApprovedNotificationHandler` | `BranchOfficeApprovalApprovedEvent` | `branchoffice.view` | "Şube onaylandı" | `"Şube onaylandı."` | Info | `$"/admin/branch-offices/{@event.BranchOfficeId}"` | `ApprovedByUserId` |
| `BranchOfficeApprovalRejectedNotificationHandler` | `BranchOfficeApprovalRejectedEvent` | `branchoffice.view` | "Şube onayı reddedildi" | `$"Şube onayı reddedildi: {@event.Reason}"` | Warning | `$"/admin/branch-offices/{@event.BranchOfficeId}"` | `RejectedByUserId` |

**Category kullanımı:** Product/Category/Brand → `NotificationCategory.Urun`; Marketplace → `NotificationCategory.Pazaryeri` (yoksa enum'a ekle); Storefront → `NotificationCategory.Magaza` (yoksa ekle); System → `NotificationCategory.Sistem`.

- [ ] **Step 1: `NotificationCategory` enum'a eksik değerleri ekle**

```csharp
public enum NotificationCategory
{
    Sistem = 0,
    Urun = 1,
    Pazaryeri = 2,
    Magaza = 3
}
```

- [ ] **Step 2: Her handler için (sırayla):**
  - Test dosyası yaz (Task 2.2 şablonu + tablo verileri)
  - Test FAIL doğrula
  - Handler dosyasını yaz
  - Test PASS doğrula
  - Tek commit: `feat(notifications): {HandlerName} + test`

- [ ] **Step 3: Tüm handler testleri yeşil**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~NotificationHandler"
```

- [ ] **Step 4: Handler auto-registration (Scrutor)**

```bash
dotnet add Application/Entegrasyon.ApplicationBootstrap package Scrutor
```

`ApplicationDependencyExtension.cs`:
```csharp
services.Scan(scan => scan.FromAssemblyOf<Entegrasyon.Business.Notifications.Handlers.ProductAddedNotificationHandler>()
    .AddClasses(c => c.AssignableTo(typeof(Entegrasyon.Business.Notifications.Handlers.IDomainEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

- [ ] **Step 5: Final commit**

```bash
git add Application/Entegrasyon.Business/Notifications/Handlers/ \
        Application/Entegrasyon.Entity/Notifications/NotificationCategory.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(notifications): kalan 25 domain event handler + Scrutor auto-registration"
```

---

## Task 2.4: End-to-end pipeline integration test

**Files:**
- Create: `Test/Entegrasyon.IntegrationTest/Notifications/DomainEventPipelineTests.cs`

- [ ] **Step 1: Test**

```csharp
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Notifications;

[Collection(nameof(WebFactoryCollection))]
public class DomainEventPipelineTests(WebFactoryFixture fixture)
{
    [Fact]
    public async Task AddProductEvent_DispatcherRuns_NotificationCreated()
    {
        await using var scope = fixture.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        // Kullanıcı + permission setup
        var otherUser = Guid.NewGuid();
        var actor = Guid.NewGuid();
        db.Users.AddRange(
            new ApplicationUser { Id = actor, IsActive = true, UserName = "actor" },
            new ApplicationUser { Id = otherUser, IsActive = true, UserName = "other" });
        db.Set<UsersClaims>().Add(new UsersClaims
        {
            ApplicationUserId = otherUser,
            Permission = "products.view"
        });
        await db.SaveChangesAsync();

        // Event ekle
        db.AddDomainEvent(new ProductAddedEvent(Guid.NewGuid(), "Test", actor) { TenantId = 1 });
        await db.SaveChangesAsync();

        // Dispatch
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // Assert
        var notif = await db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Header == "Yeni ürün eklendi");
        notif.Should().NotBeNull();
        var junctions = await db.Set<Entegrasyon.Entity.Notifications.NotificationsUsers>()
            .Where(nu => nu.NotificationId == notif!.Id).ToListAsync();
        junctions.Should().ContainSingle(nu => nu.ApplicationUserId == otherUser);
        junctions.Should().NotContain(nu => nu.ApplicationUserId == actor);
    }
}
```

- [ ] **Step 2: Test PASS + commit**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "DomainEventPipelineTests"
git add Test/Entegrasyon.IntegrationTest/Notifications/DomainEventPipelineTests.cs
git commit -m "test(notifications): end-to-end pipeline integration test"
```

---

# Phase 3: Publish Points

## Task 3.1: Feature flag + ICurrentUserContext infrastructure

**Files:**
- Create: `Application/Entegrasyon.Business/Options/NotificationFeatureFlags.cs`
- Create: `Application/Entegrasyon.Business/Abstract/ICurrentUserContext.cs`
- Create: `Application/Entegrasyon.MVC/Infrastructure/CurrentUserContext.cs`
- Modify: `Application/Entegrasyon.MVC/appsettings.json`

- [ ] **Step 1: NotificationFeatureFlags**

```csharp
namespace Entegrasyon.Business.Options;

public sealed class NotificationFeatureFlags
{
    public const string SectionName = "Features:NotificationsV2";
    public bool PublishEnabled { get; set; }
    public bool SseEnabled { get; set; }
    public bool WebPushEnabled { get; set; }
}
```

- [ ] **Step 2: ICurrentUserContext**

```csharp
namespace Entegrasyon.Business.Abstract;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
}
```

- [ ] **Step 3: CurrentUserContext**

```csharp
using System.Security.Claims;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Infrastructure;

public sealed class CurrentUserContext(IHttpContextAccessor http) : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            var val = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }
}
```

- [ ] **Step 4: DI**

```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserContext, CurrentUserContext>();
services.Configure<NotificationFeatureFlags>(config.GetSection(NotificationFeatureFlags.SectionName));
```

- [ ] **Step 5: appsettings.json**

```json
{
  "Features": {
    "NotificationsV2": {
      "PublishEnabled": false,
      "SseEnabled": false,
      "WebPushEnabled": false
    }
  }
}
```

- [ ] **Step 6: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.Business/Options/NotificationFeatureFlags.cs \
        Application/Entegrasyon.Business/Abstract/ICurrentUserContext.cs \
        Application/Entegrasyon.MVC/Infrastructure/CurrentUserContext.cs \
        Application/Entegrasyon.MVC/appsettings.json \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(notifications): feature flag + ICurrentUserContext infrastructure"
```

---

## Task 3.2: ProductManager publish points

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`

- [ ] **Step 1: ctor'a inject et**

`IOptions<NotificationFeatureFlags> flags`, `ICurrentUserContext currentUser`.

- [ ] **Step 2: AddProductAsync — SaveChanges'ten ÖNCE**

```csharp
if (flags.Value.PublishEnabled)
{
    dbContext.AddDomainEvent(new ProductAddedEvent(
        product.Id, product.Title, currentUser.UserId ?? Guid.Empty));
}
```

- [ ] **Step 3: UpdateProductAsync — aynı pattern**

```csharp
if (flags.Value.PublishEnabled)
{
    dbContext.AddDomainEvent(new ProductUpdatedEvent(
        product.Id, product.Title, changedFields.ToArray(), currentUser.UserId ?? Guid.Empty));
}
```

> `changedFields` mevcut değilse yaklaşık: EF change tracker'dan `dbContext.Entry(product).Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToArray()`.

- [ ] **Step 4: DeleteProductAsync (soft veya hard delete sonrası)**

```csharp
if (flags.Value.PublishEnabled)
{
    dbContext.AddDomainEvent(new ProductDeletedEvent(
        productId, productTitle, currentUser.UserId ?? Guid.Empty));
}
```

- [ ] **Step 5: Mevcut ProductManager testlerini çalıştır** (flag OFF default, değişiklik olmamalı)

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductManager"
```

- [ ] **Step 6: Yeni test — flag ON iken event eklendi mi**

Test kodu — unit test (DbContext mock veya InMemory provider yerine integration test daha uygun, ama basit unit ile de tutulabilir):

```csharp
[Fact]
public async Task AddProduct_FlagEnabled_AddsDomainEvent()
{
    // Arrange: Mock DbContext veya integration test + flag ON
    // Act: ProductManager.AddProductAsync(...)
    // Assert: dbContext.NotificationOutbox.Count == 1 && EventType == "ProductAddedEvent"
}
```

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ProductManager.cs \
        Test/Entegrasyon.Test/Business/ProductManagerTests.cs
git commit -m "feat(notifications): ProductManager publish points (flag arkasında)"
```

---

## Task 3.3: CategoryManager publish points

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`

Task 3.2 şablonunu uygula — Add/Update/Delete metodları:
- `CategoryAddedEvent(id, name, parentId, currentUser.UserId)`
- `CategoryUpdatedEvent(id, name, changedFields, currentUser.UserId)`
- `CategoryDeletedEvent(id, name, currentUser.UserId)`

- [ ] **Step 1: Kodu ekle**
- [ ] **Step 2: Test (mevcutlar + yeni flag-on test)**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat(notifications): CategoryManager publish points"
```

---

## Task 3.4: BrandManager publish points

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/BrandManager.cs`

Aynı şablon. Commit:
```bash
git commit -m "feat(notifications): BrandManager publish points"
```

---

## Task 3.5: Marketplace background service publish points

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/Marketplaces/Trendyol/TrendyolOrderSyncBackgroundService.cs` (varsa)
- Modify: Her marketplace'in OrderSync, QuestionSync, StatusSync, StockSync, PriceSync, ReturnSync bg servisi

Her servis içinde "yeni kayıt tespit edildi" noktasına (DB'ye save eden yere) publish:

```csharp
if (flags.Value.PublishEnabled)
{
    dbContext.AddDomainEvent(new MarketplaceOrderReceivedEvent
    {
        MarketPlaceId = 1,  // Trendyol için
        OrderId = savedOrder.Id,
        OrderNumber = externalOrder.OrderNumber,
        CustomerName = externalOrder.CustomerName,
        Amount = externalOrder.TotalPrice
    });
}
```

Benzer şekilde:
- QuestionSync → `MarketplaceQuestionAskedEvent`
- StatusSync → yeni onay/red için `MarketplaceProductApprovedEvent` / `MarketplaceProductRejectedEvent`
- StockSync (fail) → `MarketplaceStockSyncFailedEvent`
- PriceSync (fail) → `MarketplacePriceUpdateFailedEvent`
- ReturnSync → `MarketplaceReturnReceivedEvent`

- [ ] **Step 1: Her servise publish ekle**
- [ ] **Step 2: Build + test** (mevcut testler)
- [ ] **Step 3: Commit** (her marketplace ayrı commit olabilir)

```bash
git commit -m "feat(notifications): marketplace background services publish points"
```

---

## Task 3.6: Storefront controller publish points

**Files:**
- Modify: Storefront altındaki Order, Auth (new customer), Question, Review, Wallet, Cart (abandoned) controller/manager'ları

Her başarılı işlem noktasında:
```csharp
if (flags.Value.PublishEnabled)
{
    dbContext.AddDomainEvent(new StorefrontOrderPlacedEvent(order.Id, order.CustomerId, order.Total));
}
```

- [ ] **Step 1: Her noktaya publish ekle**
- [ ] **Step 2: Commit**

```bash
git commit -m "feat(notifications): storefront publish points"
```

---

## Task 3.7: System event publish points

**Files:**
- Modify: `BranchOfficeApprovalManager` (varsa)
- Create/Modify: Background job wrapper (yoksa `JobExecutionWrapper` ile hata yakalama katmanı)

- [ ] **Step 1: BranchOfficeApprovalManager**

```csharp
// RequestApproval:
dbContext.AddDomainEvent(new BranchOfficeApprovalRequestedEvent(
    approval.Id, branchOfficeId, currentUser.UserId ?? Guid.Empty));

// Approve:
dbContext.AddDomainEvent(new BranchOfficeApprovalApprovedEvent(
    approval.Id, approval.BranchOfficeId, currentUser.UserId ?? Guid.Empty));

// Reject:
dbContext.AddDomainEvent(new BranchOfficeApprovalRejectedEvent(
    approval.Id, approval.BranchOfficeId, currentUser.UserId ?? Guid.Empty, reason));
```

- [ ] **Step 2: Background job failure**

Ortak bir wrapper yoksa, geçerli her `BackgroundService`'te `try-catch` içinde hata yakalandığında:
```csharp
catch (Exception ex)
{
    // log + event
    await using var db = await contextFactory.CreateDbContextAsync(ct);
    db.AddDomainEvent(new BackgroundJobFailedEvent(
        nameof(MyJob), ex.Message, retryCount) { TenantId = tenantId });
    await db.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(notifications): system event publish points (approvals + job failures)"
```

---

# Phase 4: SSE Transport + Notification UI

## Task 4.1: SseNotificationPayload

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Sse/SseNotificationPayload.cs`

- [ ] **Step 1: Dosya**

```csharp
namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseNotificationPayload
{
    public string EventType { get; set; } = "notification";
    public long? NotificationId { get; set; }
    public string? Header { get; set; }
    public string? Content { get; set; }
    public string? Severity { get; set; }
    public string? Category { get; set; }
    public string? ActionUrl { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
```

- [ ] **Step 2: Build + commit**

```bash
dotnet build
git add Application/Entegrasyon.Business/Notifications/Sse/SseNotificationPayload.cs
git commit -m "feat(notifications): SseNotificationPayload"
```

---

## Task 4.2: ISseConnectionRegistry + SseConnectionRegistry + Unit Test

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Sse/ISseConnectionRegistry.cs`
- Create: `Application/Entegrasyon.Business/Notifications/Sse/SseConnectionRegistry.cs`
- Create: `Test/Entegrasyon.Test/Business/Notifications/SseConnectionRegistryTests.cs`

- [ ] **Step 1: Unit test (TDD)**

```csharp
using System.Threading.Channels;
using Entegrasyon.Business.Notifications.Sse;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.Test.Business.Notifications;

public class SseConnectionRegistryTests
{
    [Fact]
    public void Register_ThenGetChannels_ReturnsRegistered()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        var ch1 = Channel.CreateBounded<SseNotificationPayload>(10);
        var ch2 = Channel.CreateBounded<SseNotificationPayload>(10);

        sut.Register(userId, ch1);
        sut.Register(userId, ch2);

        sut.GetChannels(userId).Should().HaveCount(2);
    }

    [Fact]
    public void Unregister_RemovesChannel()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        var ch = Channel.CreateBounded<SseNotificationPayload>(10);
        var id = sut.Register(userId, ch);

        sut.Unregister(userId, id);

        sut.GetChannels(userId).Should().BeEmpty();
    }

    [Fact]
    public void GetActiveConnectionCount_ReflectsRegistrations()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        sut.Register(userId, Channel.CreateBounded<SseNotificationPayload>(10));
        sut.GetActiveConnectionCount(userId).Should().Be(1);
    }
}
```

- [ ] **Step 2: Interface**

```csharp
using System.Threading.Channels;

namespace Entegrasyon.Business.Notifications.Sse;

public interface ISseConnectionRegistry
{
    Guid Register(Guid userId, Channel<SseNotificationPayload> channel);
    void Unregister(Guid userId, Guid connectionId);
    IReadOnlyList<Channel<SseNotificationPayload>> GetChannels(Guid userId);
    int GetActiveConnectionCount(Guid userId);
}
```

- [ ] **Step 3: Implementation**

```csharp
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseConnectionRegistry : ISseConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<SseNotificationPayload>>> _byUser = new();

    public Guid Register(Guid userId, Channel<SseNotificationPayload> channel)
    {
        var connId = Guid.NewGuid();
        var map = _byUser.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<SseNotificationPayload>>());
        map[connId] = channel;
        return connId;
    }

    public void Unregister(Guid userId, Guid connectionId)
    {
        if (_byUser.TryGetValue(userId, out var map))
        {
            map.TryRemove(connectionId, out _);
            if (map.IsEmpty) _byUser.TryRemove(userId, out _);
        }
    }

    public IReadOnlyList<Channel<SseNotificationPayload>> GetChannels(Guid userId)
        => _byUser.TryGetValue(userId, out var map) ? map.Values.ToList() : [];

    public int GetActiveConnectionCount(Guid userId)
        => _byUser.TryGetValue(userId, out var map) ? map.Count : 0;
}
```

- [ ] **Step 4: DI (singleton)**

```csharp
services.AddSingleton<ISseConnectionRegistry, SseConnectionRegistry>();
```

- [ ] **Step 5: Test PASS + commit**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "SseConnectionRegistry"
git add Application/Entegrasyon.Business/Notifications/Sse/ \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
        Test/Entegrasyon.Test/Business/Notifications/SseConnectionRegistryTests.cs
git commit -m "feat(notifications): SseConnectionRegistry + test + DI"
```

---

## Task 4.3: SseNotificationSender (INotificationSender)

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Sse/SseNotificationSender.cs`
- Modify: `Application/Entegrasyon.Business/Notifications/SenderType.cs`

- [ ] **Step 1: SenderType'a Sse ekle**

```csharp
public enum SenderType
{
    SignalR,   // Phase 6'da kaldırılacak
    Email,
    Sms,
    Sse,
    WebPushAdmin
}
```

- [ ] **Step 2: Sender**

```csharp
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseNotificationSender(ISseConnectionRegistry registry) : INotificationSender
{
    public SenderType Type => SenderType.Sse;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            var payload = new SseNotificationPayload
            {
                EventType = "notification",
                NotificationId = message.Id,
                Header = message.Header,
                Content = message.Content,
                Severity = message.Severity.ToString(),
                Category = message.Category.ToString(),
                ActionUrl = message.ActionUrl,
                CreatedAt = message.CreatedAt
            };
            foreach (var ch in registry.GetChannels(userId))
                ch.Writer.TryWrite(payload);  // non-blocking; slow consumer drop
        }
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: DI**

```csharp
services.AddScoped<INotificationSender, SseNotificationSender>();
```

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Notifications/Sse/SseNotificationSender.cs \
        Application/Entegrasyon.Business/Notifications/SenderType.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(notifications): SseNotificationSender + SenderType.Sse"
```

---

## Task 4.4: SSE Controller Endpoint

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Notifications/SseController.cs`

- [ ] **Step 1: Controller**

```csharp
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Channels;
using Entegrasyon.Business.Notifications.Sse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public sealed class SseController(ISseConnectionRegistry registry) : Controller
{
    [HttpGet("/events/notifications")]
    public IResult StreamNotifications(HttpContext httpContext)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return TypedResults.ServerSentEvents(Stream(userId, httpContext.RequestAborted));
    }

    private async IAsyncEnumerable<SseItem<SseNotificationPayload>> Stream(
        Guid userId, [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<SseNotificationPayload>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        var connId = registry.Register(userId, channel);
        try
        {
            using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(25));
            var heartbeatTask = RunHeartbeatAsync(heartbeat, channel, ct);

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
            registry.Unregister(userId, connId);
        }
    }

    private static async Task RunHeartbeatAsync(PeriodicTimer timer, Channel<SseNotificationPayload> channel, CancellationToken ct)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                channel.Writer.TryWrite(new SseNotificationPayload { EventType = "heartbeat" });
        }
        catch (OperationCanceledException) { }
    }
}
```

> **Not:** `.NET 10` SSE API (`TypedResults.ServerSentEvents`, `SseItem<T>`). `dotnet --version` ile 10.0+ olduğundan emin ol.

- [ ] **Step 2: Build**

```bash
dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Notifications/SseController.cs
git commit -m "feat(notifications): SSE endpoint /events/notifications"
```

---

## Task 4.5: notifications-client.js (innerHTML-free, safe DOM)

**Files:**
- Create: `Application/Entegrasyon.MVC/wwwroot/js/notifications-client.js`

- [ ] **Step 1: Dosya (safe DOM methods only, no innerHTML)**

```javascript
(function () {
    'use strict';

    const bellButton = document.querySelector('[data-notification-bell]');
    const badgeEl = document.querySelector('[data-notification-badge]');
    const dropdownList = document.querySelector('[data-notification-dropdown-list]');

    if (!bellButton) return;

    let unreadCount = parseInt(bellButton.dataset.unreadCount || '0', 10);

    function setUnreadCount(n) {
        unreadCount = Math.max(0, n);
        if (!badgeEl) return;
        if (unreadCount === 0) {
            badgeEl.classList.add('d-none');
            badgeEl.textContent = '';
        } else {
            badgeEl.classList.remove('d-none');
            badgeEl.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
        }
    }

    function severityColor(s) {
        if (s === 'Error') return 'red';
        if (s === 'Warning') return 'yellow';
        if (s === 'Info') return 'blue';
        return 'secondary';
    }

    function severityToNotyfType(s) {
        if (s === 'Error') return 'error';
        if (s === 'Warning') return 'warning';
        return 'success';
    }

    function getAntiForgeryToken() {
        const t = document.querySelector('input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    // Build list item using createElement + textContent (NO innerHTML)
    function buildListItem(n) {
        const a = document.createElement('a');
        a.href = n.actionUrl || '/notifications';
        a.className = 'dropdown-item';
        a.setAttribute('data-notification-id', String(n.notificationId));

        const row = document.createElement('div');
        row.className = 'row';

        const colAuto = document.createElement('div');
        colAuto.className = 'col-auto';
        const dot = document.createElement('span');
        dot.className = 'status-dot status-dot-animated bg-' + severityColor(n.severity);
        colAuto.appendChild(dot);

        const col = document.createElement('div');
        col.className = 'col text-truncate';
        const strong = document.createElement('strong');
        strong.textContent = n.header || '';
        const small = document.createElement('div');
        small.className = 'text-secondary small text-truncate';
        small.textContent = n.content || '';
        col.appendChild(strong);
        col.appendChild(small);

        row.appendChild(colAuto);
        row.appendChild(col);
        a.appendChild(row);

        a.addEventListener('click', function () {
            fetch('/notifications/' + encodeURIComponent(n.notificationId) + '/read', {
                method: 'POST',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
        });

        return a;
    }

    function prependToDropdown(n) {
        if (!dropdownList) return;
        const empty = dropdownList.querySelector('[data-empty-state]');
        if (empty) empty.remove();
        const item = buildListItem(n);
        dropdownList.insertBefore(item, dropdownList.firstChild);
    }

    function removeFromDropdown(notificationId) {
        if (!dropdownList) return;
        const el = dropdownList.querySelector('[data-notification-id="' + CSS.escape(String(notificationId)) + '"]');
        if (el) el.remove();
    }

    function showToast(n) {
        if (window.notyf) {
            window.notyf.open({
                type: severityToNotyfType(n.severity),
                message: (n.header || '') + ': ' + (n.content || '')
            });
        }
    }

    function connect() {
        const es = new EventSource('/events/notifications');
        es.addEventListener('notification', function (e) {
            const data = JSON.parse(e.data);
            setUnreadCount(unreadCount + 1);
            prependToDropdown(data);
            showToast(data);
        });
        es.addEventListener('notification.read', function (e) {
            const data = JSON.parse(e.data);
            setUnreadCount(unreadCount - 1);
            removeFromDropdown(data.notificationId);
        });
        es.addEventListener('notification.dismissed', function (e) {
            const data = JSON.parse(e.data);
            removeFromDropdown(data.notificationId);
        });
        es.onerror = function () {
            // browser auto-reconnects
        };
    }

    document.addEventListener('DOMContentLoaded', connect);
})();
```

- [ ] **Step 2: Layout'a ekle**

`Application/Entegrasyon.MVC/Views/Shared/_Layout.cshtml` `</body>` öncesine:
```html
<script src="~/js/notifications-client.js" asp-append-version="true" defer></script>
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/js/notifications-client.js \
        Application/Entegrasyon.MVC/Views/Shared/_Layout.cshtml
git commit -m "feat(notifications): SSE client (safe DOM, innerHTML-free) + layout entegrasyonu"
```

---

## Task 4.6: Zil Dropdown Partial

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Notifications/Views/Partials/_NotificationBell.cshtml`
- Modify: `Application/Entegrasyon.MVC/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Önce Tabler dropdown + badge doc'unu kontrol et**

https://tabler.io/docs/ui/dropdown ve https://tabler.io/docs/ui/badge

- [ ] **Step 2: Partial view**

```razor
@using Entegrasyon.Business.Abstract
@using Entegrasyon.Entity.Notifications
@using System.Security.Claims
@inject INotificationManager NotificationManager

@{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var notifications = (await NotificationManager.GetNotificationsForUser(userId, onlyUnread: true, take: 10)).ToList();
    var unreadCount = notifications.Count;
}

<div class="nav-item dropdown">
    <a href="#" class="nav-link px-0" data-bs-toggle="dropdown"
       data-notification-bell data-unread-count="@unreadCount" aria-label="Bildirimler">
        <svg xmlns="http://www.w3.org/2000/svg" class="icon" width="24" height="24" viewBox="0 0 24 24"
             stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10 5a2 2 0 1 1 4 0a7 7 0 0 1 4 6v3a4 4 0 0 0 2 3h-16a4 4 0 0 0 2 -3v-3a7 7 0 0 1 4 -6"></path>
            <path d="M9 17v1a3 3 0 0 0 6 0v-1"></path>
        </svg>
        <span data-notification-badge class="badge bg-red @(unreadCount == 0 ? "d-none" : "")">
            @(unreadCount > 99 ? "99+" : unreadCount.ToString())
        </span>
    </a>
    <div class="dropdown-menu dropdown-menu-end dropdown-menu-card" style="width: 400px;">
        <div class="card">
            <div class="card-header d-flex align-items-center">
                <h3 class="card-title">Bildirimler</h3>
                @if (unreadCount > 0)
                {
                    <form asp-controller="Notification" asp-action="MarkAllAsRead" method="post" class="ms-auto">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-sm btn-ghost-primary">Tümünü okundu işaretle</button>
                    </form>
                }
            </div>
            <div class="list-group list-group-flush list-group-hoverable" data-notification-dropdown-list
                 style="max-height: 400px; overflow-y: auto;">
                @if (notifications.Count == 0)
                {
                    <div class="list-group-item text-center text-secondary" data-empty-state>
                        Henüz bildiriminiz yok
                    </div>
                }
                else
                {
                    @foreach (var n in notifications)
                    {
                        <a href="@(n.ActionUrl ?? "/notifications")" class="list-group-item list-group-item-action"
                           data-notification-id="@n.Id">
                            <div class="row align-items-center">
                                <div class="col-auto">
                                    <span class="status-dot bg-@SeverityColor(n.Severity)"></span>
                                </div>
                                <div class="col text-truncate">
                                    <strong>@n.Header</strong>
                                    <div class="text-secondary small text-truncate">@n.Content</div>
                                    <div class="text-secondary text-muted small">@n.CreatedAt.ToString("dd MMM HH:mm")</div>
                                </div>
                            </div>
                        </a>
                    }
                }
            </div>
            <div class="card-footer text-center">
                <a href="/notifications" class="btn btn-link">Tümünü gör</a>
            </div>
        </div>
    </div>
</div>

@functions {
    string SeverityColor(NotificationSeverity s) => s switch
    {
        NotificationSeverity.Error => "red",
        NotificationSeverity.Warning => "yellow",
        _ => "blue"
    };
}
```

- [ ] **Step 3: Layout'a partial'ı dahil et**

`_Layout.cshtml`'de navbar nav'ı içine (Tabler pattern'ine göre sağ üst):
```razor
<div class="navbar-nav flex-row order-md-last">
    @await Html.PartialAsync("~/Features/Notifications/Views/Partials/_NotificationBell.cshtml")
    <!-- diğer nav items -->
</div>
```

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Notifications/Views/Partials/_NotificationBell.cshtml \
        Application/Entegrasyon.MVC/Views/Shared/_Layout.cshtml
git commit -m "feat(notifications): zil dropdown partial + layout entegrasyonu"
```

---

## Task 4.7: Bildirimler sayfası — filter + tab

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Notifications/NotificationController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Notifications/Views/Index.cshtml`

- [ ] **Step 1: Index action — filter parametreleri**

```csharp
[HttpGet("/notifications")]
public async Task<IActionResult> Index(
    string? tab = "all",
    NotificationCategory? category = null,
    NotificationSeverity? severity = null)
{
    ViewData.SetPageTitle("Bildirimler");
    ViewData.SetActiveNav("notifications");

    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    bool onlyUnreadFlag = tab == "unread";
    var notifications = await notificationManager.GetNotificationsForUser(userId, onlyUnreadFlag);

    if (tab == "read")
        notifications = notifications.Where(n => n.NotificationsUsers.Any(nu =>
            nu.ApplicationUserId == userId && nu.IsRead));
    if (category.HasValue) notifications = notifications.Where(n => n.Category == category.Value);
    if (severity.HasValue) notifications = notifications.Where(n => n.Severity == severity.Value);

    ViewBag.Tab = tab;
    ViewBag.Category = category;
    ViewBag.Severity = severity;
    return View(notifications);
}
```

- [ ] **Step 2: Index.cshtml — Tabler card + 3 tab + filter + empty state**

Tab bar, filter form, satır listesi. Her satır `<a href="@n.ActionUrl ?? "#"` olarak navigasyon destekler. Boş state için Tabler `empty`.

- [ ] **Step 3: Test (manuel + mevcut controller testleri)**

```bash
dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "NotificationController"
```

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(notifications): Bildirimler sayfası — tab/filter/empty state"
```

---

## Task 4.8: NotificationReadSyncHandler + NotificationDismissedSyncHandler

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/Handlers/NotificationReadSyncHandler.cs`
- Create: `Application/Entegrasyon.Business/Notifications/Handlers/NotificationDismissedSyncHandler.cs`
- Create: Integration test

- [ ] **Step 1: ReadSyncHandler**

```csharp
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Sse;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class NotificationReadSyncHandler(
    ISseConnectionRegistry registry) : IDomainEventHandler<NotificationReadEvent>
{
    public Task HandleAsync(NotificationReadEvent @event, CancellationToken ct = default)
    {
        var payload = new SseNotificationPayload
        {
            EventType = "notification.read",
            NotificationId = @event.NotificationId,
            ReadAt = @event.ReadAt
        };
        foreach (var ch in registry.GetChannels(@event.UserId))
            ch.Writer.TryWrite(payload);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: DismissedSyncHandler**

```csharp
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Sse;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class NotificationDismissedSyncHandler(
    ISseConnectionRegistry registry) : IDomainEventHandler<NotificationDismissedEvent>
{
    public Task HandleAsync(NotificationDismissedEvent @event, CancellationToken ct = default)
    {
        var payload = new SseNotificationPayload
        {
            EventType = "notification.dismissed",
            NotificationId = @event.NotificationId
        };
        foreach (var ch in registry.GetChannels(@event.UserId))
            ch.Writer.TryWrite(payload);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Integration test — multi-tab read sync**

```csharp
[Fact]
public async Task MarkAsRead_SseChannelsReceiveReadEvent()
{
    await using var scope = fixture.CreateScope();
    var registry = scope.ServiceProvider.GetRequiredService<ISseConnectionRegistry>();
    var manager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
    await using var db = await factory.CreateDbContextAsync();

    var userId = Guid.NewGuid();
    db.Users.Add(new ApplicationUser { Id = userId, IsActive = true, UserName = "u" });
    await db.SaveChangesAsync();

    var ch = Channel.CreateBounded<SseNotificationPayload>(10);
    registry.Register(userId, ch);

    await manager.SendNotification("t", "c", NotificationSeverity.Info, NotificationCategory.Sistem, [userId]);
    var notif = await db.Notifications.FirstAsync();
    await manager.MarkAsRead(notif.Id, userId);

    await Task.Delay(300);  // InProcessEventDispatcher handle etsin

    // SSE channel'a iki mesaj yazılmış olmalı: "notification" + "notification.read"
    ch.Reader.TryRead(out var m1).Should().BeTrue();
    ch.Reader.TryRead(out var m2).Should().BeTrue();
    var types = new[] { m1.EventType, m2.EventType };
    types.Should().Contain("notification.read");
}
```

- [ ] **Step 4: Test PASS + commit**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "MarkAsRead_SseChannelsReceiveReadEvent"
git add Application/Entegrasyon.Business/Notifications/Handlers/NotificationReadSyncHandler.cs \
        Application/Entegrasyon.Business/Notifications/Handlers/NotificationDismissedSyncHandler.cs \
        Test/Entegrasyon.IntegrationTest/Notifications/MultiDeviceSyncTests.cs
git commit -m "feat(notifications): read/dismissed SSE sync handlers + integration test"
```

---

## Task 4.9: E2E Playwright Testleri

**Files:**
- Create: `Test/Entegrasyon.E2E/Notifications/NotificationE2ETests.cs`

- [ ] **Step 1: Test**

```csharp
using Microsoft.Playwright;
using NUnit.Framework;

namespace Entegrasyon.E2E.Notifications;

public class NotificationE2ETests : BaseE2ETest
{
    [Test]
    public async Task ProductAdded_BadgeUpdatesInOtherUserTab()
    {
        await using var browser = await Playwright.Chromium.LaunchAsync();
        var ctxA = await browser.NewContextAsync();
        var pageA = await ctxA.NewPageAsync();
        await LoginAsync(pageA, "admin", "123456789");

        var ctxB = await browser.NewContextAsync();
        var pageB = await ctxB.NewPageAsync();
        await LoginAsync(pageB, "otheruser", "password");
        var badge = pageB.Locator("[data-notification-badge]");
        await Assertions.Expect(badge).ToHaveClassAsync("d-none", new() { Timeout = 5000 });

        await pageA.GotoAsync($"{BaseUrl}/products/create");
        await pageA.Locator("[name='Title']").FillAsync("Playwright Test");
        await pageA.Locator("form button[type='submit']").ClickAsync();

        await Assertions.Expect(badge).Not.ToHaveClassAsync("d-none", new() { Timeout = 10000 });
    }

    [Test]
    public async Task ClickNotification_NavigatesToActionUrl()
    {
        // Setup: bildirim ile login, dropdown aç, satır tıkla → /products/{id}
    }

    [Test]
    public async Task MarkAsRead_SyncsAcrossTabs()
    {
        // Aynı user 2 sekmede → sekme A'da okundu → sekme B'de badge düşmeli
    }
}
```

- [ ] **Step 2: Uygulama debug'da ayakta → testleri çalıştır**

```bash
(cd Application/Entegrasyon.MVC && dotnet run &) ; sleep 5
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "NotificationE2ETests"
```

- [ ] **Step 3: Commit**

```bash
git commit -m "test(notifications): E2E Playwright — bell/navigation/multi-tab sync"
```

---

## Task 4.10: Chrome DevTools MCP Manuel Duman Testi

- [ ] **Step 1: Uygulama ayakta (port 5100)**

- [ ] **Step 2: Akış**

```
mcp__chrome-devtools-mcp__new_page → http://localhost:5100
mcp__chrome-devtools-mcp__fill_form → login admin/123456789
mcp__chrome-devtools-mcp__take_snapshot → zil ikonu var mı
mcp__chrome-devtools-mcp__list_network_requests → /events/notifications GET stream başladı mı (200, text/event-stream)
mcp__chrome-devtools-mcp__navigate_page → /products/create
mcp__chrome-devtools-mcp__fill_form → yeni ürün
mcp__chrome-devtools-mcp__click → submit
mcp__chrome-devtools-mcp__wait_for → zil badge artışı görünür olsun
mcp__chrome-devtools-mcp__click → zil dropdown → satır tıkla
mcp__chrome-devtools-mcp__take_snapshot → /products/{id} sayfasındayız
mcp__chrome-devtools-mcp__list_console_messages → error yok
```

- [ ] **Step 3: Bulgular → düzeltme (gerekirse)**

---

# Phase 5: Admin Web Push

## Task 5.1: VAPID + WebPushOptions + NuGet

**Files:**
- Modify: `Application/Entegrasyon.Business/Entegrasyon.Business.csproj` (NuGet)
- Create: `Application/Entegrasyon.Business/Notifications/WebPush/WebPushOptions.cs`
- Modify: `Application/Entegrasyon.MVC/appsettings.json`

- [ ] **Step 1: NuGet paketi**

```bash
dotnet add Application/Entegrasyon.Business package Lib.Net.Http.WebPush
```

- [ ] **Step 2: VAPID anahtar çifti üret**

Scratchpad console program veya bir kerelik unit test:
```csharp
var keys = Lib.Net.Http.WebPush.Authentication.VapidHelper.GenerateVapidKeys();
Console.WriteLine($"Public: {keys.PublicKey}");
Console.WriteLine($"Private: {keys.PrivateKey}");
```

Çıkanı `appsettings.Development.json`'a yaz. Production için user-secrets: `dotnet user-secrets set WebPush:Admin:VapidPrivateKey "..."`.

- [ ] **Step 3: Options sınıfı**

```csharp
namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush:Admin";
    public string VapidSubject { get; set; } = "mailto:admin@entegrasyon.tr";
    public string VapidPublicKey { get; set; } = "";
    public string VapidPrivateKey { get; set; } = "";
}
```

- [ ] **Step 4: appsettings.json (public key — private değil!)**

```json
{
  "WebPush": {
    "Admin": {
      "VapidSubject": "mailto:admin@entegrasyon.tr",
      "VapidPublicKey": "<public_key_buraya>"
    }
  }
}
```

`appsettings.Development.json`'a hem public hem private konulabilir.

- [ ] **Step 5: DI**

```csharp
services.Configure<WebPushOptions>(config.GetSection(WebPushOptions.SectionName));
```

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Notifications/WebPush/WebPushOptions.cs \
        Application/Entegrasyon.Business/Entegrasyon.Business.csproj \
        Application/Entegrasyon.MVC/appsettings.json \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(notifications): VAPID config + WebPushOptions + Lib.Net.Http.WebPush"
```

---

## Task 5.2: AdminPushSubscriptionManager

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/WebPush/IAdminPushSubscriptionManager.cs`
- Create: `Application/Entegrasyon.Business/Notifications/WebPush/AdminPushSubscriptionManager.cs`

- [ ] **Step 1: Interface**

```csharp
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Notifications.WebPush;

public interface IAdminPushSubscriptionManager
{
    Task<IResult> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent);
    Task<IResult> UnsubscribeAsync(string endpoint);
    Task<List<AdminPushSubscription>> GetByUserAsync(Guid userId);
}
```

- [ ] **Step 2: Implementation**

```csharp
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class AdminPushSubscriptionManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IAdminPushSubscriptionManager
{
    public async Task<IResult> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var existing = await db.AdminPushSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Endpoint == endpoint);

        if (existing is not null)
        {
            existing.P256dhKey = p256dh;
            existing.AuthKey = auth;
            existing.UserAgent = userAgent;
            existing.LastSeenAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.AdminPushSubscriptions.Add(new AdminPushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dhKey = p256dh,
                AuthKey = auth,
                UserAgent = userAgent,
                LastSeenAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return new SuccessResult("Abonelik kaydedildi.");
    }

    public async Task<IResult> UnsubscribeAsync(string endpoint)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var sub = await db.AdminPushSubscriptions.AsTracking().FirstOrDefaultAsync(x => x.Endpoint == endpoint);
        if (sub is null) return new ErrorResult("Abonelik bulunamadı.");
        db.AdminPushSubscriptions.Remove(sub);
        await db.SaveChangesAsync();
        return new SuccessResult("Abonelik kaldırıldı.");
    }

    public async Task<List<AdminPushSubscription>> GetByUserAsync(Guid userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.AdminPushSubscriptions.Where(x => x.UserId == userId).ToListAsync();
    }
}
```

- [ ] **Step 3: DI**

```csharp
services.AddScoped<IAdminPushSubscriptionManager, AdminPushSubscriptionManager>();
```

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(notifications): AdminPushSubscriptionManager"
```

---

## Task 5.3: AdminWebPushSender + PushServiceClient DI

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/WebPush/AdminWebPushSender.cs`

- [ ] **Step 1: Sender**

```csharp
using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class AdminWebPushSender(
    IAdminPushSubscriptionManager subscriptionManager,
    PushServiceClient pushClient,
    ILogger<AdminWebPushSender> logger) : INotificationSender
{
    public SenderType Type => SenderType.WebPushAdmin;

    public async Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            title = message.Header,
            body = message.Content,
            actionUrl = message.ActionUrl,
            notificationId = message.Id
        });

        foreach (var userId in userIds)
        {
            var subs = await subscriptionManager.GetByUserAsync(userId);
            foreach (var sub in subs)
            {
                try
                {
                    var pushSub = new PushSubscription(sub.Endpoint, sub.P256dhKey, sub.AuthKey);
                    var pushMsg = new PushMessage(payloadJson)
                    {
                        Topic = $"notification-{message.Id}",
                        Urgency = PushMessageUrgency.Normal
                    };
                    await pushClient.RequestPushMessageDeliveryAsync(pushSub, pushMsg);
                }
                catch (PushServiceClientException ex) when (ex.StatusCode == HttpStatusCode.Gone)
                {
                    await subscriptionManager.UnsubscribeAsync(sub.Endpoint);
                    logger.LogInformation("Removed stale push subscription {Endpoint}", sub.Endpoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Push failed to {Endpoint}", sub.Endpoint);
                }
            }
        }
    }
}
```

- [ ] **Step 2: PushServiceClient DI**

```csharp
services.AddSingleton<Lib.Net.Http.WebPush.PushServiceClient>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WebPushOptions>>().Value;
    var client = new Lib.Net.Http.WebPush.PushServiceClient();
    client.DefaultAuthentication = new Lib.Net.Http.WebPush.Authentication.VapidAuthentication(
        opts.VapidPublicKey, opts.VapidPrivateKey)
    {
        Subject = opts.VapidSubject
    };
    return client;
});
services.AddScoped<INotificationSender, AdminWebPushSender>();
```

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(notifications): AdminWebPushSender + PushServiceClient DI"
```

---

## Task 5.4: AdminPushController

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Notifications/AdminPushController.cs`

- [ ] **Step 1: Controller**

```csharp
using System.Security.Claims;
using Entegrasyon.Business.Notifications.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public sealed class AdminPushController(
    IAdminPushSubscriptionManager manager,
    IOptions<WebPushOptions> options) : Controller
{
    [HttpGet("/admin-push/vapid-public-key")]
    public IActionResult VapidPublicKey()
        => Content(options.Value.VapidPublicKey, "text/plain");

    [HttpPost("/admin-push/subscribe")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await manager.SubscribeAsync(userId, dto.Endpoint, dto.P256dh, dto.Auth,
            Request.Headers.UserAgent.ToString());
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("/admin-push/unsubscribe")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
    {
        var result = await manager.UnsubscribeAsync(dto.Endpoint);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    public sealed record SubscribeDto(string Endpoint, string P256dh, string Auth);
    public sealed record UnsubscribeDto(string Endpoint);
}
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(notifications): AdminPushController — subscribe/unsubscribe/vapid key"
```

---

## Task 5.5: Service worker sw-admin.js

**Files:**
- Create: `Application/Entegrasyon.MVC/wwwroot/sw-admin.js`

- [ ] **Step 1: Service worker**

```javascript
self.addEventListener('install', function (e) {
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    e.waitUntil(self.clients.claim());
});

self.addEventListener('push', function (event) {
    if (!event.data) return;
    const data = event.data.json();
    event.waitUntil(self.registration.showNotification(data.title || 'Bildirim', {
        body: data.body || '',
        icon: '/img/logo-192.png',
        badge: '/img/badge-72.png',
        data: { url: data.actionUrl, notificationId: data.notificationId },
        tag: 'notification-' + (data.notificationId || Date.now()),
        renotify: false
    }));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const url = (event.notification.data && event.notification.data.url) || '/notifications';
    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clients) {
        for (const client of clients) {
            if (client.url.endsWith(url) && 'focus' in client) return client.focus();
        }
        return self.clients.openWindow(url);
    }));
});
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/sw-admin.js
git commit -m "feat(notifications): service worker sw-admin.js"
```

---

## Task 5.6: Client push registration (notifications-client.js genişletme)

**Files:**
- Modify: `Application/Entegrasyon.MVC/wwwroot/js/notifications-client.js`

- [ ] **Step 1: Dosyanın sonuna push registration ekle**

```javascript
async function registerPush() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;
    try {
        const registration = await navigator.serviceWorker.register('/sw-admin.js');
        let subscription = await registration.pushManager.getSubscription();

        if (!subscription) {
            const permission = await Notification.requestPermission();
            if (permission !== 'granted') return;

            const vapidKey = await fetch('/admin-push/vapid-public-key').then(r => r.text());
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidKey)
            });
        }

        await fetch('/admin-push/subscribe', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                endpoint: subscription.endpoint,
                p256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
                auth: arrayBufferToBase64(subscription.getKey('auth'))
            })
        });
    } catch (e) {
        console.warn('Push registration failed', e);
    }
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    const out = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i);
    return out;
}

function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i]);
    return btoa(binary);
}
```

- [ ] **Step 2: `DOMContentLoaded` listener'ını güncelle**

Mevcut:
```javascript
document.addEventListener('DOMContentLoaded', connect);
```

Yeni:
```javascript
document.addEventListener('DOMContentLoaded', function () {
    connect();
    registerPush();
});
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/js/notifications-client.js
git commit -m "feat(notifications): web push client-side subscription (notifications-client.js)"
```

---

## Task 5.7: Web Push Manuel Test (Chrome DevTools MCP)

- [ ] **Step 1: Uygulamayı çalıştır, login ol, izin penceresini kabul et**

- [ ] **Step 2: DB'de abonelik yazıldı mı kontrol**

```sql
SELECT id, user_id, endpoint, last_seen_at FROM admin_push_subscriptions;
```

- [ ] **Step 3: Chrome sekmeyi kapat**

- [ ] **Step 4: Admin UI'dan manuel bildirim gönder (mevcut /admin/notifications Send formu)**

- [ ] **Step 5: OS-level notification görünmeli**

- [ ] **Step 6: Notification click → sekme açılmalı, ActionUrl'e gitmeli**

---

# Phase 6: SignalR NotificationHub Kaldırma + Flag Açma

## Task 6.1: SignalR bildirim dosyalarını sil

**Files (silinecek):**

- [ ] **Step 1: Dosyaları sil**

```bash
rm Application/Entegrasyon.Business/Notifications/SignalR/NotificationHub.cs
rm Application/Entegrasyon.Business/Notifications/SignalR/SignalRSender.cs
rm Application/Entegrasyon.Business/Notifications/SignalR/ISignalRNotificationSender.cs
```

- [ ] **Step 2: `ApplicationUserIdProvider.cs` — Chat kullanıyor mu**

Chat'te user-id mapping için kullanılıyorsa **BIRAKILIR**. Sadece notification için ise silinir.

- [ ] **Step 3: Blazor kalıntıları (artık kullanılmıyor)**

```bash
rm -rf Application/Entegrasyon.Blazor/Utility/Notifications/
```

- [ ] **Step 4: DI kayıtlarını temizle**

`ApplicationDependencyExtension.cs`:
```csharp
// SİL:
services.AddScoped<INotificationSender, SignalRSender>();
services.AddScoped<ISignalRNotificationSender, SignalRSender>();
```

- [ ] **Step 5: Program.cs — MapHub kaldır**

```csharp
// sil (varsa): app.MapHub<NotificationHub>("/notificationHub");
// BIRAKILIR: app.MapHub<ChatHub>("/chatHub");
```

- [ ] **Step 6: `SenderType.SignalR` enum değerini kaldır**

```csharp
public enum SenderType
{
    Email,
    Sms,
    Sse,
    WebPushAdmin
}
```

Bu değerin kullanıldığı yerleri `Sse`'ye güncelle (kalmış kod varsa).

- [ ] **Step 7: Build + test**

```bash
dotnet build
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(notifications): SignalR NotificationHub + Blazor kalıntıları kaldırıldı"
```

---

## Task 6.2: Feature flag'leri production'da aç

**Files:**
- Modify: `Application/Entegrasyon.MVC/appsettings.json`

- [ ] **Step 1: Flag'leri ON yap**

```json
{
  "Features": {
    "NotificationsV2": {
      "PublishEnabled": true,
      "SseEnabled": true,
      "WebPushEnabled": true
    }
  }
}
```

- [ ] **Step 2: Uygulamayı çalıştır, Chrome DevTools MCP ile full akış (Task 4.10 + 5.7)**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(notifications): production feature flag'leri aktif"
```

---

# Phase 7: Legacy Kolon Drop

## Task 7.1: Notification.IsRead/ReadAt kaldır + migration

**Files:**
- Modify: `Application/Entegrasyon.Entity/Notifications/Notification.cs`

- [ ] **Step 1: Entity'den alanları sil**

```csharp
public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    public string? Header { get; set; }
    public string? Content { get; set; }
    public NotificationSeverity Severity { get; set; }
    public NotificationCategory Category { get; set; }
    public string? ActionUrl { get; set; }
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; } = new List<NotificationsUsers>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<NotificationsClaims> NotificationClaims { get; set; } = new List<NotificationsClaims>();
}
```

- [ ] **Step 2: Migration**

```bash
dotnet ef migrations add DropLegacyNotificationReadColumns \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 3: Uygula + senkron doğrula**

```bash
dotnet ef database update ...
dotnet ef migrations has-pending-model-changes ...  # → No changes
```

- [ ] **Step 4: Full test**

```bash
dotnet test
```

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Notifications/Notification.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(notifications): Notification.IsRead/ReadAt drop — junction tek kaynak"
```

---

# Plan Özeti

- **Phase 0 (14 task):** Outbox altyapısı, IEventBus, dispatcher'lar, DI.
- **Phase 1 (3 task):** Junction IsRead/ReadAt migration + NotificationManager taşıma.
- **Phase 2 (4 task):** 24 yeni event sınıfı + 26 handler + pipeline integration test.
- **Phase 3 (7 task):** Business manager + marketplace + storefront + system publish points.
- **Phase 4 (10 task):** SSE endpoint + client JS + zil + sayfa + E2E + Chrome DevTools.
- **Phase 5 (7 task):** VAPID + subscription manager + sender + SW + client registration + manuel test.
- **Phase 6 (2 task):** SignalR kaldırma + feature flag açma.
- **Phase 7 (1 task):** Legacy kolon drop.

**Toplam: ~48 task.** Her biri 2-20 dakika arası. Sıra kritik: Phase 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7.

**Strict rule (tüm faz/task'larda):**
- Her task'tan önce `dotnet test` baseline → yeşil.
- Her task'tan sonra `dotnet test` + ilgili integration + E2E (varsa) → yeşil.
- Her task ayrı commit, anlaşılır mesaj.
- `NotificationManager`, `INotificationSender` public API'leri korunur (transport değişir, sorumluluk değişmez).
- Vanilla JS'te `innerHTML` yasak, `textContent` + `createElement` zorunlu.

---

**Sonraki adım:** Aşağıdaki iki seçenekten birini tercih et.
