# Notification System Design

**Date:** 2026-03-12
**Status:** Approved (v2 — post-review fixes)

## Overview

Sağlam ve genişleyebilir bir bildirim altyapısı. Şu an in-process EventChannel üzerine kurulu, ileride SignalR'a geçiş için soyutlama katmanı mevcut. Kullanıcıya üç kanaldan ulaşır: header'daki zil ikonu dropdown, snackbar toast ve tam geçmiş sayfası.

---

## 1. Entity & Domain Değişiklikleri

### `Notification` entity'sine eklenen alanlar (`Entegrasyon.Entity/Notifications/`)

```csharp
public NotificationSeverity Severity { get; set; }   // default: Info (0)
public NotificationCategory Category { get; set; }   // default: Sistem (0)
public string? ActionUrl { get; set; }
```

### Yeni enum'lar (`Entegrasyon.Entity/Notifications/`)

```csharp
public enum NotificationSeverity { Info, Warning, Error, Success }
public enum NotificationCategory { Sistem, Pazaryeri, Siparis, Stok }
```

### `NotificationEvent`'e eklenen alanlar + constructor güncellemesi (`Entegrasyon.Business`)

```csharp
public NotificationSeverity Severity { get; set; }
public NotificationCategory Category { get; set; }
public string? ActionUrl { get; set; }

// Mevcut 4-arg constructor kaldırılır, yeni tam constructor eklenir:
public NotificationEvent(long notificationId, string header, string content,
    IEnumerable<Guid> userIds, NotificationSeverity severity,
    NotificationCategory category, string? actionUrl = null)
{
    NotificationId = notificationId; Header = header; Content = content;
    UserIds = userIds; Severity = severity; Category = category; ActionUrl = actionUrl;
}
```

### Migration

EF Core migration eklenir. Enum'lar integer olarak saklanır (EF Core varsayılanı). Mevcut satırlar `Severity=0` (Info), `Category=0` (Sistem), `ActionUrl=null` değerlerini alır — kabul edilebilir.

`NotificationEntityConfiguration` şu şekilde güncellenir:
- `Header`: `HasMaxLength(50)` → `HasMaxLength(200)` (validator ile tutarlı)
- `Content`: `HasMaxLength(400)` → `HasMaxLength(1000)` (validator ile tutarlı)
- `ActionUrl` için `HasMaxLength(500)` eklenir

### `IsRead` sorunu — bilinçli karar

Mevcut `IsRead` / `ReadAt` alanları `Notification` entity'sinin kendisinde, `NotificationsUsers` junction tablosunda değil. Bu, bir kullanıcının okundu işaretlemesinin diğer tüm kullanıcılarda da okundu göstermesi anlamına gelir.

**Bu kapsamda düzeltilmeyecek.** Sistem şimdilik single-user senaryosuna yönelik. Çok kullanıcılı doğru davranış gerekliyse `NotificationsUsers.IsRead` alanı eklenmeli — bu ayrı bir kapsam.

---

## 2. Delivery Altyapısı

### Mevcut kodun kaderi

`IBlazorNotificationSender`, `BlazorNotificationSender` ve `NotificationEventPublisher` stub'ı **silinir**. Yerini `INotificationDeliveryService` + `InProcessNotificationDeliveryService` alır.

`INotificationSender`, `EmailSender`, `SignalRSender` **korunur**. `NotificationManager` constructor'ı `IEnumerable<INotificationSender>` parametresini tutar; yeni `SendNotification` implementasyonu DB kaydı sonrası tüm registered sender'ları çağırır (artık `SenderType` filtresi yoktur — tümü tetiklenir).

`SenderType` enum'undan `RealTime` değeri kaldırılır. `BlazorNotificationSender` silinince bu değerin implementoru kalmaz. `SignalR` ve `Email` değerleri korunur, enum silinmez.

