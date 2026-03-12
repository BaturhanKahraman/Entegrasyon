# Notification System Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sağlam, genişleyebilir bir bildirim sistemi: entity güncellemesi, in-process delivery altyapısı, business katmanı yeniden yazımı ve header zil ikonu + `/notifications` sayfası.

**Architecture:** `EventChannel<NotificationEvent>` → `NotificationEventPublisher (BackgroundService)` → `INotificationDeliveryService` → Blazor component handler'ları. `INotificationChannel` interface'i ileride SignalR için genişleme noktası sağlar.

**Tech Stack:** .NET 8, Blazor Server, MudBlazor, EF Core (PostgreSQL), FluentValidation, xUnit + Moq + FluentAssertions, `System.Collections.Immutable`

---

## File Map

### Yeni Dosyalar
| Dosya | Sorumluluk |
|-------|------------|
| `Application/Entegrasyon.Entity/Notifications/NotificationSeverity.cs` | Enum: Info/Warning/Error/Success |
| `Application/Entegrasyon.Entity/Notifications/NotificationCategory.cs` | Enum: Sistem/Pazaryeri/Siparis/Stok |
| `Application/Entegrasyon.Business/Notifications/SendNotificationRequest.cs` | Validation için record DTO |
| `Application/Entegrasyon.Business/Notifications/INotificationDeliveryService.cs` | Subscribe/Unsubscribe/DeliverAsync interface |
| `Application/Entegrasyon.Business/Notifications/INotificationChannel.cs` | SignalR genişleme noktası interface |
| `Application/Entegrasyon.Blazor/Utility/Notifications/InProcessNotificationDeliveryService.cs` | Singleton subscriber registry + delivery |
| `Application/Entegrasyon.Blazor/Features/Notifications/NotificationItem.razor` + `.cs` | Tekil bildirim satırı component |
| `Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor` + `.cs` | Header zil + badge + dropdown |
| `Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor` + `.cs` | `/notifications` tam geçmiş sayfası |
| `Test/Entegrasyon.Test/NotificationManagerTests.cs` | Unit testler |

### Değiştirilen Dosyalar
| Dosya | Değişiklik |
|-------|-----------|
| `Application/Entegrasyon.Entity/Notifications/Notification.cs` | +Severity, +Category, +ActionUrl |
| `Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationEvent.cs` | +Severity, +Category, +ActionUrl, constructor güncelle |
| `Application/Entegrasyon.Business/Notifications/SenderType.cs` | RealTime değeri kaldır |
| `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/NotificationEntityConfiguration.cs` | MaxLength güncelle + ActionUrl ekle |
| `Application/Entegrasyon.Business/Validation/FluentValidation/SendNotificationValidator.cs` | `Notification` → `SendNotificationRequest` |
| `Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs` | `IValidator<Notification>` → `IValidator<SendNotificationRequest>` |
| `Application/Entegrasyon.Business/Abstract/INotificationManager.cs` | Yeni imza + MarkAllAsRead ekle |
| `Application/Entegrasyon.Business/Concrete/NotificationManager.cs` | Tam yeniden yazım |
| `Application/Entegrasyon.Blazor/Utility/Notifications/NotificationEventPublisher.cs` | Stub → BackgroundService |
| `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs` | SendNotificationAsync yardımcı kaldır, yeni imza |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | AddNotification'dan BlazorNotificationSender kaldır |
| `Application/Entegrasyon.Blazor/Program.cs` | BlazorNotificationSender kaldır, yeni DI ekle |
| `Application/Entegrasyon.Blazor/Components/Shared/MainLayout.razor` + `.cs` | Mock bildirimleri kaldır, `<NotificationBell />` ekle |
| `Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor` | Bildirimler linki ekle |

### Silinen Dosyalar
| Dosya |
|-------|
| `Application/Entegrasyon.Blazor/Utility/Notifications/BlazorNotificationSender.cs` |
| `Application/Entegrasyon.Blazor/Utility/Notifications/IBlazorNotificationSender.cs` |
| `Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor` |
| `Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor.cs` |
| `Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor.css` |

---

## Chunk 1: Domain & Infrastructure

### Task 1: Entity Enums Ekle

**Files:**
- Create: `Application/Entegrasyon.Entity/Notifications/NotificationSeverity.cs`
- Create: `Application/Entegrasyon.Entity/Notifications/NotificationCategory.cs`
- Modify: `Application/Entegrasyon.Entity/Notifications/Notification.cs`

- [ ] **Step 1: `NotificationSeverity` enum dosyasını oluştur**

```csharp
// Application/Entegrasyon.Entity/Notifications/NotificationSeverity.cs
namespace Entegrasyon.Entity.Notifications;

public enum NotificationSeverity
{
    Info,
    Warning,
    Error,
    Success
}
```

- [ ] **Step 2: `NotificationCategory` enum dosyasını oluştur**

```csharp
// Application/Entegrasyon.Entity/Notifications/NotificationCategory.cs
namespace Entegrasyon.Entity.Notifications;

public enum NotificationCategory
{
    Sistem,
    Pazaryeri,
    Siparis,
    Stok
}
```

- [ ] **Step 3: `Notification.cs` entity'sine 3 alan ekle**

Mevcut dosya:
```csharp
// Application/Entegrasyon.Entity/Notifications/Notification.cs
public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    public string Header { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset ReadAt { get; set; }
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; }
    public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<NotificationsClaims> NotificationClaims { get; set; } = [];
}
```

