# POS Beklenen Kasa Bug Fix — Tasarım

**Tarih:** 2026-04-18
**Durum:** Auto-onaylı
**Paket:** A (POS iyileştirme serisinin ilk paketi)

## Problem

POS ekranında satış yapıldığında üstte gösterilen "Beklenen Kasa" değeri güncellenmiyor. Oturum boyunca açılış kasası değerinde sabit kalıyor.

## Root Cause

`POSSessionManager.GetSessionSummaryAsync` beklenen kasayı şöyle hesaplıyor:

```csharp
var totalCash = salePayments
    .Where(p => p.PaymentMethod.SystemCode == "Cash")
    .Sum(p => p.Amount);
// ...
var expectedCash = session.OpeningCash + totalCash + totalCashIn - totalCashOut;
```

`salePayments` filtresi: `p.SaleId` IN `saleIds`, `saleIds` ise **`POSTransactions` tablosundan** geliyor:

```csharp
var transactions = await dbContext.POSTransactions
    .Where(t => t.POSSessionId == sessionId)
    .Include(t => t.Sale).ThenInclude(s => s.SaleItems)
    .ToListAsync();
var saleIds = transactions.Select(t => t.SaleId).ToList();
```

**`POSController.Checkout`**'te `saleManager.MakeSale` çağrılıyor ama **`POSTransaction` satırı hiç oluşturulmuyor**. Sonuç: summary 0 transaction görüyor → totalCash=0 → expectedCash = OpeningCash (sabit).

## Fix

İki değişiklik:

1. **`IPOSSessionManager`'a yeni metod ekle:**

```csharp
Task<IResult> AddTransactionRecordAsync(
    long sessionId,
    Guid saleId,
    decimal cashReceived,
    decimal changeGiven);
```

Bu metod `POSTransaction` satırı oluşturur, `Sale` ile `POSSession`'ı ilişkilendirir. `MakeSale`'i tekrar çağırmaz — sadece link satırı yazar.

2. **`POSController.Checkout`'ta satış başarılı olduktan sonra çağır:**

```csharp
var saleResult = await saleManager.MakeSale(makeSaleDto);
if (!saleResult.Success) { /* error */ }

// YENİ: POSTransaction link satırı oluştur
var cashPayment = payments.FirstOrDefault(p => /* nakit ödeme */);
var cashReceived = cashPayment?.CashReceived ?? 0;
var changeGiven  = cashPayment?.CashReceived.HasValue == true
    ? Math.Max(0, cashPayment.CashReceived.Value - cashPayment.Amount)
    : 0;
await posSessionManager.AddTransactionRecordAsync(
    sessionResult.Data.Id, saleResult.Data, cashReceived, changeGiven);
```

Kart-only ödemelerde `cashReceived` ve `changeGiven` = 0 olur — summary doğru hesaplar çünkü `totalCash` `SalePayments.PaymentMethod.SystemCode == "Cash"` üzerinden gelir, `POSTransaction.CashReceived` alanı sadece bilgi amaçlı tutulur.

## Neden Mevcut `RecordTransactionAsync` Kullanılmıyor

Mevcut `RecordTransactionAsync`:
- İçinde `MakeSale`'i kendi çağırır (sale ikinci kez yaratılır, stock iki kez düşer).
- `PaymentMethod` tek değer bekler (enum); POSController ise `List<SalePaymentDto>` ile multi-payment destekliyor.

Uyumsuz. Yeni minimal metod tercih edildi. Mevcut `RecordTransactionAsync` deprecate yorumu alacak (hiç kullanımı yok çünkü Scrutor auto-scan sadece interface → concrete register ediyor; bu metod hiçbir yerden çağrılmıyor — ileride temizlenebilir ama bu iş kapsamında dokunmuyoruz).

## Test

- **Unit:** `POSSessionManagerTests.AddTransactionRecordAsync_CreatesLinkRow` — session + sale seed, metod çağrısı, DB'de POSTransaction satırı kontrolü.
- **Integration:** `POSCheckoutIntegrationTests.Checkout_UpdatesExpectedCash_AfterSale` — login → session aç → ürün sepetlenip nakit ödeme → index sayfasında `Model.Summary.ExpectedCash > OpeningCash` kontrolü.

## Başarı Kriterleri

1. POS oturumu açık iken bir nakit satışı yapılır → `/pos` sayfasında "Beklenen Kasa" değeri `OpeningCash + satış tutarı` olur.
2. Kart-only satışı → beklenen kasa değişmez (cash-only etkisi yok), ama işlem sayacı artar.
3. X-Report ve Z-Report'ta da doğru beklenen kasa görünür.
4. Mevcut unit testler regresyonsuz.

## Kapsam Dışı

- "Beklenen Kasa" canlı/SSE ile güncelleme (full page refresh yeterli)
- Mevcut `RecordTransactionAsync` temizliği
- Karma ödemelerde (nakit+kart) `changeGiven` edge case'leri — mevcut davranış korunur
