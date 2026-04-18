# POS Sepet (Genel) İndirimi — Tasarım

**Tarih:** 2026-04-19
**Durum:** Taslak, implementasyon için onay bekleniyor
**İlgili mevcut özellik:** `2026-04-18-pos-line-discount-design.md` (kalem bazlı indirim — zaten prod)

## Amaç

POS satış sırasında kasiyerin **sepet (genel) toplamı üzerinden** indirim uygulayabilmesi. Örnek davranış: toplam 1.500,00 TL → 250,00 TL indirim → **tam 1.250,00 TL**. Her kalemin KDV matrahı ve net fiyatı bu indirime göre otomatik ayarlanmalı (e-fatura ve iade uyumu).

## Gerekçe

Kalem bazlı indirim (`SaleItem.DiscountAmount`, `DiscountPercent`) zaten mevcut. Eksik parça: müşteri pazarlığı gibi senaryolarda sepet geneline uygulanacak indirim. `Sale.GeneralDiscount` alanı entity'de var ama POS akışı bu değeri `0` olarak gönderiyor ve `SaleManager.MakeSale` fiyat hesabında kullanmıyor.

## Kapsam Dışı

- Promosyon/kupon kodu sistemi (`DiscountVoucher` zaten ayrı bir akış)
- Kampanya motoru, otomatik indirim kuralları
- Online/B2B satış için genel indirim (şimdilik sadece POS)

## Kararlar Özeti

