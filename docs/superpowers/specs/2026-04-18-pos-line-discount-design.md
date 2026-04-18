# POS Ürün Başı İndirim — Tasarım

**Tarih:** 2026-04-18
**Paket:** C (POS iyileştirme serisi — 3. paket)

## Kararlar (onaylı)

1. **% veya TL, biri-ya-diğeri.** Aynı anda ikisi birden değer taşıyamaz.
2. İndirim nedeni: opsiyonel **dropdown** (sabit liste) + opsiyonel **serbest not**.
3. **Sadece POS akışında.** Manual/marketplace satışlarında yok. Ürün zaten indirimli (`SalePrice < ListPrice`) ise uyarı modal'ı çıkar: "Bu ürün zaten indirimde. Yine de indirim yapmak ister misiniz?"
4. Yetki: `Permissions.Sales.Create` olan herkes verebilir. Limit yok (ileride).

## Veri Modeli

### Mevcut `SaleItem`
```csharp
public double DiscountPercent { get; set; }   // zaten var, kalır
```

### Yeni Kolonlar
```csharp
public decimal? DiscountAmount { get; set; }     // TL tipi indirim (tek bir kalem başına)
public int? DiscountReasonId { get; set; }       // FK → DiscountReasons
public DiscountReason? DiscountReason { get; set; }
[StringLength(200)]
public string? DiscountReasonNote { get; set; }  // serbest not
```

### Yeni Entity: `DiscountReason`
```csharp
public sealed class DiscountReason : BaseEntity
{
    public int Id { get; set; }
    [StringLength(100)] public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
```

### Seed (migration içinde)
- "Müşteri isteği"
- "Ürün hasarı"
- "Promosyon"
- "Çalışan indirimi"
- "Toplu alım"
- "Vade indirimi"

### Validation (Business)
- `DiscountPercent > 0` VE `DiscountAmount > 0` aynı anda olamaz (FluentValidation kuralı)
- `DiscountPercent` aralığı: [0, 100]
- `DiscountAmount` ≥ 0 ve < `UnitPrice * Quantity`

DB tarafı: uygulama katmanı validation yeter, CHECK constraint eklemiyoruz (migration hafif kalsın).

## Hesaplama

Satır toplamı:
```
lineGross = UnitPrice × Quantity
lineDiscount = DiscountAmount ?? (lineGross × DiscountPercent / 100)
lineNet = lineGross − lineDiscount
```

Sonra `Sale.GeneralDiscount` uygulanır (cascade):
```
totalAfterLine = Σ(lineNet)
grandTotal = totalAfterLine × (1 − GeneralDiscount / 100)
```

## POS UI Akışı

### Cart Satırı
Her sepet satırının sağında yeni küçük buton:
```
[ürün adı]     [qty − +]    [fiyat]     [%% ▾]
                                         ↑ İndirim tetikleyici
```

Tıklayınca inline popover veya küçük modal açılır.

### İndirim Modal (partial: `_POSLineDiscountModal.cshtml`)
```
┌─────────────────────────────────┐
│ İndirim Uygula — "Kalem Adı"    │
│                                 │
│ ⚠ Bu ürün zaten indirimde       │ ← sadece SalePrice<ListPrice ise
│   (Liste ₺50 → Satış ₺40)       │
│                                 │
│ (●) Yüzde   ( ) TL              │
│ [_______] %                     │
│                                 │
│ Neden (opsiyonel):              │
│ [Müşteri isteği      ▾]         │
│                                 │
│ Not (opsiyonel):                │
│ [___________________________]   │
│                                 │
│                  [İptal] [Uygula]│
└─────────────────────────────────┘
```

Apply sonrası session'daki cart item'a indirim state'i yazılır, subtotal güncellenir.

## Session State Güncellemesi

`POSCartItemVm` (mevcut) içine ekle:
```csharp
public double DiscountPercent { get; set; }
public decimal? DiscountAmount { get; set; }
public int? DiscountReasonId { get; set; }
public string? DiscountReasonNote { get; set; }
```

Checkout'ta `SaleItemDto` doldurulurken bu alanlar `MakeSaleDto`'ya aktarılır.

## Dosyalar

**Yeni:**
- `Entegrasyon.Entity/Sales/DiscountReason.cs`
- `Entegrasyon.DataAccess/.../EntityConfigurations/DiscountReasonEntityConfiguration.cs`
- Migration: `AddLineDiscountToSaleItem`
- `Entegrasyon.Business/Abstract/IDiscountReasonManager.cs` (sadece `GetActiveAsync`)
- `Entegrasyon.Business/Concrete/DiscountReasonManager.cs`
- `Entegrasyon.Business/Validation/FluentValidation/SaleItemDtoValidator.cs` (% vs TL XOR kuralı)
- `Entegrasyon.MVC/Features/POS/Views/Partials/_POSLineDiscountModal.cshtml`

**Değiştirilen:**
- `Entegrasyon.Entity/Sales/SaleItem.cs` — 3 yeni alan
- `Entegrasyon.Entity/Dtos/Sale/SaleItemDto.cs` — 3 yeni alan
- `Entegrasyon.Business/Mappers/SaleMapper.cs` — yeni alanlar mapping
- `Entegrasyon.DataAccess/.../IntegrationDbContext.cs` — `DbSet<DiscountReason>`
- `Entegrasyon.MVC/Features/POS/ViewModels/POSCartItemVm.cs` — 4 yeni alan
- `Entegrasyon.MVC/Features/POS/POSController.cs` — yeni action `/pos/apply-line-discount` (HTMX)
- `Entegrasyon.MVC/Features/POS/Views/Index.cshtml` — cart satırında indirim butonu
- `Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml` — satır indirimi kolonu göster

## Test

- **Unit:** `SaleItemDtoValidator` — % ve TL aynı anda → invalid; geçerli kombinasyonlar → valid.
- **Unit:** `SaleManager.MakeSale` — SaleItem'a DiscountAmount/Reason doğru yazılır.
- **Manuel E2E:** POS cart → satır indirimi uygula (%10) → toplam doğru → satış yap → Sale Detail'da indirim görünür.

## Başarı Kriterleri

1. POS cart'ta bir satıra %20 veya 5 TL indirim uygulanabilir.
2. Aynı anda ikisi girilmeye çalışılırsa uygulama engeller.
3. İndirimli ürüne indirim vermeye çalışınca uyarı modal'ı çıkar, "Yine de Uygula" denirse devam eder.
4. Satış kaydedildiğinde `SaleItem.DiscountAmount` ve `DiscountReasonId` DB'de saklanır.
5. Sale detay sayfasında her kalemin indirimi görünür.

## Kapsam Dışı

- Raporlama / istatistik (ayrı iş)
- Yetki limiti / onay akışı (ileride)
- Kupon/voucher entegrasyonu (zaten ayrı sistem)
- İndirim üstüne indirim hesabı (cascade; basit tutuldu)
