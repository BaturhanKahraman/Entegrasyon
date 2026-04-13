# Satış Modülü Yeniden Tasarım Spec

**Tarih:** 2026-04-13
**Yaklaşım:** B — Refactor Core (Sale entity merkez, mevcut altyapı korunarak genişletme)
**Kapsam:** POS yeniden tasarım + Satış listesi + Ödeme yöntemi yapılandırma + Satış detay + İade akışı + X/Z Raporu

---

## 1. Entity Modeli

### 1.1 Yeni Entity'ler

#### SalePayment

Split payment destekli ödeme kaydı. Bir Sale'e N tane SalePayment bağlanabilir.

```csharp
public class SalePayment : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethodDefinition PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }           // numeric(18,2)
    public decimal? CashReceived { get; set; }    // Nakit ödemelerde alınan tutar
    public decimal? ChangeGiven { get; set; }     // Para üstü
    public string? CardAuthCode { get; set; }     // Kart provizyon kodu
    public string? TransactionRef { get; set; }   // Harici referans
    public DateTimeOffset PaidAt { get; set; }
}
```

#### PaymentMethodDefinition

Ayarlardan yönetilen, tenant bazlı ödeme yöntemleri.

```csharp
public class PaymentMethodDefinition : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";           // "Nakit", "Kredi Kartı" vb.
    public string SystemCode { get; set; } = "";      // "Cash", "CreditCard", "DebitCard", "MealCard", "BankTransfer"
    public string Icon { get; set; } = "";            // Tabler icon: "ti ti-cash", "ti ti-credit-card"
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public decimal? CommissionRate { get; set; }      // Komisyon oranı (%)
    public bool RequiresAuthCode { get; set; }        // Kart provizyon kodu gerekli mi
    public bool RequiresCashInput { get; set; }       // Nakit alınan tutar girişi gerekli mi
    public int TenantId { get; set; }
}
```

Seed data (varsayılan ödeme yöntemleri):

| SystemCode | Name | Icon | RequiresAuthCode | RequiresCashInput |
|---|---|---|---|---|
| Cash | Nakit | ti ti-cash | false | true |
| CreditCard | Kredi Kartı | ti ti-credit-card | true | false |
| DebitCard | Banka Kartı | ti ti-credit-card | true | false |
| MealCard | Yemek Kartı | ti ti-tools-kitchen-2 | true | false |
| BankTransfer | Havale/EFT | ti ti-building-bank | true | false |

#### SaleReturn

İade ana kaydı. Onay akışlı.

```csharp
public class SaleReturn : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public DateTimeOffset ReturnDate { get; set; }
    public Guid ReturnedByUserId { get; set; }
    public ApplicationUser ReturnedBy { get; set; } = null!;
    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public ReturnStatus ReturnStatus { get; set; }
    public string ReturnReason { get; set; } = "";
    public int? RefundPaymentMethodId { get; set; }
    public PaymentMethodDefinition? RefundPaymentMethod { get; set; }
    public decimal RefundAmount { get; set; }     // numeric(18,2)
    public string? Note { get; set; }
    public ICollection<SaleReturnItem> Items { get; set; } = [];
}
```

#### SaleReturnItem

İade kalemi. Kısmi iade destekli.

```csharp
public class SaleReturnItem : BaseEntity
{
    public long Id { get; set; }
    public long SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;
    public Guid SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
```

### 1.2 Enum'lar

```csharp
public enum SaleSource { POS = 1, Manual = 2 }

public enum SaleStatus { Completed = 1, PartialReturn = 2, FullReturn = 3, Cancelled = 4 }

public enum ReturnStatus { Pending = 1, Approved = 2, Rejected = 3 }
```

### 1.3 Mevcut Entity Değişiklikleri

#### Sale — Yeni alanlar

```csharp
// Yeni alanlar
public string SaleNumber { get; set; } = "";          // Benzersiz fiş numarası (auto-generate)
public DateTimeOffset SaleDate { get; set; }
public SaleSource SaleSource { get; set; }
public SaleStatus SaleStatus { get; set; } = SaleStatus.Completed;
public string? Note { get; set; }

// Yeni navigation'lar
public ICollection<SalePayment> Payments { get; set; } = [];
public ICollection<SaleReturn> Returns { get; set; } = [];
```

#### SaleItem — Yeni alanlar

```csharp
// Denormalize alanlar (fiş yazdırma, satış listesi gösterimi için)
public string Barcode { get; set; } = "";
public string ProductTitle { get; set; } = "";

// İade takibi
public int ReturnedQuantity { get; set; }  // default 0, iade yapıldıkça artar
```