`INotificationSender.Type { get; }` property'si **korunur**. Filtreleme şu an devre dışı olsa da interface'den kaldırmak `EmailSender`/`SignalRSender` için breaking change oluşturur; ileride filtreleme geri gelebilir.

**EventChannel doğrudan okunması yasaktır:** `NotificationEventPublisher`'dan sonra `EventChannel<NotificationEvent>`'in tek consumer'ı `NotificationEventPublisher`'dır. Hiçbir component veya servis bu kanaldan `ReadAllAsync` ile doğrudan okuma yapamaz — tüm consumer'lar `INotificationDeliveryService.Subscribe` kullanmalıdır.

### `INotificationDeliveryService` (`Entegrasyon.Business/Notifications/`)

```csharp
public interface INotificationDeliveryService
{
    void Subscribe(Guid userId, Func<NotificationEvent, Task> handler);
    void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler);
    Task DeliverAsync(NotificationEvent evt);
}
```

### `InProcessNotificationDeliveryService` (`Entegrasyon.Blazor/Utility/Notifications/`)

- Singleton yaşam döngüsü
- `ConcurrentDictionary<Guid, ImmutableList<Func<NotificationEvent, Task>>>` kullanılır. `ImmutableList.Add/Remove` her zaman yeni liste döndürdüğü için `AddOrUpdate` thread-safe compare-and-swap sağlar:

```csharp
public void Subscribe(Guid userId, Func<NotificationEvent, Task> handler)
    => _subscribers.AddOrUpdate(userId,
        _ => ImmutableList.Create(handler),
        (_, existing) => existing.Add(handler));  // ImmutableList.Add yeni liste döner

public void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler)
    => _subscribers.AddOrUpdate(userId,
        _ => ImmutableList<Func<NotificationEvent, Task>>.Empty,
        (_, existing) => existing.Remove(handler));
```

- `DeliverAsync`: `_subscribers.TryGetValue(userId, out var handlers)` ile listeyi snapshot alır, `Task.WhenAll` ile çağırır. Snapshot sonraki mutation'lardan etkilenmez.

### `INotificationChannel` — SignalR genişleme noktası (`Entegrasyon.Business/Notifications/`)

```csharp
public interface INotificationChannel
{
    Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds);
}
```

`InProcessNotificationDeliveryService` bu interface'i implement eder. İleride `SignalRNotificationChannel` eklenince `DeliverAsync` her iki kanalı da çağırır — mevcut kod değişmez.

### `NotificationEventPublisher` (stub → `BackgroundService`, `Entegrasyon.Blazor/Utility/Notifications/`)

```csharp
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

---

## 3. Business Katmanı Değişiklikleri

### `INotificationManager.SendNotification` yeni imzası

```csharp
Task SendNotification(
    string header,
    string content,
    NotificationSeverity severity,
    NotificationCategory category,
    IEnumerable<Guid> userIds,
    string? actionUrl = null);
```

**İç akış:**
```csharp
// 1. Validation
var request = new SendNotificationRequest(header, content, severity, category, userIds, actionUrl);
await validator.ValidateAndThrowAsync(request);

// 2. Business Rules — (şimdilik yok, genişleme noktası)

// 3. Execution
var notification = new Notification { Header = header, Content = content,
    Severity = severity, Category = category, ActionUrl = actionUrl };
// kullanıcıları attach et, DB'ye kaydet...
await context.SaveChangesAsync();

// Tüm INotificationSender'ları tetikle (email, signalr stub'ları)
await Task.WhenAll(notificationSenders.Select(s => s.SendNotification(notification, userIds)));