Yeni alanları `IsRead`'in altına ekle:
```csharp
    public bool IsRead { get; set; }
    public DateTimeOffset ReadAt { get; set; }
    public NotificationSeverity Severity { get; set; }
    public NotificationCategory Category { get; set; }
    public string? ActionUrl { get; set; }
```

- [ ] **Step 4: Build al**

```bash
dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Notifications/
git commit -m "feat: add NotificationSeverity/Category enums and new fields to Notification entity"
```

---

### Task 2: NotificationEvent Güncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationEvent.cs`

Mevcut constructor 4 arg alıyor (`notificationId, header, content, userIds`). Yeni 3 alan + yeni constructor eklenecek.

- [ ] **Step 1: `NotificationEvent.cs` dosyasını güncelle**

```csharp
// Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationEvent.cs
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Channels.Events.Notifications;

public class NotificationEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IEnumerable<Guid> UserIds { get; set; } = [];
    public NotificationSeverity Severity { get; set; }
    public NotificationCategory Category { get; set; }
    public string? ActionUrl { get; set; }

    public NotificationEvent() { }

    public NotificationEvent(long notificationId, string header, string content,
        IEnumerable<Guid> userIds, NotificationSeverity severity,
        NotificationCategory category, string? actionUrl = null)
    {
        NotificationId = notificationId;
        Header = header;
        Content = content;
        UserIds = userIds;
        Severity = severity;
        Category = category;
        ActionUrl = actionUrl;
    }
}
```

- [ ] **Step 2: Build al**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/Events/Notifications/NotificationEvent.cs
git commit -m "feat: add Severity/Category/ActionUrl to NotificationEvent"
```

---

### Task 3: EF Core Konfigürasyon + Migration

**Files:**
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/NotificationEntityConfiguration.cs`

- [ ] **Step 1: `NotificationEntityConfiguration.cs` güncelle**

```csharp
// Application/Entegrasyon.DataAccess/.../NotificationEntityConfiguration.cs
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class NotificationEntityConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Header).HasMaxLength(200);
        builder.Property(x => x.Content).HasMaxLength(1000);
        builder.Property(x => x.ActionUrl).HasMaxLength(500);

        builder.HasMany(n => n.Users)
               .WithMany(u => u.Notifications)
               .UsingEntity<NotificationsUsers>();
    }
}
```

- [ ] **Step 2: Migration oluştur**

```bash
dotnet ef migrations add AddNotificationSeverityCategoryActionUrl \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```
Expected: Migration dosyası `Application/Entegrasyon.DataAccess/Migrations/` içinde oluştu.

- [ ] **Step 3: Migration'ı incele**

Oluşan migration'da şunları kontrol et:
- `Severity` integer kolonu eklendi (default 0 = Info)
- `Category` integer kolonu eklendi (default 0 = Sistem)
- `ActionUrl` varchar(500) nullable kolonu eklendi
- `Header` max-length 200'e çıktı
- `Content` max-length 1000'e çıktı

- [ ] **Step 4: Migration uygula**

```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```
Expected: `Done.`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/
git commit -m "feat: add EF migration for Notification Severity/Category/ActionUrl fields"
```

---

### Task 4: Delivery Interface'leri Ekle

**Files:**
- Modify: `Application/Entegrasyon.Business/Notifications/SenderType.cs`
- Create: `Application/Entegrasyon.Business/Notifications/INotificationDeliveryService.cs`
- Create: `Application/Entegrasyon.Business/Notifications/INotificationChannel.cs`

- [ ] **Step 1: `SenderType.cs`'den `RealTime` kaldır**

```csharp
// Application/Entegrasyon.Business/Notifications/SenderType.cs
namespace Entegrasyon.Business.Notifications;

public enum SenderType
{
    SignalR,
    Email
}
```

- [ ] **Step 2: `INotificationDeliveryService.cs` oluştur**

```csharp
// Application/Entegrasyon.Business/Notifications/INotificationDeliveryService.cs
using Entegrasyon.Business.Channels.Events.Notifications;

namespace Entegrasyon.Business.Notifications;

public interface INotificationDeliveryService
{
    void Subscribe(Guid userId, Func<NotificationEvent, Task> handler);
    void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler);
    Task DeliverAsync(NotificationEvent evt);
}
```

- [ ] **Step 3: `INotificationChannel.cs` oluştur**

```csharp
// Application/Entegrasyon.Business/Notifications/INotificationChannel.cs
using Entegrasyon.Business.Channels.Events.Notifications;

namespace Entegrasyon.Business.Notifications;

/// <summary>
/// SignalR veya başka bir real-time transport eklenecekse bu interface implement edilir.
/// Şu an InProcessNotificationDeliveryService bu interface'i implement eder.
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds);
}
```

- [ ] **Step 4: Build al**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Notifications/
git commit -m "feat: add INotificationDeliveryService, INotificationChannel interfaces; remove SenderType.RealTime"
```

---

### Task 5: SendNotificationRequest + Validator Güncelle

**Files:**
- Create: `Application/Entegrasyon.Business/Notifications/SendNotificationRequest.cs`
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/SendNotificationValidator.cs`
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs`

- [ ] **Step 1: `SendNotificationRequest.cs` oluştur**

```csharp
// Application/Entegrasyon.Business/Notifications/SendNotificationRequest.cs
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications;

public record SendNotificationRequest(
    string Header,
    string Content,
    NotificationSeverity Severity,
    NotificationCategory Category,
    IEnumerable<Guid> UserIds,
    string? ActionUrl);
```

- [ ] **Step 2: `SendNotificationValidator.cs` güncelle**