#### POSTransaction — Değişiklikler

```
- PaymentMethod enum KALDIRILIR (ödeme bilgisi SalePayment'a taşındı)
- CashReceived, ChangeGiven KALIR (POS-spesifik para üstü bilgisi)
- CardAuthCode KALDIRILIR (SalePayment'a taşındı)
+ Sadece POS oturum bağlantısı olarak kalır (SessionId + SaleId)
```

### 1.4 KDV Gösterim Kuralı

- DB'de fiyatlar KDV HARİÇ tutulmaya devam eder (ProductVariant.SalePrice, SaleItem.UnitPrice)
- UI'da HER YERDE KDV DAHİL gösterilir: `KdvDahilFiyat = UnitPrice * (1 + VatRate / 100)`
- ViewModel'larda `UnitPriceWithVat` computed property eklenir
- Satış listesi, POS sepeti, satış detayı, fiş yazdırma — hepsinde KDV dahil
- KDV dökümü ayrıca gösterilir (oran bazlı gruplama: %1, %10, %20)

---

## 2. POS Terminali Yeniden Tasarım

### 2.1 Kasa Açılış

Mevcut akış korunur: Başlangıç kasası + Terminal ID girişi → POSSession oluştur.

### 2.2 Satış Akışı

Mevcut HTMX-based akış korunur, iyileştirmeler:

- Ürün arama: Barkod okuma + metin arama (mevcut)
- Sepet: Session-based cart (mevcut), fiyatlar KDV DAHİL gösterilir
- Müşteri seçimi: POS'a da müşteri seçme eklenir (Sales'teki gibi, opsiyonel)

### 2.3 Ödeme Ekranı (Yeniden Tasarım)

Mevcut hardcoded 3-tab yapısı (Nakit/Kredi Kartı/Karma) yerine:

- `PaymentMethodDefinition` tablosundan aktif yöntemler dinamik yüklenir
- Her yöntem kendi input'larını gösterir (RequiresCashInput → nakit girişi, RequiresAuthCode → provizyon kodu)
- Split payment: "Ödeme Ekle" butonu ile birden fazla ödeme yöntemi eklenebilir
- Kalan tutar otomatik hesaplanır
- Toplam ödeme >= GrandTotal olduğunda "Tamamla" aktif olur

### 2.4 Kasa Kapama

Mevcut akış korunur + X/Z raporu desteği eklenir.

---

## 3. X/Z Raporu

### 3.1 X Raporu (Anlık Kasa Durumu)

- Endpoint: `GET /pos/x-report`
- Kasa kapanmaz, sadece anlık durum gösterilir
- İçerik:
  - Kasa açılış bilgisi (kasiyer, tarih, başlangıç kasası)
  - İşlem sayısı
  - Ödeme yöntemi bazlı dağılım (nakit, kart, yemek kartı vb.)
  - KDV dökümü (oran bazlı)
  - İade sayısı ve tutarı
  - Beklenen kasa (açılış + nakit satış - nakit iade - kasa çıkış)
- Yazdırılabilir sayfa

### 3.2 Z Raporu (Gün Sonu)

- Kasa kapama işlemiyle birlikte oluşturulur
- X raporu içeriği + gerçek kasa tutarı + fark
- Kasiyer bazlı döküm (birden fazla vardiya varsa)
- Yazdırılabilir sayfa
- Rapor verisi `POSSession` kapanış bilgisinden türetilir

---

## 4. Satış Listesi Sayfası

### 4.1 Özet Kartlar (Dashboard)

Sayfa üstünde 4 özet kart:

| Kart | Hesaplama |
|------|-----------|
| Toplam Satış | Filtrelenen dönemdeki GrandTotal toplamı (KDV dahil) |
| Satış Adedi | Toplam fiş sayısı |
| Ortalama Sepet | Toplam Satış / Satış Adedi |
| İade Tutarı | Toplam iade edilen tutar |

### 4.2 Filtreler

- Tarih aralığı (varsayılan: bugün) + hızlı filtreler (bugün, dün, bu hafta, bu ay)
- Metin arama (fiş no, müşteri adı, barkod)
- Kaynak: POS / Manuel / Tümü
- Durum: Tamamlandı / Kısmi İade / Tam İade / İptal
- Ödeme yöntemi
- Kasiyer/Personel
- Şube

### 4.3 Tablo Sütunları