// EventChannel'a yaz → Publisher → DeliveryService → Blazor component'ları
var evt = new NotificationEvent(notification.Id, header, content, userIds, severity, category, actionUrl);
await channel.Writer.WriteAsync(evt);
```

Eski `Task SendNotification(Notification notification, IEnumerable<SenderType> senderTypes)` imzası **kaldırılır**.

### Mevcut çağrı yerleri ve migrasyon

| Dosya | Mevcut çağrı | Yeni hali |
|-------|-------------|-----------|
| `TrendyolCategoryImportBackgroundService.cs:100` | `SendNotificationAsync(...)` yardımcı metodu → `notificationManager.SendNotification(notification, [SenderType.RealTime])` | Yardımcı metod kaldırılır, doğrudan yeni imzayla çağrılır; `severity` ve `category` duruma göre seçilir |
| `NotificationList.razor.cs:36,74` | `NotificationManager.GetNotificationsForUser` + `MarkAsRead` | İmza değişmiyor, sadece mock userId temizlenir |

### `SendNotificationValidator` güncellemesi

Validator artık `Notification` entity'si değil, yeni primitive parametreleri doğrular. `IFluentValidator` mekanizması korunur — yeni bir `SendNotificationRequest` record'u oluşturulur ve validator bunu alır:

```csharp
public record SendNotificationRequest(
    string Header, string Content,
    NotificationSeverity Severity, NotificationCategory Category,
    IEnumerable<Guid> UserIds, string? ActionUrl);

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

DI kaydı `ServiceDependencyExtension.cs`'de `IValidator<Notification>` → `IValidator<SendNotificationRequest>` olarak güncellenir.

### `GetNotificationsForUser` dönüş tipi

`IEnumerable<Notification>` olarak kalır — DTO projection eklenmez. Yeni `Severity`, `Category`, `ActionUrl` alanları doğrudan entity üzerinde taşındığından UI için yeterlidir. No-tracking EF Core varsayılanı navigation property sorununu önler.

### Mock user ID temizliği

`NotificationList.razor.cs`'deki `Guid.Parse("00000000-...")` kaldırılır. `AuthenticationStateProvider.GetAuthenticationStateAsync()` ile gerçek user ID alınır. `NotificationList` bileşeni kapsam dışıdır (silinecek), bu temizlik yeni `NotificationBell` ve `NotificationsPage` component'larında doğrudan doğru yapılır.

### `MarkAllAsRead`

`INotificationManager`'a yeni metod eklenir:
```csharp
Task MarkAllAsRead(Guid userId);
```
N+1 döngüsü yerine tek sorguda güncelleme:
```csharp
await context.Notifications
    .Where(n => n.Users.Any(u => u.Id == userId) && !n.IsRead)
    .ExecuteUpdateAsync(s => s
        .SetProperty(n => n.IsRead, true)
        .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
```

### Pagination kararı

`GetNotificationsForUser` şimdilik pagination eklemez. `NotificationsPage` tüm bildirimleri yükler. Bu, yüksek bildirim hacminde performans sorunu yaratabilir — kabul edilmiş sınırlama, ileride server-side pagination eklenebilir.

---

## 4. UI Bileşenleri

### Klasör yapısı

```
Features/Notifications/
  NotificationBell.razor(.cs)     ← header'daki zil + badge + dropdown
  NotificationsPage.razor(.cs)    ← /notifications tam geçmiş sayfası
  NotificationItem.razor(.cs)     ← tekil bildirim satırı (paylaşılan)
```

**Silinecekler:**
- `Components/Shared/NotificationList.razor` + `.cs` + `.css`

### `MainLayout` temizliği

`MainLayout.razor.cs` içindeki şu kodlar kaldırılır:
- `_notificationPanelOpen`, `_notificationCount`, `_notifications` alanları
- `ToggleNotificationPanel()`, `ClearNotifications()`, `GetNotificationIcon()`, `GetNotificationColor()` metodları
- `private record NotificationItem(...)` ve `private enum NotificationType` tanımları

`MainLayout.razor`'da bu alanları kullanan tüm UI kodu da kaldırılır. Yerine `<NotificationBell />` bileşeni header'a eklenir.

### `NotificationBell`