```csharp
// Application/Entegrasyon.Business/Validation/FluentValidation/SendNotificationValidator.cs
using Entegrasyon.Business.Notifications;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Header).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.UserIds).NotEmpty();
        RuleFor(x => x.ActionUrl).MaximumLength(500).When(x => x.ActionUrl != null);
    }
}
```

- [ ] **Step 3: `ServiceDependencyExtension.cs` güncelle — satır 41'i değiştir**

Eski satır:
```csharp
services.AddScoped<IValidator<Notification>, SendNotificationValidator>();
```

Yeni satır:
```csharp
services.AddScoped<IValidator<SendNotificationRequest>, SendNotificationValidator>();
```

Dosya başındaki `using Entegrasyon.Entity.Notifications;` satırı artık bu validator için gerekli değil, ama başka yerlerde kullanılıyor olabilir — kaldırma.

- [ ] **Step 4: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/NotificationValidatorTests.cs
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;

namespace Entegrasyon.UnitTest;

public class NotificationValidatorTests
{
    private readonly SendNotificationValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        var request = new SendNotificationRequest(
            "Test Başlık",
            "Test içerik bildirim",
            NotificationSeverity.Info,
            NotificationCategory.Sistem,
            [Guid.NewGuid()],
            null);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyHeader_ShouldFail()
    {
        var request = new SendNotificationRequest(
            "",
            "Geçerli içerik",
            NotificationSeverity.Info,
            NotificationCategory.Sistem,
            [Guid.NewGuid()],
            null);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Header");
    }

    [Fact]
    public async Task Validate_EmptyUserIds_ShouldFail()
    {
        var request = new SendNotificationRequest(
            "Başlık",
            "Geçerli içerik",
            NotificationSeverity.Info,
            NotificationCategory.Sistem,
            [],
            null);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserIds");
    }

    [Fact]
    public async Task Validate_ActionUrlTooLong_ShouldFail()
    {
        var request = new SendNotificationRequest(
            "Başlık",
            "Geçerli içerik",
            NotificationSeverity.Info,
            NotificationCategory.Sistem,
            [Guid.NewGuid()],
            new string('a', 501));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ActionUrl");
    }
}
```

- [ ] **Step 5: Testleri çalıştır — FAIL bekleniyor**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~NotificationValidatorTests" -v minimal
```
Expected: Build error veya test fail (validator henüz `Notification` tipini alıyor).

- [ ] **Step 6: Build al — tüm değişiklikler tamamlandı mı kontrol et**

```bash
dotnet build Entegrasyon.sln
```
Expected: Build succeeded (tüm projeler).

- [ ] **Step 7: Testleri tekrar çalıştır — PASS bekleniyor**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~NotificationValidatorTests" -v minimal
```
Expected: 4 test PASSED.

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Business/Notifications/SendNotificationRequest.cs \
        Application/Entegrasyon.Business/Validation/ \
        Test/Entegrasyon.Test/NotificationValidatorTests.cs
git commit -m "feat: replace SendNotificationValidator with SendNotificationRequest-based validation"
```

---

### Task 6: InProcessNotificationDeliveryService Oluştur

**Files:**
- Create: `Application/Entegrasyon.Blazor/Utility/Notifications/InProcessNotificationDeliveryService.cs`
- Delete: `Application/Entegrasyon.Blazor/Utility/Notifications/BlazorNotificationSender.cs`
- Delete: `Application/Entegrasyon.Blazor/Utility/Notifications/IBlazorNotificationSender.cs`

- [ ] **Step 1: `InProcessNotificationDeliveryService.cs` oluştur**

```csharp
// Application/Entegrasyon.Blazor/Utility/Notifications/InProcessNotificationDeliveryService.cs
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// In-process notification delivery: Blazor component'ları subscribe olur,
/// NotificationEventPublisher her event'i bu servis üzerinden iletir.
/// EventChannel'dan doğrudan okuma yapılmaz — tüm consumer'lar bu servisi kullanır.
/// </summary>
public sealed class InProcessNotificationDeliveryService : INotificationDeliveryService, INotificationChannel
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<Func<NotificationEvent, Task>>> _subscribers = new();

    public void Subscribe(Guid userId, Func<NotificationEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList.Create(handler),
            (_, existing) => existing.Add(handler));

    public void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList<Func<NotificationEvent, Task>>.Empty,
            (_, existing) => existing.Remove(handler));

    public async Task DeliverAsync(NotificationEvent evt)
    {
        foreach (var userId in evt.UserIds)
        {
            if (_subscribers.TryGetValue(userId, out var handlers) && handlers.Count > 0)
                await Task.WhenAll(handlers.Select(h => h(evt)));
        }
    }

    // INotificationChannel — SignalR için genişleme noktası
    public Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds)
        => DeliverAsync(evt);
}
```

- [ ] **Step 2: `BlazorNotificationSender.cs` sil**

```bash
rm Application/Entegrasyon.Blazor/Utility/Notifications/BlazorNotificationSender.cs
rm Application/Entegrasyon.Blazor/Utility/Notifications/IBlazorNotificationSender.cs
```

- [ ] **Step 3: Build al (derleme hataları bulunacak)**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj 2>&1 | grep -E "error|Error"
```
Beklenen hatalar: `IBlazorNotificationSender` ve `BlazorNotificationSender` referansları.

- [ ] **Step 4: `NotificationList.razor.cs` içindeki inject'i temizle**

`NotificationList.razor.cs`'i sil (bir sonraki task'ta UI temizlenecek) — şimdilik sadece inject'i kaldır:
`[Inject] private NotificationManager NotificationManager` satırı `INotificationManager` olarak değiştir (bu dosya Task 10'da silinecek, şimdilik build açısından yeterli).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Utility/Notifications/InProcessNotificationDeliveryService.cs
git commit -m "feat: implement InProcessNotificationDeliveryService with ImmutableList thread-safety"
```