| Sütun | Açıklama |
|-------|----------|
| Fiş No | SaleNumber (tıklanabilir satır → detay) |
| Tarih | SaleDate (saat dahil) |
| Kaynak | SaleSource badge (POS / Manuel) |
| Müşteri | CustomerName veya "—" |
| Kasiyer | SalePerson adı |
| Ürün Adedi | TotalItemCount |
| Tutar | GrandTotal (KDV dahil) |
| Ödeme | Ödeme yöntemi/yöntemleri badge'leri |
| Durum | SaleStatus badge |

Tıklanabilir satır standardı: `<tr data-href="/sales/@item.Id">`

### 4.4 HTMX Pagination

Mevcut pattern: `_SaleTable.cshtml` partial, HTMX ile sayfalandırma.

---

## 5. Satış Detay Sayfası

### 5.1 Layout

Üstte satış bilgi kartı, altında 3 bölüm:

#### Satış Bilgileri Kartı
- Fiş No, Tarih, Kaynak (POS/Manuel), Kasiyer, Şube, Müşteri (varsa), Durum badge

#### Kalemler Tablosu
- Ürün (başlık + barkod), Adet, Birim Fiyat (KDV dahil), KDV Oranı, Satır Toplamı (KDV dahil), İade Durumu
- İade edilen kalemler farklı renk/strikethrough

#### Ödeme Bilgileri
- Her SalePayment bir satır: Ödeme Yöntemi, Tutar, Provizyon Kodu (varsa), Tarih
- Split payment durumunda birden fazla satır

#### KDV Dökümü
- Oran bazlı gruplama tablosu: KDV Oranı | Matrah | KDV Tutarı | Toplam
- Footer: Genel Toplam

### 5.2 Aksiyonlar

- **İade Et** butonu → İade modal'ı açar
- **Yazdır** butonu → Yazdırılabilir fiş sayfası
- **İptal Et** butonu (sadece bugünkü satışlar, yetkili onayı ile)

---

## 6. İade Akışı

### 6.1 İade Başlatma

Satış detay sayfasından "İade Et" butonu → Modal dialog:

1. İade edilecek kalemleri seç (checkbox + adet girişi)
   - Maksimum adet = Satılan adet - Daha önce iade edilen adet
2. İade nedeni seç/yaz (zorunlu)
3. İade ödeme yöntemi seç (nakit iade, karta iade vb.)
4. Otomatik hesaplanan iade tutarı göster (KDV dahil)
5. "İade Talebi Oluştur" butonu

### 6.2 Onay Akışı

- İade talebi oluşturulunca `ReturnStatus = Pending`
- Yetkili kullanıcı satış detay sayfasından onaylar/reddeder
- Onay sonrası:
  - `SaleItem.ReturnedQuantity` güncellenir
  - Stok geri artırılır (`IOfficeStockManager`)
  - `Sale.SaleStatus` güncellenir (PartialReturn veya FullReturn)
  - ApplicationLog kaydı oluşturulur

### 6.3 Değişim

İade + yeni satış olarak modellenir. Ayrı bir entity/akış yok — kullanıcı önce iade yapar, sonra yeni satış yapar. Bu en basit ve anlaşılır yaklaşım.

---

## 7. Ödeme Yöntemi Ayarları Sayfası

### 7.1 Konum

Settings/Ayarlar feature'ı altında yeni bir sekme veya sayfa.

### 7.2 UI

- Sıralanabilir liste (SortableJS ile drag-and-drop)
- Her satır: İkon, Ad, SystemCode (readonly), Aktif/Pasif toggle, Komisyon Oranı, Düzenle butonu
- Yeni ödeme yöntemi ekleme (modal)
- Silme yok — sadece pasif yapma (veri bütünlüğü)

### 7.3 Seed Data

İlk migration'da varsayılan 5 ödeme yöntemi seed edilir (Nakit, Kredi Kartı, Banka Kartı, Yemek Kartı, Havale/EFT).

---

## 8. Business Layer Değişiklikleri

### 8.1 Yeni Interface'ler

```csharp
public interface ISalePaymentService
{
    Task<IDataResult<List<PaymentMethodDefinition>>> GetActivePaymentMethodsAsync(int tenantId);
    Task<IResult> UpdatePaymentMethodAsync(int id, UpdatePaymentMethodDto dto);
    Task<IResult> TogglePaymentMethodAsync(int id);
    Task<IResult> ReorderPaymentMethodsAsync(List<int> orderedIds);
}

public interface ISaleReturnManager
{
    Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto);
    Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId);
    Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason);
    Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId);
}
```

### 8.2 Mevcut Interface Değişiklikleri

