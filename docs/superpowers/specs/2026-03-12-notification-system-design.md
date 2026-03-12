# Notification System Design

**Date:** 2026-03-12
**Status:** Approved

## Overview

Sağlam ve genişleyebilir bir bildirim altyapısı. Şu an in-process EventChannel üzerine kurulu, ileride SignalR'a geçiş için soyutlama katmanı mevcut. Kullanıcıya üç kanaldan ulaşır: header'daki zil ikonu dropdown, snackbar toast ve tam geçmiş sayfası.

---

## 1. Entity & Domain Değişiklikleri

### `Notification` entity'sine eklenen alanlar (`Entegrasyon.Entity`)

```csharp
public NotificationSeverity Severity { get; set; }
public NotificationCategory Category { get; set; }
public string? ActionUrl { get; set; }
```

### Yeni enum'lar (`Entegrasyon.Entity/Notifications/`)

```csharp
public enum NotificationSeverity { Info, Warning, Error, Success }
public enum NotificationCategory { Sistem, Pazaryeri, Siparis, Stok }
```

### `NotificationEvent`'e eklenen alanlar (`Entegrasyon.Business`)

```csharp
public NotificationSeverity Severity { get; set; }
public NotificationCategory Category { get; set; }
public string? ActionUrl { get; set; }
```

**Migration:** Yeni alanlar için EF Core migration eklenir.

---

## 2. Delivery Altyapısı

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
- `ConcurrentDictionary<Guid, List<Func<NotificationEvent, Task>>>` ile abone kaydı
- `DeliverAsync`: userId'ye göre ilgili handler'ları paralel çağırır

### `INotificationChannel` — SignalR genişleme noktası (`Entegrasyon.Business/Notifications/`)

```csharp
public interface INotificationChannel
{
    Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds);
}
```

`InProcessNotificationDeliveryService` bu interface'i implement eder. İleride `SignalRNotificationChannel` eklenince `DeliverAsync` her iki kanalı da çağırır; mevcut kod değişmez.

### `NotificationEventPublisher` (mevcut stub → `BackgroundService`)

- `EventChannel<NotificationEvent>` kanalını dinler
- Her event için `INotificationDeliveryService.DeliverAsync` çağırır
- `ApplicationDependencyExtension.AddBackgroundServices()` üzerinden kaydedilir

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

**İç akış:** DB'ye kaydet → `NotificationEvent` oluştur → `EventChannel`'a yaz → `NotificationEventPublisher` alır → `INotificationDeliveryService.DeliverAsync` iletir.

### Mock user ID temizliği

`NotificationList.razor.cs` ve `MarkAsRead` içindeki `Guid.Parse("00000000-...")` kaldırılır. Gerçek `AuthenticationStateProvider` kullanılır.

---

## 4. UI Bileşenleri

### Klasör yapısı

```
Features/Notifications/
  NotificationBell.razor(.cs)     ← header'daki zil + badge + dropdown
  NotificationsPage.razor(.cs)    ← /notifications tam geçmiş sayfası
  NotificationItem.razor(.cs)     ← tekil bildirim satırı (paylaşılan)
```

Mevcut `Components/Shared/NotificationList.razor` kaldırılır, yerine bu yapı gelir.

### `NotificationBell`

- `MainLayout`'a eklenir (header bölümü)
- Okunmamış sayısını `MudBadge` ile gösterir
- `MudMenu` veya `MudPopover` ile son 10 bildirimi dropdown'da listeler
- "Tümünü gör" → `/notifications`
- `INotificationDeliveryService.Subscribe` ile real-time güncelleme
- Yeni bildirim gelince `ISnackbar` toast da gösterir (severity'e göre renk)
- `OnInitializedAsync`'ta subscribe, `Dispose`'da unsubscribe

### `NotificationsPage` (`/notifications`)

- `MudDataGrid` ile tam bildirim listesi
- Severity ve Category filtre chip'leri
- "Okunmamış / Tümü" toggle
- Satıra tıklayınca: `ActionUrl` varsa navigate, yoksa okundu işaretle
- "Tümünü okundu işaretle" butonu
- `INotificationManager.GetNotificationsForUser` ile veri yükleme

### `NotificationItem`

- Severity'ye göre ikon ve renk: `Error` → kırmızı, `Warning` → turuncu, `Success` → yeşil, `Info` → mavi
- Category chip'i (küçük `MudChip`)
- `ActionUrl` varsa tıklanabilir wrapper

---

## 5. Entegrasyon Örneği

Background service veya business manager içinden:

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

`ApplicationDependencyExtension.cs` içinde:

- `services.AddSingleton<INotificationDeliveryService, InProcessNotificationDeliveryService>()`
- `services.AddSingleton<INotificationChannel>(sp => sp.GetRequiredService<INotificationDeliveryService>() as INotificationChannel)`
- `NotificationEventPublisher` → `AddBackgroundServices()` içine eklenir

---

## Kapsam Dışı (Şimdilik)

- SignalR implementasyonu (`SignalRNotificationChannel`) — altyapı hazır, kod yazılmayacak
- Push notification (browser/mobil)
- Bildirim tercihleri sayfası (kullanıcı bazlı mute/filtre)