| Karar | Seçim | Gerekçe |
|---|---|---|
| Dağıtım stratejisi | **Pro-rata (orantısal)** | Her kalemin KDV matrahı doğru kalır, iade tutarı otomatik doğru hesaplanır |
| Kalem + sepet indirim etkileşimi | **Katmanlı** (önce kalem, sonra sepet) | Kasiyer en esnek kullanım; gerçek senaryoyla uyumlu |
| UI konumu | **Sepet altında** + ödeme dialog'unda "düzenle" butonu | Cart her zaman gerçek durumu gösterir, payment dialog'da ek erişim |
| Girdi tipleri | **Yüzde (%) ve TL** | Kalem indirimiyle tutarlı |
| Sebep/not | **Opsiyonel** | Kalem indirimiyle tutarlı, audit için |
| Persistans | Pro-rata **kalemlere dağıtılır** (`SaleItem.DiscountAmount` içine eklenir), `Sale.GeneralDiscount` bilgi/audit amaçlı tutulur | İade ve KDV mantığını değiştirmeden çalışır |
| `Sale.GeneralDiscount` tipi | `double` → `decimal(18,2)` (breaking migration) | Para alanında `double` hatalı; alan zaten kullanılmıyor, risk düşük |
| Sebep alanı konumu | Sadece `Sale` üzerinde (SaleItem'a kopyalanmaz) | Sepet indirimi tek mantıksal olay, denormalizasyon gereksiz |
| Distribüsyon lokasyonu | Controller (`POSController.CompleteSale`) | `MakeSaleDto` oluşturulmadan önce kalemler dağıtılmış halde geçer; business layer minimum değişir |
| Cart session şeması | `List<POSCartItemVm>` → **`POSCartVm`** (refactor) | Tek source-of-truth; cart temizliği tek yerden |

## Mimari Akış

```
POSCartVm (session: pos_cart JSON)
  ├─ Items: List<POSCartItemVm>        ← kalem bazlı indirim dahil
  ├─ GeneralDiscountType: "percent"|"amount"|null
  ├─ GeneralDiscountValue: decimal
  ├─ GeneralDiscountReasonId: int?
  └─ GeneralDiscountReasonNote: string?

         ↓ (CompleteSale)

POSController.BuildItemsWithDistributedDiscount()
  1. lineGrossAfterLine_i = Items[i].LineTotalWithVat (kalem indirimi uygulanmış, KDV dahil)
  2. subtotalAfterLine = Σ lineGrossAfterLine_i
  3. generalDiscount = (percent ? subtotalAfterLine*pct/100 : amount)
  4. shareGross_i = Round(lineGrossAfterLine_i / subtotalAfterLine * generalDiscount, 2)
  5. shareGross[last] += (generalDiscount - Σ shareGross) // rounding remainder
  6. shareNet_i = Round(shareGross_i / (1 + VatRate_i/100), 2)
  7. newDiscountAmount_i = (mevcut DiscountAmount veya %-den türetilen) + shareNet_i

         ↓

MakeSaleDto
  ├─ SaleItems[*].DiscountAmount: kalem + genel indirim birleşik, KDV hariç net
  ├─ SaleItems[*].DiscountPercent: 0 (normalize edildi, tek kaynağa TL)
  ├─ GeneralDiscount: toplam (KDV dahil, bilgi amaçlı)
  ├─ GeneralDiscountReasonId
  └─ GeneralDiscountReasonNote

         ↓

SaleManager.MakeSale → Sale entity'sine kaydedilir
```

## Invariant Kuralı

**Her senaryoda:**
`Σ(SaleItem.LineTotalWithVat) − Sale.GeneralDiscount == KullanıcıHedefToplamı`

Pro-rata adım 5'teki remainder düzeltmesi bu invariant'ı garantiler (kuruş sapması yok).

## Şema Değişiklikleri

### `Sale` entity

```csharp
// Mevcut alan — tip değişikliği
public decimal GeneralDiscount { get; set; }   // önceden: double

// YENİ alanlar
public int? GeneralDiscountReasonId { get; set; }
public DiscountReason? GeneralDiscountReason { get; set; }

[StringLength(200)]
public string? GeneralDiscountReasonNote { get; set; }
```

### DTO değişiklikleri

```csharp
public record MakeSaleDto(
    Guid SalePersonId,
    int? CustomerId,
    decimal GeneralDiscount,            // önceden: double
    int? GeneralDiscountReasonId,       // YENİ
    string? GeneralDiscountReasonNote,  // YENİ
    int BranchOfficeId,
    SaleSource SaleSource,
    string? Note,
    List<SaleItemDto> SaleItems,
    List<SalePaymentDto> Payments);
```

### EF Migration

`AddSaleGeneralDiscountAndReason`:
- `Sale.GeneralDiscount`: `double` → `decimal(18,2)`
- `Sale.GeneralDiscountReasonId`: `int?`, FK → `DiscountReason(Id)`
- `Sale.GeneralDiscountReasonNote`: `nvarchar(200)?`

Migration'dan önce mevcut verinin `GeneralDiscount` değerleri `double`'dan `decimal`'e PostgreSQL-level cast ile taşınır (değerler hep `0` olduğu için veri kaybı riski yok).

## ViewModel Değişiklikleri

### `POSCartVm` (genişletilir)

```csharp
public class POSCartVm
{
    public List<POSCartItemVm> Items { get; set; } = [];
    public string? GeneralDiscountType { get; set; }   // "percent" | "amount" | null
    public decimal GeneralDiscountValue { get; set; }
    public int? GeneralDiscountReasonId { get; set; }
    public string? GeneralDiscountReasonName { get; set; }
    public string? GeneralDiscountReasonNote { get; set; }

    public decimal SubtotalAfterLineDiscount => Items.Sum(i => i.LineTotalWithVat);

    public decimal GeneralDiscountAmount =>
        GeneralDiscountType switch
        {
            "percent" => Math.Round(SubtotalAfterLineDiscount * GeneralDiscountValue / 100m, 2),
            "amount"  => Math.Min(GeneralDiscountValue, SubtotalAfterLineDiscount),
            _         => 0m
        };

    public decimal GrandTotalAfterAllDiscounts => SubtotalAfterLineDiscount - GeneralDiscountAmount;
    public bool HasGeneralDiscount => GeneralDiscountAmount > 0;
}
```

### `POSPaymentDialogVm`

Yeni alanlar: `GeneralDiscountAmount`, `GeneralDiscountReasonName`, `SubtotalBeforeGeneralDiscount`.

### Yeni: `POSCartDiscountDialogVm`

```csharp
public class POSCartDiscountDialogVm
{
    public decimal SubtotalAfterLineDiscount { get; set; }
    public string? CurrentType { get; set; }
    public decimal CurrentValue { get; set; }
    public int? CurrentReasonId { get; set; }
    public string? CurrentNote { get; set; }
    public List<DiscountReason> Reasons { get; set; } = [];
}
```

## UI Akışı

### Cart footer (`_POSCart.cshtml`)

```
Ara Toplam        | 3 adet   | 1.500,00 TL
KDV               |          |   270,00 TL  (opsiyonel ayrıntı)
────────────────────────────────────────────
Sepet İndirimi    | %16.67   |  -250,00 TL  [✕ kaldır]
────────────────────────────────────────────
TOPLAM            |          | 1.250,00 TL  [🏷️ düzenle]
```

- Sepet boşsa "Sepet İndirimi" satırı render edilmez.
- "İndirim uygula/düzenle" butonu modalı açar (HTMX `hx-get="/pos/cart-discount-dialog"`).
- Kaldır butonu `hx-post="/pos/clear-cart-discount"` çağırır.

### Modal (`_POSCartDiscountModal.cshtml`)

Kalem indirimi modalının (`_POSLineDiscountModal`) birebir analogu:
- Yüzde/TL toggle
- Değer input'u (IMask ile para formatı)
- Sebep dropdown (aktif `DiscountReason` listesi)
- Not textarea (200 karakter sınırı)
- "Uygula" ve "Kaldır" butonları

### Payment dialog (`_POSPaymentDialog.cshtml`)

Özet bölümüne yeni satırlar:
```
Ara Toplam         1.500,00 TL
Sepet İndirimi     -250,00 TL   (🏷️ Sadakat müşterisi)
Ödenecek Tutar     1.250,00 TL
```

`HasGeneralDiscount` true ise "İndirim düzenle" butonu gösterilir → `hx-get="/pos/cart-discount-dialog"` modalı açar → modal kaydedince payment dialog yeniden render edilir (HTMX trigger ile).

## Controller Action'ları

```csharp
[HttpGet("/pos/cart-discount-dialog")]
public async Task<IActionResult> CartDiscountDialog()
    → PartialView("Partials/_POSCartDiscountModal", vm)

[HttpPost("/pos/apply-cart-discount")]
[ValidateAntiForgeryToken]
public IActionResult ApplyCartDiscount(
    [FromForm] string discountType,
    [FromForm] decimal? percent,
    [FromForm] decimal? amount,
    [FromForm] int? reasonId,
    [FromForm] string? note)
    → PartialView("Partials/_POSCart", cartVm)  // footer dahil tüm cart

[HttpPost("/pos/clear-cart-discount")]
[ValidateAntiForgeryToken]
public IActionResult ClearCartDiscount()
    → PartialView("Partials/_POSCart", cartVm)
```

**Session refactor:** `GetCartFromSession` / `SaveCartToSession` artık `POSCartVm` (list yerine) serialize/deserialize eder. Tüm mevcut cart action'ları bu refactor'dan etkilenir ama mantık değişmez — `cart.Items`'a erişim tek ek nokta.

## Business Layer Değişiklikleri

`SaleManager.MakeSale`: **minimal**. Pro-rata dağıtım zaten controller'da yapıldığı için `SaleManager` kalemleri olduğu gibi kaydeder. Tek eklenenler:
- `Sale.GeneralDiscount`, `GeneralDiscountReasonId`, `GeneralDiscountReasonNote` alanlarını map et (Mapperly zaten isim uyumuyla yakalar).
- `ApplicationLog`: genel indirim varsa `AddLog($"Sepet indirimi: {dto.GeneralDiscount:N2} TL", LogType.Sale, LogAction.Add)` eklenir.

## Validasyon

**FluentValidation — `MakeSaleDtoValidator`:**
- `GeneralDiscount >= 0`
- `GeneralDiscount > 0` ise: `SaleItems.Sum(i => i.UnitPrice * i.Quantity * (1 + i.TaxPercentage/100))` üzerinden büyük olamaz

**LogicRunner — business rules:**
- `GeneralDiscountReasonId.HasValue` → DB'de `IsActive=true` olan bir `DiscountReason` var mı?

**Controller (server-side):**
- `percent` mode: 1-100 arası değilse toast error
- `amount` mode: > 0 ve ≤ `SubtotalAfterLineDiscount` olmalı
- Sepet boşken `CartDiscountDialog` açılmaz (HTMX `Reswap("none")` + toast)
- `ClearCart` action'ında genel indirim alanları da sıfırlanır

## Sale Detail Görünümü

`SaleDetailDto` zaten `GeneralDiscount` içeriyor. `SaleDetail.cshtml` view'ında ek satır eklenir:
```
Ara Toplam       1.500,00 TL
Sepet İndirimi  -250,00 TL    (Sadakat müşterisi — "ilk alışveriş")
Genel Toplam    1.250,00 TL
```

İade işlemi (`SaleReturnManager`) değişiklik gerektirmiyor — her kalemin `DiscountAmount`'ı pro-rata pay eklenmiş halde; mevcut iade tutar hesabı (`UnitPrice * Quantity − DiscountAmount`) otomatik doğru çalışır.

## Test Stratejisi

### Unit (`Test/Entegrasyon.Test`)

`PosCartDiscountDistributionTests.cs`:
- Tek kalem, %16.67 → hedef tutara tam düşer
- İki kalem eşit fiyatta, 100 TL → her biri 50 TL
- Üç farklı KDV'li kalem (%20/%1/%10), 250 TL → KDV matrahı doğru
- Yuvarlama remainder: 3 eşit kalem, 10 TL → 3.33 + 3.33 + 3.34
- **Invariant testi:** her senaryoda `Σ(LineTotalWithVat − shareGross) == GrandTotalAfterAllDiscounts` (±0.00)
- Katmanlı: kalem bazlı %10 + sepet 100 TL → beklenen kombine değer

`MakeSaleDtoValidatorTests.cs`:
- `GeneralDiscount < 0` invalid
- `GeneralDiscount > subtotal` invalid
- `GeneralDiscountReasonId` DB'de yoksa business rule fail

### Integration (`Test/Entegrasyon.IntegrationTest`)

`SaleWithGeneralDiscountTests.cs`:
- `MakeSale` sepet indirimi ile → DB'de `Sale.GeneralDiscount` + kalemlerin `DiscountAmount` pro-rata dağıtılmış
- İade: kalem iade → tutar = indirilmiş satır toplamı
- `GetSalesPageable` total = `subtotalAfterLine − generalDiscount` (±0.01)

### E2E (`Test/Entegrasyon.E2E`)

`POSCartDiscountE2ETests.cs` (Playwright + NUnit):
- Ürün ekle → "İndirim" modalı → %10 uygula → footer'da görünür, TOPLAM düşer
- Payment dialog aç → "Ödenecek Tutar" düşmüş → "Düzenle" butonu → modal → güncelle → dialog yeniden render
- Satışı tamamla → SaleDetail sayfasında sepet indirimi görünür

### MVC (`Test/Entegrasyon.MVC.Test`)

`POSControllerDiscountTests.cs`:
- `CartDiscountDialog` GET → partial + aktif reasons
- `ApplyCartDiscount` POST invalid percent (150) → toast error
- `ApplyCartDiscount` POST valid amount → session update, cart partial
- `ClearCartDiscount` POST → session temizlenir
- Sepet boşken `CartDiscountDialog` → 204 + reswap="none"

## Dosya Değişiklikleri Özeti

**Yeni dosyalar:**
- `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCartDiscountModal.cshtml`
- `Application/Entegrasyon.DataAccess/Migrations/XXXX_AddSaleGeneralDiscountAndReason.cs`
- `Test/Entegrasyon.Test/Business/Sales/PosCartDiscountDistributionTests.cs`
- `Test/Entegrasyon.IntegrationTest/Sales/SaleWithGeneralDiscountTests.cs`
- `Test/Entegrasyon.E2E/POS/POSCartDiscountE2ETests.cs`
- `Test/Entegrasyon.MVC.Test/Features/POS/POSControllerDiscountTests.cs`

**Düzenlenen dosyalar:**
- `Application/Entegrasyon.Entity/Sales/Sale.cs`
- `Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs`
- `Application/Entegrasyon.Business/Concrete/SaleManager.cs` (minimal)
- `Application/Entegrasyon.Business/Mappers/SaleMapper.cs` (yeni alanlar)
- `Application/Entegrasyon.Business/Validation/FluentValidation/MakeSaleDtoValidator.cs`
- `Application/Entegrasyon.MVC/Features/POS/POSController.cs` (3 yeni action + cart refactor)
- `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs`
- `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml` (footer)
- `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml` (sepet indirimi satırı)

## Riskler ve Önlemler

| Risk | Önlem |
|---|---|
| Prod DB'de `Sale.GeneralDiscount` `double` değerlerinin kayıp/bozulması | Migration öncesi veri audit'i; tüm değerler `0` ise cast güvenli |
| Yuvarlama kuruş sapması (1500 → 1249.99) | Remainder son kaleme yazılır → invariant test garantisi |
| Cart session JSON şema değişikliği → eski açık oturumlarda deserialize hatası | Try-catch ile eski list şeması da desteklenir; deserialize fail olursa boş cart döner |
| Kalem + sepet indirimi kombinasyonunda toplam negatif olur | Controller validation: `generalDiscount ≤ subtotalAfterLine` |
| FluentValidation rule'u sunucu tarafında atlanırsa toplam negatif | Belt-and-suspenders: `SaleManager.MakeSale` içinde de invariant kontrol |

## TDD Sırası (İmplementasyon rehberi)

1. Unit test: `PosCartDiscountDistributionTests` — yazma öncesi RED
2. Distribüsyon helper'ı (pure function) — GREEN
3. EF migration + DTO + entity değişiklikleri
4. ViewModel genişletmesi
5. Controller action'ları + cart session refactor
6. Partial view'lar
7. MVC controller testleri
8. Integration testler
9. E2E testler
10. Manuel debug (browser'da gerçek kullanıcı gibi — feedback_debug_before_done kuralı)