```csharp
// ISaleManager — genişletmeler
public interface ISaleManager
{
    Task<IResult> MakeSale(MakeSaleDto dto);   // mevcut, güncellenir (SalePayment dahil)
    Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto);  // mevcut, güncellenir
    Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId);   // yeni
    Task<IResult> CancelSaleAsync(Guid saleId, Guid cancelledByUserId); // yeni
    Task<IDataResult<SaleSummaryDto>> GetSalesSummaryAsync(SalePageableDto dto); // yeni — özet kartlar
}

// IPOSSessionManager — X/Z raporu eklenir
public interface IPOSSessionManager
{
    // ... mevcut metodlar korunur ...
    Task<IDataResult<POSReportDto>> GetXReportAsync(long sessionId);    // yeni
    Task<IDataResult<POSReportDto>> GetZReportAsync(long sessionId);    // yeni
}
```

### 8.3 DTO'lar

```csharp
// MakeSaleDto güncellenir
public record MakeSaleDto
{
    public Guid SalePersonId { get; init; }
    public int BranchOfficeId { get; init; }
    public int? CustomerId { get; init; }
    public SaleSource SaleSource { get; init; }
    public decimal GeneralDiscount { get; init; }
    public string? Note { get; init; }
    public List<SaleItemDto> SaleItems { get; init; } = [];
    public List<SalePaymentDto> Payments { get; init; } = [];  // YENİ
}

public record SalePaymentDto
{
    public int PaymentMethodId { get; init; }
    public decimal Amount { get; init; }
    public decimal? CashReceived { get; init; }
    public string? CardAuthCode { get; init; }
}

public record SaleListDetailDto
{
    public Guid Id { get; init; }
    public string SaleNumber { get; init; }
    public DateTimeOffset SaleDate { get; init; }
    public SaleSource SaleSource { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public string? CustomerName { get; init; }
    public string SalePersonName { get; init; }
    public int TotalItemCount { get; init; }
    public decimal GrandTotal { get; init; }       // KDV dahil
    public List<string> PaymentMethods { get; init; } = [];
}

public record SaleDetailDto
{
    public Guid Id { get; init; }
    public string SaleNumber { get; init; }
    public DateTimeOffset SaleDate { get; init; }
    public SaleSource SaleSource { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public string? CustomerName { get; init; }
    public string SalePersonName { get; init; }
    public string BranchOfficeName { get; init; }
    public string? Note { get; init; }
    public decimal SubTotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal GeneralDiscount { get; init; }
    public List<SaleDetailItemDto> Items { get; init; } = [];
    public List<SaleDetailPaymentDto> Payments { get; init; } = [];
    public List<VatSummaryLineDto> VatSummary { get; init; } = [];
    public List<SaleReturnSummaryDto> Returns { get; init; } = [];
}

public record SaleDetailItemDto
{
    public Guid Id { get; init; }
    public string ProductTitle { get; init; }
    public string Barcode { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPriceWithVat { get; init; }
    public decimal VatRate { get; init; }
    public decimal LineTotalWithVat { get; init; }
    public int ReturnedQuantity { get; init; }
}

public record VatSummaryLineDto
{
    public decimal VatRate { get; init; }
    public decimal TaxBase { get; init; }       // Matrah
    public decimal VatAmount { get; init; }     // KDV tutarı
    public decimal Total { get; init; }         // KDV dahil
}

public record SaleSummaryDto
{
    public decimal TotalSales { get; init; }
    public int SaleCount { get; init; }
    public decimal AverageBasket { get; init; }
    public decimal TotalReturns { get; init; }
}

public record CreateSaleReturnDto
{
    public Guid SaleId { get; init; }
    public Guid ReturnedByUserId { get; init; }
    public string ReturnReason { get; init; }
    public int? RefundPaymentMethodId { get; init; }
    public string? Note { get; init; }
    public List<SaleReturnItemDto> Items { get; init; } = [];
}

public record SaleReturnItemDto
{
    public Guid SaleItemId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
}

public record POSReportDto
{
    public long SessionId { get; init; }
    public string CashierName { get; init; }
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public decimal OpeningCash { get; init; }
    public int TransactionCount { get; init; }
    public decimal TotalSales { get; init; }
    public decimal TotalReturns { get; init; }
    public decimal NetSales { get; init; }
    public List<PaymentMethodSummaryDto> PaymentBreakdown { get; init; } = [];
    public List<VatSummaryLineDto> VatBreakdown { get; init; } = [];
    public decimal ExpectedCash { get; init; }
    public decimal? ActualCash { get; init; }      // Z raporunda dolu
    public decimal? CashDifference { get; init; }  // Z raporunda dolu
}

public record PaymentMethodSummaryDto
{
    public string MethodName { get; init; }
    public int Count { get; init; }
    public decimal Total { get; init; }
}
```