- `MainLayout`'a eklenir (header bölümü)
- Okunmamış sayısını `MudBadge` ile gösterir
- `MudMenu` veya `MudPopover` ile son 10 bildirimi dropdown'da listeler
- "Tümünü gör" → `/notifications`
- `OnInitializedAsync`'ta `INotificationManager.GetNotificationsForUser(..., onlyUnread: true)` ile başlangıç unread count yüklenir
- `INotificationDeliveryService.Subscribe` ile real-time güncelleme
- Yeni bildirim gelince `ISnackbar` toast da gösterir (severity'e göre renk)

**Yaşam döngüsü notu:** `INotificationDeliveryService` Singleton, `INotificationManager` Scoped. İkisi aynı component'ta inject edilebilir — lifetime çakışması yoktur (Blazor circuit Scoped DI scope'u yönetir).

**Subscribe/Unsubscribe delegate pattern — önemli:**
```csharp
// Doğru: member method referansı (aynı referans Subscribe ve Unsubscribe'a gider)
private Task HandleNotification(NotificationEvent evt) { ... }

protected override async Task OnInitializedAsync()
    => _deliveryService.Subscribe(_userId, HandleNotification);

public void Dispose()
    => _deliveryService.Unsubscribe(_userId, HandleNotification);
```
Lambda kullanılmaz — farklı referans oluşturur, `Unsubscribe` çalışmaz ve handler sızıntısı oluşur.

`HandleNotification` içinde `await InvokeAsync(StateHasChanged)` zorunludur — `NotificationEventPublisher` background thread'den çağırır, Blazor sync context dışında kalır.

### `NotificationsPage` (`/notifications`)

- Route: `@page "/notifications"`
- `MudDataGrid` ile tam bildirim listesi
- Severity ve Category filtre chip'leri
- "Okunmamış / Tümü" toggle
- Satıra tıklayınca: `ActionUrl` varsa `NavigationManager.NavigateTo(actionUrl)`, yoksa okundu işaretle
- "Tümünü okundu işaretle" butonu
- `INotificationManager.GetNotificationsForUser` ile veri yükleme

### `NotificationItem`

- Severity'ye göre ikon ve renk: `Error` → kırmızı, `Warning` → turuncu, `Success` → yeşil, `Info` → mavi
- Category chip'i (küçük `MudChip`)
- `ActionUrl` varsa tıklanabilir wrapper

---

## 5. Entegrasyon Örneği

```csharp
await _notificationManager.SendNotification(
    header: "Trendyol Senkronizasyonu Tamamlandı",
    content: "142 ürün başarıyla güncellendi.",
    severity: NotificationSeverity.Success,
    category: NotificationCategory.Pazaryeri,
    userIds: adminUserIds,
    actionUrl: "/marketplace/sync"
);
```

---

## 6. DI Kayıtları

`InProcessNotificationDeliveryService` `Entegrasyon.Blazor` katmanında yaşadığı için kaydı `Entegrasyon.Blazor`'ın `Program.cs`'inde yapılır — `ApplicationBootstrap` projesi Blazor projesine referans veremez.

```csharp
// Program.cs (Entegrasyon.Blazor)
builder.Services.AddSingleton<InProcessNotificationDeliveryService>();
builder.Services.AddSingleton<INotificationDeliveryService>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddSingleton<INotificationChannel>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddHostedService<NotificationEventPublisher>();
```

`ApplicationDependencyExtension.AddNotification()` içindeki mevcut `BlazorNotificationSender` + `IBlazorNotificationSender` kayıtları kaldırılır.

---

## Kapsam Dışı (Şimdilik)

- SignalR implementasyonu (`SignalRNotificationChannel`) — altyapı hazır, kod yazılmayacak
- `IsRead` per-user düzeltmesi — `NotificationsUsers` junction'ına taşıma
- Server-side pagination — `GetNotificationsForUser` için
- Push notification (browser/mobil)
- Bildirim tercihleri sayfası (kullanıcı bazlı mute/filtre)