---

## Chunk 2: Business Katmanı

### Task 7: INotificationManager + NotificationManager Güncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/INotificationManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/NotificationManager.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/NotificationManagerTests.cs
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Entegrasyon.UnitTest;

public class NotificationManagerTests : BaseTest
{
    private readonly Mock<IFluentValidator> _mockValidator;
    private readonly Mock<INotificationSender> _mockSender;
    private readonly EventChannel<NotificationEvent> _eventChannel;
    private readonly NotificationManager _sut;

    public NotificationManagerTests()
    {
        _mockValidator = new Mock<IFluentValidator>();
        _mockValidator.Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>()))
                      .Returns(Task.CompletedTask);

        _mockSender = new Mock<INotificationSender>();
        _mockSender.Setup(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()))
                   .Returns(Task.CompletedTask);

        _eventChannel = new EventChannel<NotificationEvent>();

        // In-memory DB kullan
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new IntegrationDbContext(options);

        _sut = new NotificationManager(
            [_mockSender.Object],
            dbContext,
            _mockValidator.Object,
            _eventChannel);
    }

    [Fact]
    public async Task SendNotification_ValidRequest_SavesToDB()
    {
        var userId = Guid.NewGuid();

        await _sut.SendNotification(
            "Test Başlık",
            "Test içerik mesajı",
            NotificationSeverity.Info,
            NotificationCategory.Sistem,
            [userId]);

        _mockValidator.Verify(v => v.ValidateAndThrowAsync(It.IsAny<object>()), Times.Once);
        _mockSender.Verify(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WritesToEventChannel()
    {
        var userId = Guid.NewGuid();

        await _sut.SendNotification(
            "Kanal Testi",
            "Kanal içeriği mesajı",
            NotificationSeverity.Success,
            NotificationCategory.Pazaryeri,
            [userId],
            "/marketplace");

        _eventChannel.Reader.TryRead(out var evt).Should().BeTrue();
        evt!.Header.Should().Be("Kanal Testi");
        evt.Severity.Should().Be(NotificationSeverity.Success);
        evt.Category.Should().Be(NotificationCategory.Pazaryeri);
        evt.ActionUrl.Should().Be("/marketplace");
    }
}
```

- [ ] **Step 2: Test çalıştır — FAIL bekleniyor**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~NotificationManagerTests" -v minimal
```
Expected: Build error — `NotificationManager` henüz yeni imzayı almıyor.

- [ ] **Step 3: `INotificationManager.cs` güncelle**

```csharp
// Application/Entegrasyon.Business/Abstract/INotificationManager.cs
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Abstract;

public interface INotificationManager
{
    Task SendNotification(
        string header,
        string content,
        NotificationSeverity severity,
        NotificationCategory category,
        IEnumerable<Guid> userIds,
        string? actionUrl = null);

    Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false);
    Task MarkAsRead(long notificationId, Guid userId);
    Task MarkAllAsRead(Guid userId);
}
```

- [ ] **Step 4: `NotificationManager.cs` yeniden yaz**

```csharp
// Application/Entegrasyon.Business/Concrete/NotificationManager.cs
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(
    IEnumerable<INotificationSender> notificationSenders,
    IntegrationDbContext context,
    IFluentValidator validator,
    EventChannel<NotificationEvent> eventChannel) : INotificationManager
{
    public async Task SendNotification(
        string header,
        string content,
        NotificationSeverity severity,
        NotificationCategory category,
        IEnumerable<Guid> userIds,
        string? actionUrl = null)
    {
        // 1. Validation
        var request = new SendNotificationRequest(header, content, severity, category, userIds, actionUrl);
        await validator.ValidateAndThrowAsync(request);

        // 2. Business Rules — (genişleme noktası)

        // 3. Execution
        var userIdList = userIds.ToList();

        var trackedUsers = await context.Users
            .Where(u => userIdList.Contains(u.Id))
            .ToListAsync();

        var notification = new Notification
        {
            Header = header,
            Content = content,
            Severity = severity,
            Category = category,
            ActionUrl = actionUrl,
            Users = trackedUsers,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        // Tüm sender'ları tetikle (email, signalr stub'ları — filtre yok)
        if (trackedUsers.Count > 0)
        {
            var trackedUserIds = trackedUsers.Select(u => u.Id);
            await Task.WhenAll(notificationSenders.Select(s =>
                s.SendNotification(notification, trackedUserIds)));
        }

        // EventChannel'a yaz → NotificationEventPublisher → INotificationDeliveryService
        var evt = new NotificationEvent(
            notification.Id, header, content, userIdList, severity, category, actionUrl);
        await eventChannel.Writer.WriteAsync(evt);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false)
    {
        var query = context.Notifications
            .Include(n => n.Users)
            .Where(n => n.Users.Any(u => u.Id == userId));

        if (onlyUnread)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task MarkAsRead(long notificationId, Guid userId)
    {
        var notification = await context.Notifications
            .Include(n => n.Users)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Users.Any(u => u.Id == userId));

        if (notification is null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task MarkAllAsRead(Guid userId)
    {
        await context.Notifications
            .Where(n => n.Users.Any(u => u.Id == userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
    }
}
```

- [ ] **Step 5: Build al**

```bash
dotnet build Entegrasyon.sln 2>&1 | grep -E "^.*error"
```
Expected: Derleme hataları — `TrendyolCategoryImportBackgroundService` eski imzayı çağırıyor. Sonraki task'ta düzeltilecek.

- [ ] **Step 6: Testleri çalıştır — PASS bekleniyor**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~NotificationManagerTests" -v minimal
```
Expected: 2 test PASSED.

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/INotificationManager.cs \
        Application/Entegrasyon.Business/Concrete/NotificationManager.cs \
        Test/Entegrasyon.Test/NotificationManagerTests.cs
git commit -m "feat: rewrite NotificationManager with new SendNotification signature and MarkAllAsRead"
```

---

### Task 8: TrendyolCategoryImportBackgroundService Güncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs`

`SendNotificationAsync` yardımcı metodu kaldırılır; her çağrı doğrudan yeni imzayla yazılır.

- [ ] **Step 1: 3 `SendNotificationAsync` çağrısını güncelle**

Eski yardımcı metod çağrısı (satır 50-53):
```csharp
await SendNotificationAsync(notificationManager,
    "Trendyol kategori içe aktarma işlemi başladı",
    $"{importEvent.Categories.Count()} kategori içe aktarılıyor...",
    [importEvent.UserId]);
```

Yeni:
```csharp
await notificationManager.SendNotification(
    header: "Trendyol kategori içe aktarma işlemi başladı",
    content: $"{importEvent.Categories.Count()} kategori içe aktarılıyor...",
    severity: NotificationSeverity.Info,
    category: NotificationCategory.Pazaryeri,
    userIds: [importEvent.UserId]);
```

Eski çağrı (satır 71):
```csharp
await SendNotificationAsync(notificationManager, header, content, [importEvent.UserId]);
```

Yeni (`result.Success` branch'e göre severity seçilmeli — `header` ve `content` değişkenleri mevcut):
```csharp
await notificationManager.SendNotification(
    header: header,
    content: content,
    severity: result.Success ? NotificationSeverity.Success : NotificationSeverity.Error,
    category: NotificationCategory.Pazaryeri,
    userIds: [importEvent.UserId],
    actionUrl: result.Success ? "/marketplace/sync" : null);
```

Eski çağrı (satır 81-84, catch block):
```csharp
await SendNotificationAsync(notificationManager,
    "Trendyol kategori içe aktarma hatası",
    $"Beklenmeyen hata: {ex.Message}",
    [importEvent.UserId]);
```

Yeni:
```csharp
await notificationManager.SendNotification(
    header: "Trendyol kategori içe aktarma hatası",
    content: $"Beklenmeyen hata: {ex.Message}",
    severity: NotificationSeverity.Error,
    category: NotificationCategory.Pazaryeri,
    userIds: [importEvent.UserId]);
```

- [ ] **Step 2: `SendNotificationAsync` yardımcı metodunu sil (satır 89-106)**

Yardımcı metod tamamen kaldırılır.

- [ ] **Step 3: Gerekli using'leri ekle**

Dosya başına ekle (henüz yoksa):
```csharp
using Entegrasyon.Entity.Notifications;
```

- [ ] **Step 4: Build al**

```bash
dotnet build Entegrasyon.sln
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs
git commit -m "feat: migrate TrendyolCategoryImportBackgroundService to new SendNotification signature"
```

---

### Task 9: NotificationEventPublisher Güncelle + DI Yapılandır

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Utility/Notifications/NotificationEventPublisher.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- Modify: `Application/Entegrasyon.Blazor/Program.cs`

- [ ] **Step 1: `NotificationEventPublisher.cs` yeniden yaz**

```csharp
// Application/Entegrasyon.Blazor/Utility/Notifications/NotificationEventPublisher.cs
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Microsoft.Extensions.Hosting;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// EventChannel&lt;NotificationEvent&gt; kanalının TEK consumer'ı.
/// Hiçbir component bu kanaldan doğrudan okuma yapamaz.
/// Tüm Blazor bileşenleri INotificationDeliveryService.Subscribe kullanır.
/// </summary>
public sealed class NotificationEventPublisher(
    EventChannel<NotificationEvent> channel,
    INotificationDeliveryService deliveryService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
            await deliveryService.DeliverAsync(evt);
    }
}
```

- [ ] **Step 2: `ApplicationDependencyExtension.cs` — `AddNotification`'dan BlazorNotificationSender kaldır**

`AddNotification()` metodu içinde yalnızca `SignalRSender` ve `EmailSender` kalmalı. Bu metot zaten doğru durumda (`BlazorNotificationSender` bootstrap'ta kayıtlı değil). Değişiklik yok — sadece doğrula.

- [ ] **Step 3: `Program.cs` güncelle**

Eski satırlar (84-88):
```csharp
builder.Services.AddSingleton<IBlazorNotificationSender, BlazorNotificationSender>();
builder.Services.AddSingleton<INotificationSender, BlazorNotificationSender>();

// Register notification event publisher
builder.Services.AddSingleton<NotificationEventPublisher>();
```

Yeni (bu 4 satırın yerine):
```csharp
builder.Services.AddSingleton<InProcessNotificationDeliveryService>();
builder.Services.AddSingleton<INotificationDeliveryService>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddSingleton<INotificationChannel>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddHostedService<NotificationEventPublisher>();
```

Gerekli using ekle:
```csharp
using Entegrasyon.Blazor.Utility.Notifications;
using Entegrasyon.Business.Notifications;
```

- [ ] **Step 4: Build al**

```bash
dotnet build Entegrasyon.sln
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Utility/Notifications/NotificationEventPublisher.cs \
        Application/Entegrasyon.Blazor/Program.cs
git commit -m "feat: wire NotificationEventPublisher as BackgroundService, register InProcessNotificationDeliveryService"
```

---

## Chunk 3: UI Bileşenleri

### Task 10: NotificationItem Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationItem.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationItem.razor.cs`

- [ ] **Step 1: `NotificationItem.razor.cs` oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Notifications/NotificationItem.razor.cs
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationItem : ComponentBase
{
    [Parameter, EditorRequired] public Notification Notification { get; set; } = null!;
    [Parameter] public EventCallback<long> OnMarkAsRead { get; set; }

    internal string SeverityIcon => Notification.Severity switch
    {
        NotificationSeverity.Success => Icons.Material.Filled.CheckCircle,
        NotificationSeverity.Warning => Icons.Material.Filled.Warning,
        NotificationSeverity.Error   => Icons.Material.Filled.Error,
        _                            => Icons.Material.Filled.Info
    };

    internal Color SeverityColor => Notification.Severity switch
    {
        NotificationSeverity.Success => Color.Success,
        NotificationSeverity.Warning => Color.Warning,
        NotificationSeverity.Error   => Color.Error,
        _                            => Color.Info
    };

    internal string CategoryLabel => Notification.Category switch
    {
        NotificationCategory.Pazaryeri => "Pazaryeri",
        NotificationCategory.Siparis   => "Sipariş",
        NotificationCategory.Stok      => "Stok",
        _                              => "Sistem"
    };
}
```

- [ ] **Step 2: `NotificationItem.razor` oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Notifications/NotificationItem.razor *@
@using Entegrasyon.Entity.Notifications

<div class="d-flex align-center gap-2 py-1">
    <MudIcon Icon="@SeverityIcon" Color="@SeverityColor" Size="Size.Small" />
    <div class="flex-grow-1">
        <div class="d-flex align-center gap-1 mb-1">
            <MudText Typo="Typo.body2" Class="font-weight-medium">@Notification.Header</MudText>
            <MudChip T="string" Size="Size.Small" Color="Color.Default" Class="ml-1">@CategoryLabel</MudChip>
        </div>
        <MudText Typo="Typo.body2" Color="Color.Secondary">@Notification.Content</MudText>
        <MudText Typo="Typo.caption" Color="Color.Secondary">
            @Notification.CreatedAt.ToLocalTime().ToString("g")
        </MudText>
    </div>
    @if (!Notification.IsRead)
    {
        <MudIconButton Icon="@Icons.Material.Filled.MarkEmailRead"
                       Size="Size.Small"
                       Color="Color.Primary"
                       OnClick="@(() => OnMarkAsRead.InvokeAsync(Notification.Id))"
                       Title="Okundu işaretle" />
    }
</div>
```

- [ ] **Step 3: Build al**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Notifications/
git commit -m "feat: add NotificationItem component"
```

---

### Task 11: NotificationBell Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor.cs`

- [ ] **Step 1: `NotificationBell.razor.cs` oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationDeliveryService DeliveryService { get; set; } = null!;
    [Inject] private INotificationManager NotificationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private Guid _userId;
    private List<Notification> _recentNotifications = [];
    private int _unreadCount;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        var all = await NotificationManager.GetNotificationsForUser(_userId, onlyUnread: false);
        _recentNotifications = all.Take(10).ToList();
        _unreadCount = _recentNotifications.Count(n => !n.IsRead);

        DeliveryService.Subscribe(_userId, HandleNotification);
    }

    // ÖNEMLI: Member method referansı — lambda kullanılmaz (Unsubscribe çalışmaz)
    private async Task HandleNotification(NotificationEvent evt)
    {
        var notification = new Notification
        {
            Id = evt.NotificationId,
            Header = evt.Header,
            Content = evt.Content,
            Severity = evt.Severity,
            Category = evt.Category,
            ActionUrl = evt.ActionUrl,
            CreatedAt = evt.OccurredAt,
            IsRead = false
        };

        _recentNotifications.Insert(0, notification);
        if (_recentNotifications.Count > 10)
            _recentNotifications.RemoveAt(10);
        _unreadCount++;

        var severity = evt.Severity switch
        {
            NotificationSeverity.Success => Severity.Success,
            NotificationSeverity.Warning => Severity.Warning,
            NotificationSeverity.Error   => Severity.Error,
            _                            => Severity.Info
        };

        Snackbar.Add(evt.Header, severity);

        // ÖNEMLI: Background thread'den çağrıldığı için InvokeAsync zorunlu
        await InvokeAsync(StateHasChanged);
    }

    private async Task MarkAsRead(long notificationId)
    {
        await NotificationManager.MarkAsRead(notificationId, _userId);
        var n = _recentNotifications.FirstOrDefault(x => x.Id == notificationId);
        if (n is not null)
        {
            n.IsRead = true;
            _unreadCount = Math.Max(0, _unreadCount - 1);
            StateHasChanged();
        }
    }

    public void Dispose()
        => DeliveryService.Unsubscribe(_userId, HandleNotification);
}
```

- [ ] **Step 2: `NotificationBell.razor` oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor *@
@using Entegrasyon.Entity.Notifications
@using Entegrasyon.Blazor.Features.Notifications

<MudMenu AnchorOrigin="Origin.BottomRight" TransformOrigin="Origin.TopRight" Dense="true">
    <ActivatorContent>
        <MudBadge Content="@_unreadCount" Color="Color.Error" Overlap="true"
                  Visible="@(_unreadCount > 0)">
            <MudIconButton Icon="@Icons.Material.Filled.Notifications"
                           Color="Color.Inherit" />
        </MudBadge>
    </ActivatorContent>
    <ChildContent>
        <MudPaper Class="pa-0" Style="min-width:320px;max-width:400px">
            <div class="d-flex align-center justify-space-between pa-3">
                <MudText Typo="Typo.subtitle1" Class="font-weight-bold">Bildirimler</MudText>
                <MudButton Variant="Variant.Text" Size="Size.Small"
                           Href="/notifications" Color="Color.Primary">
                    Tümünü Gör
                </MudButton>
            </div>
            <MudDivider />
            @if (_recentNotifications.Any())
            {
                <MudList T="Notification" Dense="true">
                    @foreach (var notification in _recentNotifications)
                    {
                        <MudListItem T="Notification" Class="@(notification.IsRead ? "" : "mud-background-gray")">
                            <NotificationItem Notification="@notification"
                                              OnMarkAsRead="MarkAsRead" />
                        </MudListItem>
                        <MudDivider />
                    }
                </MudList>
            }
            else
            {
                <div class="pa-4 d-flex justify-center">
                    <MudText Color="Color.Secondary" Typo="Typo.body2">Bildirim yok</MudText>
                </div>
            }
        </MudPaper>
    </ChildContent>
</MudMenu>
```

- [ ] **Step 3: Build al**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor \
        Application/Entegrasyon.Blazor/Features/Notifications/NotificationBell.razor.cs
git commit -m "feat: add NotificationBell component with real-time subscribe and snackbar toast"
```

---

### Task 12: MainLayout Güncelle

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Components/Shared/MainLayout.razor`
- Modify: `Application/Entegrasyon.Blazor/Components/Shared/MainLayout.razor.cs`

- [ ] **Step 1: `MainLayout.razor.cs` — mock bildirimleri kaldır**

Şu satırları/metotları sil:
- `private bool _notificationPanelOpen = false;`
- `private int _notificationCount = 3;`
- `private List<NotificationItem> _notifications = [];`
- `OnInitialized` içindeki `_notifications = [...]` bloğu
- `ToggleNotificationPanel()` metodu
- `ClearNotifications()` metodu
- `GetNotificationIcon(NotificationType type)` metodu
- `GetNotificationColor(NotificationType type)` metodu
- `private record NotificationItem(...)` tanımı
- `private enum NotificationType { ... }` tanımı

Temizlenmiş `MainLayout.razor.cs`:
```csharp
using Entegrasyon.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private ErrorBoundary? _errorBoundary;
    private bool _drawerOpen = true;
    private bool _isDarkMode = false;
    private MudTheme _theme = new();

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1976D2", Secondary = "#424242",
                Success = "#4CAF50", Info = "#2196F3",
                Warning = "#FF9800", Error = "#F44336",
                AppbarBackground = "#1976D2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#90CAF9", Secondary = "#BDBDBD",
                Success = "#81C784", Info = "#64B5F6",
                Warning = "#FFB74D", Error = "#E57373",
                AppbarBackground = "#212121",
            }
        };
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        => _errorBoundary?.Recover();

    public void Dispose()
        => NavigationManager.LocationChanged -= OnLocationChanged;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
    private void ToggleTheme() => _isDarkMode = !_isDarkMode;

    private async Task HandleLogout()
    {
        if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
            await customAuthStateProvider.UpdateAuthenticationState(null);

        NavigationManager.NavigateTo("/auth/login", forceLoad: true);
    }
}
```

- [ ] **Step 2: `MainLayout.razor` — bildirim drawer'ını kaldır, `<NotificationBell />` ekle**

Eski notification bloğu (satır 23-28):
```razor
<!-- Notifications -->
<MudBadge Content="_notificationCount" Color="Color.Error" Overlap="true" Visible="@(_notificationCount > 0)">
    <MudIconButton Icon="@Icons.Material.Filled.Notifications"
                   Color="Color.Inherit"
                   OnClick="ToggleNotificationPanel" />
</MudBadge>
```

Yeni (aynı yere):
```razor
<!-- Notifications -->
<NotificationBell />
```

`@using Entegrasyon.Blazor.Features.Notifications` satırını dosya başına ekle.

Eski notification drawer (satır 61-100):
```razor
<!-- Notification Panel -->
<MudDrawer @bind-Open="_notificationPanelOpen" Anchor="Anchor.End" ...>
    ...
</MudDrawer>
```
Bu bloğun tamamını sil.

- [ ] **Step 3: Build al**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Components/Shared/MainLayout.razor \
        Application/Entegrasyon.Blazor/Components/Shared/MainLayout.razor.cs
git commit -m "feat: replace MainLayout mock notifications with NotificationBell component"
```

---

### Task 13: NotificationsPage Oluştur

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor.cs`

- [ ] **Step 1: `NotificationsPage.razor.cs` oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationsPage : ComponentBase
{
    [Inject] private INotificationManager NotificationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private Guid _userId;
    private List<Notification> _notifications = [];
    private bool _onlyUnread = false;
    private NotificationSeverity? _severityFilter;
    private NotificationCategory? _categoryFilter;

    private IEnumerable<Notification> FilteredNotifications => _notifications
        .Where(n => _severityFilter == null || n.Severity == _severityFilter)
        .Where(n => _categoryFilter == null || n.Category == _categoryFilter);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await LoadNotifications();
    }

    private async Task LoadNotifications()
    {
        var result = await NotificationManager.GetNotificationsForUser(_userId, _onlyUnread);
        _notifications = result.ToList();
    }

    private async Task OnRowClick(Notification notification)
    {
        if (!notification.IsRead)
            await NotificationManager.MarkAsRead(notification.Id, _userId);

        notification.IsRead = true;

        if (notification.ActionUrl is not null)
            NavigationManager.NavigateTo(notification.ActionUrl);
    }

    private async Task MarkAllAsRead()
    {
        await NotificationManager.MarkAllAsRead(_userId);
        foreach (var n in _notifications)
            n.IsRead = true;
        StateHasChanged();
    }

    private async Task OnOnlyUnreadChanged(bool value)
    {
        _onlyUnread = value;
        await LoadNotifications();
    }
}
```

- [ ] **Step 2: `NotificationsPage.razor` oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor *@
@page "/notifications"
@using Entegrasyon.Entity.Notifications

<MudText Typo="Typo.h5" Class="mb-4">Bildirimler</MudText>

<MudPaper Class="pa-4 mb-4" Elevation="1">
    <div class="d-flex align-center gap-4 flex-wrap">
        <MudSwitch T="bool" Value="_onlyUnread"
                   ValueChanged="OnOnlyUnreadChanged"
                   Color="Color.Primary"
                   Label="Yalnızca okunmamış" />

        <MudSelect T="NotificationSeverity?" Label="Önem Seviyesi"
                   Value="_severityFilter"
                   ValueChanged="@((v) => { _severityFilter = v; StateHasChanged(); })"
                   Clearable="true" Style="min-width:160px">
            <MudSelectItem T="NotificationSeverity?" Value="NotificationSeverity.Info">Bilgi</MudSelectItem>
            <MudSelectItem T="NotificationSeverity?" Value="NotificationSeverity.Warning">Uyarı</MudSelectItem>
            <MudSelectItem T="NotificationSeverity?" Value="NotificationSeverity.Error">Hata</MudSelectItem>
            <MudSelectItem T="NotificationSeverity?" Value="NotificationSeverity.Success">Başarı</MudSelectItem>
        </MudSelect>

        <MudSelect T="NotificationCategory?" Label="Kategori"
                   Value="_categoryFilter"
                   ValueChanged="@((v) => { _categoryFilter = v; StateHasChanged(); })"
                   Clearable="true" Style="min-width:160px">
            <MudSelectItem T="NotificationCategory?" Value="NotificationCategory.Sistem">Sistem</MudSelectItem>
            <MudSelectItem T="NotificationCategory?" Value="NotificationCategory.Pazaryeri">Pazaryeri</MudSelectItem>
            <MudSelectItem T="NotificationCategory?" Value="NotificationCategory.Siparis">Sipariş</MudSelectItem>
            <MudSelectItem T="NotificationCategory?" Value="NotificationCategory.Stok">Stok</MudSelectItem>
        </MudSelect>

        <MudSpacer />

        <MudButton Variant="Variant.Outlined" Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.DoneAll"
                   OnClick="MarkAllAsRead">
            Tümünü Okundu İşaretle
        </MudButton>
    </div>
</MudPaper>

<MudDataGrid T="Notification" Items="@FilteredNotifications.AsEnumerable()"
             Hover="true" Dense="true" RowClick="@(args => OnRowClick(args.Item))">
    <Columns>
        <PropertyColumn Property="x => x.Header" Title="Başlık" />
        <TemplateColumn Title="Seviye">
            <CellTemplate>
                <NotificationItem Notification="@context.Item"
                                  OnMarkAsRead="@(async (id) => { await NotificationManager.MarkAsRead(id, _userId); context.Item.IsRead = true; StateHasChanged(); })" />
            </CellTemplate>
        </TemplateColumn>
        <PropertyColumn Property="x => x.CreatedAt" Title="Tarih"
                        Format="dd.MM.yyyy HH:mm" />
        <TemplateColumn Title="Durum">
            <CellTemplate>
                @if (context.Item.IsRead)
                {
                    <MudChip T="string" Size="Size.Small" Color="Color.Default">Okundu</MudChip>
                }
                else
                {
                    <MudChip T="string" Size="Size.Small" Color="Color.Primary">Yeni</MudChip>
                }
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

- [ ] **Step 3: Build al**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor \
        Application/Entegrasyon.Blazor/Features/Notifications/NotificationsPage.razor.cs
git commit -m "feat: add NotificationsPage at /notifications with filtering and mark-all-read"
```