---

## 9. MVC Feature Yapısı

### 9.1 Mevcut Feature'lar (Güncellenen)

```
Features/Sales/
├── SaleController.cs          (güncellenir — detay, iptal, iade endpoint'leri)
├── ViewModels/
│   └── SaleEntryVm.cs         (güncellenir — KDV dahil alanlar)
└── Views/
    ├── Create.cshtml           (güncellenir — KDV dahil gösterim)
    ├── Index.cshtml            (yeniden yazılır — özet kartlar, filtreler)
    ├── Detail.cshtml           (YENİ — satış detay)
    ├── Print.cshtml            (YENİ — fiş yazdırma)
    └── Partials/
        ├── _SaleCart.cshtml          (güncellenir — KDV dahil)
        ├── _SaleTable.cshtml         (yeniden yazılır — yeni sütunlar)
        ├── _SaleSearchResults.cshtml (mevcut korunur)
        ├── _SaleCustomerBadge.cshtml (mevcut korunur)
        ├── _SaleCustomerResults.cshtml (mevcut korunur)
        ├── _SaleSummaryCards.cshtml  (YENİ — özet kartlar)
        └── _SaleReturnDialog.cshtml  (YENİ — iade modal)

Features/POS/
├── POSController.cs           (güncellenir — rapor, dinamik ödeme)
├── ViewModels/
│   └── POSTerminalVm.cs       (güncellenir)
└── Views/
    ├── Index.cshtml            (güncellenir — KDV dahil, müşteri seçimi)
    ├── XReport.cshtml          (YENİ)
    ├── ZReport.cshtml          (YENİ)
    └── Partials/
        ├── _POSCart.cshtml              (güncellenir — KDV dahil)
        ├── _POSSearchResults.cshtml     (mevcut korunur)
        ├── _POSPaymentDialog.cshtml     (yeniden yazılır — dinamik ödeme yöntemleri)
        └── _POSCloseSessionDialog.cshtml (mevcut korunur)
```

### 9.2 Yeni Feature

```
Features/Settings/
└── Views/
    └── PaymentMethods.cshtml  (YENİ — veya mevcut Settings altına tab)
```

---

## 10. Migration Planı

### 10.1 Yeni Tablolar
- `PaymentMethodDefinitions` + seed data (5 varsayılan yöntem)
- `SalePayments`
- `SaleReturns`
- `SaleReturnItems`

### 10.2 Mevcut Tablo Değişiklikleri
- `Sales` → `SaleNumber`, `SaleDate`, `SaleSource`, `SaleStatus`, `Note` sütunları eklenir
- `SaleItems` → `Barcode`, `ProductTitle`, `ReturnedQuantity` sütunları eklenir
- `POSTransactions` → `PaymentMethod` sütunu kaldırılır, `CardAuthCode` kaldırılır

### 10.3 Veri Migrasyonu
- Mevcut `POSTransaction.PaymentMethod` enum → `SalePayment` kaydına dönüştürülür
- Mevcut `Sale` kayıtları → `SaleStatus = Completed`, `SaleSource = POS`, `SaleDate = CreatedAt`
- `SaleNumber` mevcut kayıtlar için `"LEGACY-{Id}"` formatında auto-fill

---

## 11. Kapsam Dışı (Gelecek İterasyonlar)

- Fatura entegrasyonu (e-arşiv/e-fatura otomatik kesim)
- Hızlı satış butonları (favori ürünler)
- Kampanya motoru
- Sadakat programı / puan sistemi
- Marketplace siparişlerinin satış listesinde gösterimi
- Tartılı ürün desteği
- Taksitli ödeme detayı

---

## 12. Test Stratejisi

### Unit Test
- SaleManager.MakeSale — split payment validasyonu, stok düşümü
- SaleReturnManager — kısmi iade, onay akışı, stok geri artışı
- KDV hesaplama — oran bazlı doğruluk
- SaleNumber üretimi — uniqueness

### Integration Test
- Satış oluşturma → SalePayment kayıtları → stok düşümü end-to-end
- İade akışı → stok geri artışı → SaleStatus güncelleme
- PaymentMethodDefinition CRUD
- POS oturum akışı → satış → X/Z raporu

### E2E Test
- POS tam akış: kasa aç → ürün ekle → ödeme → kasa kapat
- Satış listesi filtreleme
- İade akışı UI
