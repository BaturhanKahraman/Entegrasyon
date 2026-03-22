# Pazarama Faz 3: Sipariş Yönetimi + İade/İptal — Tasarım Dokümanı

## Amaç

Pazarama pazaryerinden sipariş çekme, sipariş durumu güncelleme, kargo takip bildirme, iade ve iptal yönetimi eklemek. Webhook desteği olmadığı için polling ile çalışır.

## Kapsam

- IPazaramaOrderService: sipariş çekme, statü güncelleme, kargo takip, toplu güncelleme
- OrderManager.ImportPazaramaOrdersAsync: sipariş import (dedup + stok azaltma)
- PazaramaOrderPollingService: 2dk interval sipariş polling
- IPazaramaRefundService: iade talepleri listeleme, onay/red, iptal listeleme/güncelleme
- PazaramaRefundPollingService: 5dk interval iade/iptal polling
- Sipariş ve iade DTO'ları
- Mock servisleri + unit testler

## Kapsam Dışı

- Fatura yükleme (Faz 4)
- Muhasebe/finans sorgusu (Faz 4)
- Soru-cevap yönetimi (Faz 4)
- Paket bölme/birleştirme (ileri faz)
- Split order V2 (ileri faz)

---

## Mimari Kararlar

### 1. Polling — Webhook Yok
Pazarama webhook desteği sunmuyor. Siparişler `POST /order/getOrdersForApi` ile çekilir, iadeler `POST /order/getRefund` ile. 2dk (sipariş) ve 5dk (iade) interval.

### 2. POST ile Sipariş Çekme
Pazarama sipariş listeleme endpoint'i `POST` metodu kullanır (Trendyol GET, N11 SOAP). Request body'de tarih aralığı ve sayfalama gönderilir. Bu durum `IPazaramaApiClient.PostAsync` kullanımını gerektirir.

### 3. Dedup: OrderNumber
Pazarama siparişleri `orderNumber` (long) ile benzersiz tanımlanır. Import sırasında `Order.OrderNumber` alanı ile dedup yapılır (N11 pattern).

### 4. Advisory Lock: 2005
PostgreSQL `pg_try_advisory_lock(2005)` ile concurrent import korunur. Mevcut: Trendyol=2001, N11=2002.

### 5. Money Object → Decimal
Pazarama fiyatları money object formatında döner (`{ value, valueInt, valueString, currency }`). `value` alanı (decimal) kullanılır.

### 6. OrderItemStatus Mapping
Pazarama statüleri dahili duruma map'lenir:

| Pazarama Status | Değer | Dahili Karşılık |
|-----------------|-------|-----------------|
| Siparişiniz Alındı | 3 | Created |
| Hazırlanıyor | 12 | Processing |
| Kargoya Verildi | 5 | Shipped |
| Teslim Edildi | 11 | Delivered |
| Teslim Edilemedi | 14 | DeliveryFailed |
| İptal Edildi | 6 | Cancelled |
| Tedarik Edilemedi | 13 | Unsupplied |

### 7. İade/İptal Ayrımı
Pazarama iade ve iptali farklı endpoint'lerle yönetir:
- İade: `POST /order/getRefund` + `POST /order/updateRefund`
- İptal: `POST /order/api/cancel/items` + `PUT /order/api/cancel`
Her ikisi de `refundId` + `status` ile güncellenir (2=Onay, 3=Red).

### 8. Sipariş Akışı (Önemli)
Pazarama'da sipariş statüsü `OrderItemId` bazlı takip edilir:
1. Statü 3 (Alındı) → alıcı iptal edebilir
2. Statü 12 (Hazırlanıyor) yapılmalı → kargo kartı açılabilir
3. Statü 5 (Kargoya Verildi) → kargo takip numarası zorunlu
4. Statü 11 (Teslim Edildi) veya 14 (Teslim Edilemedi)
5. Teslim sonrası → alıcı iade başlatabilir

---

## Bileşenler

### A. IPazaramaOrderService

```csharp
interface IPazaramaOrderService
{
    Task<IDataResult<List<PazaramaOrderDto>>> FetchOrdersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int pageSize = 500, int pageNumber = 1);
    Task<IResult> UpdateOrderItemStatusAsync(long orderNumber, PazaramaOrderItemUpdate item);
    Task<IResult> BulkUpdateOrderStatusAsync(long orderNumber, int status);
}
```

**FetchOrdersAsync:**
- `POST /order/getOrdersForApi` with `{ startDate, endDate, pageSize, pageNumber }`
- Tarih formatı: `YYYY-MM-DD` veya `YYYY-MM-DDThh:mm`
- Response: `PazaramaResponse<List<PazaramaOrderDto>>`

**UpdateOrderItemStatusAsync:**
- `PUT /order/updateOrderStatus` with `{ orderNumber, item: { orderItemId, status, deliveryType?, shippingTrackingNumber?, trackingUrl?, cargoCompanyId? } }`
- Statü 5 (Kargoya Verildi) için kargo bilgisi zorunlu

**BulkUpdateOrderStatusAsync:**
- `PUT /order/updateOrderStatusList` with `{ orderNumber, status }`
- Tüm item'ları aynı statüye günceller

### B. IPazaramaRefundService