---

### Task 14: NavMenu + Eski Bileşen Temizliği

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor`
- Delete: `Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor` + `.cs` + `.css`

- [ ] **Step 1: `NavMenu.razor`'a Bildirimler linki ekle**

"Yönetim" grubuna `Özellikler` linkinin altına ekle:
```razor
<MudNavLink Href="/notifications" Match="NavLinkMatch.Prefix" Icon="@Icons.Material.Filled.NotificationsActive">
    Bildirimler
</MudNavLink>
```

- [ ] **Step 2: Eski `NotificationList` dosyalarını sil**

```bash
rm Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor
rm Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor.cs
rm Application/Entegrasyon.Blazor/Components/Shared/NotificationList.razor.css
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```
Expected: Build succeeded.

- [ ] **Step 4: Tüm testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal
```
Expected: Tüm testler PASSED.

- [ ] **Step 5: Uygulamayı başlat ve manuel test**

```bash
cd Application/Entegrasyon.Blazor && dotnet run
```

Kontrol et:
- Header'da zil ikonu görünüyor
- Zile tıklayınca dropdown açılıyor
- `/notifications` sayfası açılıyor
- Filtreler çalışıyor
- "Tümünü okundu işaretle" butonu çalışıyor

- [ ] **Step 6: Final commit**

```bash
git add Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor
git commit -m "feat: add Bildirimler link to NavMenu; remove legacy NotificationList component"
```