```csharp
interface IPazaramaRefundService
{
    Task<IDataResult<PazaramaRefundListResponse>> GetRefundsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateRefundAsync(string refundId, int status, int? refundRejectType = null);
    Task<IDataResult<PazaramaCancelListResponse>> GetCancellationsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateCancellationAsync(string refundId, int status);
}
```

**GetRefundsAsync:** `POST /order/getRefund`
**UpdateRefundAsync:** `POST /order/updateRefund` — status 3 için `RefundRejectType` zorunlu
**GetCancellationsAsync:** `POST /order/api/cancel/items`
**UpdateCancellationAsync:** `PUT /order/api/cancel`

### C. OrderManager.ImportPazaramaOrdersAsync

Mevcut `OrderManager`'a yeni metod. N11 import pattern'ını takip eder:

1. Advisory lock (2005) al
2. Mevcut `OrderNumber`'ları batch yükle (dedup)
3. Barkod → ProductVariant batch lookup
4. Her sipariş için:
   - Dedup kontrolü
   - Pazarama DTO → Order entity map
   - Adres mapping (shipmentAddress, billingAddress)
   - OrderItem'ları oluştur (product eşleme)
   - Stok azaltma: `DecreaseStockAtomicAsync` (MarketPlaceWarehouse'dan)
5. SaveChanges

### D. PazaramaOrderPollingService

Background service:
- 2dk interval, 20sn startup delay
- `Pazarama:UseMock` kontrolü — mock ise polling devre dışı
- `ConcurrentDictionary<int, DateTimeOffset>` ile lastPollTime (multi-tenant ready)
- İlk çalışmada 1 gün geriye bak
- `FetchOrdersAsync(lastPoll, now)` → `ImportPazaramaOrdersAsync()`
- Hata durumunda log, devam

### E. PazaramaRefundPollingService

Background service:
- 5dk interval, 25sn startup delay
- Yeni iade talepleri (status=1, Onay Bekliyor) çek
- Yeni iptal talepleri (status=1) çek
- Sonuçları log'la — admin panelden yönetilecek
- İleride: otomatik onay kuralları eklenebilir

### F. Sipariş DTO'ları

`PazaramaOrderModels.cs`:
- `PazaramaOrderDto` — orderId, orderNumber, orderDate, orderAmount, paymentType, orderStatus, customer, items, addresses
- `PazaramaOrderItemDto` — orderItemId, orderItemStatus, deliveryType, quantity, product, prices (money objects), cargo, shipmentCode
- `PazaramaMoneyDto` — value, valueString, currency (money object helper)
- `PazaramaOrderItemUpdate` — orderItemId, status, deliveryType, shippingTrackingNumber, trackingUrl, cargoCompanyId
- `PazaramaRefundDto` — refundId, orderNumber, refundStatus, refundType, customer, product, amounts
- `PazaramaCancelDto` — same structure as refund
- `PazaramaRefundListResponse` — responsePage, pageReport, refundList
- `PazaramaCancelListResponse` — same structure

---

## Sprint Yapısı

| Sprint | Kapsam | Tahmini Test |
|--------|--------|-------------|
| 1 | Order/Refund/Cancel DTO'ları + IPazaramaOrderService | ~10 |
| 2 | OrderManager.ImportPazaramaOrdersAsync + PazaramaOrderPollingService | ~8 |
| 3 | IPazaramaRefundService + MockPazaramaRefundService | ~8 |
| 4 | PazaramaRefundPollingService + DI + Mock order service | ~6 |

---

## Dosya Envanteri

### Yeni Dosyalar (~14)
1. `Business/Concrete/Pazarama/PazaramaOrderModels.cs` — sipariş/iade/iptal DTO'ları
2. `Business/Abstract/IPazaramaOrderService.cs`
3. `Business/Concrete/Pazarama/PazaramaOrderService.cs`
4. `Business/Concrete/Pazarama/MockPazaramaOrderService.cs`
5. `Business/Abstract/IPazaramaRefundService.cs`
6. `Business/Concrete/Pazarama/PazaramaRefundService.cs`
7. `Business/Concrete/Pazarama/MockPazaramaRefundService.cs`
8. `Business/BackgroundServices/PazaramaOrderPollingService.cs`
9. `Business/BackgroundServices/PazaramaRefundPollingService.cs`
10. `Test/Entegrasyon.Test/Pazarama/PazaramaOrderServiceTests.cs`
11. `Test/Entegrasyon.Test/Pazarama/PazaramaOrderImportTests.cs`
12. `Test/Entegrasyon.Test/Pazarama/PazaramaRefundServiceTests.cs`

### Değişecek Dosyalar
1. `Business/Abstract/IOrderManager.cs` — ImportPazaramaOrdersAsync ekle
2. `Business/Concrete/OrderManager.cs` — ImportPazaramaOrdersAsync implementasyonu
3. `ApplicationBootstrap/ApplicationDependencyExtension.cs` — yeni servisler + BG servisleri DI

---

## Doğrulama

1. `dotnet build Entegrasyon.sln` — hatasız build
2. `dotnet test --filter "Pazarama"` — tüm yeni testler yeşil
3. Mevcut testler kırılmamış
4. Mock modda: sipariş polling devre dışı
5. Mock modda: order service mock sipariş listesi döner
