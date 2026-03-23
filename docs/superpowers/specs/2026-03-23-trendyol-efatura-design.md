# Trendyol e-Fatura Entegrasyonu — Design Spec

**Tarih:** 2026-03-23
**MarketPlaceId:** 1 (Trendyol)
**Platform:** Trendyol e-Faturam (marketplace API'den bagimsiz ayri platform)

---

## 1. Genel Bakis

### e-Fatura Nedir?

Trendyol e-Faturam, GIB'e (Gelir Idaresi Baskanligi) dogrudan baglanmadan e-fatura, e-arsiv ve e-irsaliye islemleri yapmayi saglayan ayri bir platformdur. Marketplace API'sinden tamamen bagimsizdur — farkli base URL, farkli auth mekanizmasi kullanir.

### Neden Lazim?

1. **Yasal zorunluluk:** Turkiye'de e-ticaret satis islemleri icin e-fatura/e-arsiv zorunludur
2. **Trendyol entegrasyonu:** Fatura gonderilmeden kargo etiketi bastirilmaz (mevcut `TrendyolInvoiceService.SendInvoiceLinkAsync`)
3. **Otomasyon:** Manuel fatura kesme yerine otomatik fatura olusturma

### Hangi Senaryolarda Tetiklenir?

| Senaryo | Fatura Turu | Tetikleme |
|---------|-------------|-----------|
| Siparis kargoya verildiginde | e-Arsiv veya e-Fatura | Otomatik (background service) |
| Alici e-fatura mukellefiyse | Giden e-Fatura (TEMELFATURA/TICARIFATURA) | Otomatik — VKN ile mukellef sorgula |
| Alici e-fatura mukellifi degilse | e-Arsiv (EARSIVFATURA) | Otomatik |
| Iade durumunda | Iade faturasi (invoiceTypeCode: IADE) | Otomatik veya manuel |
| Manuel tetikleme | Herhangi biri | UI uzerinden |

**Karar:** Siparis `Shipped` durumuna gectiginde (kargoya verildiginde) otomatik olarak tetiklenir. Background servis ile poll edilir.

---

## 2. Trendyol e-Fatura API Endpoint'leri Ozeti

### Ortamlar

| Ortam | URL |
|-------|-----|
| STAGE | `https://stage-apigateway.trendyolefaturam.com` |
| PROD | `https://apigateway.trendyolecozum.com` |

### Auth Akisi (2 asamali)

```
1. Partner Sign-In → POST /api/auth/signin → partner access token
2. Customer Sign-In → POST /api/invoice/partners/customer/signin
   → Header'da partner token gerekli
   → Response: userId, companyId, partnerCustomerId, accessToken
3. Sonraki tum isteklerde musteri token kullanilir
```

### Kritik Kurallar

- **Tutarlar kurus olarak girilir:** 114.55 TL → `11455` (long)
- **1 Temmuz 2024'ten itibaren** internet satis e-arsiv faturalarinda `paymentInfo` ve `deliveryInfo` ZORUNLU
- **source:** Pazaryeri entegratorleri icin `PARTNER`

### Kullanilacak Endpoint'ler (Faz 1 Scope)

| Endpoint | Metod | Aciklama |
|----------|-------|---------|
| `/api/auth/signin` | POST | Partner token al |
| `/api/invoice/partners/customer/signin` | POST | Musteri token al |
| `/api/invoice/taxpayers/{taxId}` | GET | Mukellef sorgula (e-fatura mi?) |
| `/api/invoice/documents/earchive` | POST | e-Arsiv fatura olustur |
| `/api/invoice/documents/outgoing-einvoice` | POST | Giden e-Fatura olustur |
| `/api/invoice/documents/earchive/status/{uuid}` | GET | e-Arsiv durum sorgula |
| `/api/invoice/documents/outgoing-einvoice/status/{uuid}` | GET | e-Fatura durum sorgula |
| `/api/invoice/documents/earchive/cancel` | POST | e-Arsiv iptal |
| `/api/invoice/documents/download/permanent-url` | POST | PDF indirme URL'i al |
| `/api/invoice/partners/{partnerId}/credits/remaining/customers/{partnerCustomerId}` | GET | Kalan kontor sorgula |

### Fatura Statu Kodlari

| Kod | Durum |
|-----|-------|
| 10 | Isleniyor |
| 20 | Dokuman hazirlaniyor |
| 29 | Dokuman hatasi |
| 30 | Olusturuldu |
| 40 | GIB'e gonderildi |
| 205 | Onaylandi (final) |
| 305 | Iptal edildi |
| 405 | Hatali |

---

## 3. Mimari & Dosya Yapisi

### Yeni API Client (Ayri Platform)

Mevcut `TrendyolApiClient` marketplace API'si icindir (Basic Auth, `apigw.trendyol.com`). e-Fatura platformu tamamen farkli:
- Farkli base URL (`apigateway.trendyolecozum.com`)
- Farkli auth (partner token + customer token)
- Farkli header yapisi

Bu nedenle **yeni bir API client** gereklidir.

### Business Layer

```
Business/Concrete/Trendyol/EFatura/
├── TrendyolEFaturaApiClient.cs          # HTTP client, 2-asamali token auth
├── TrendyolEFaturaService.cs            # Ana fatura islem servisi
├── TrendyolEFaturaInvoiceBuilder.cs     # Order → e-Fatura request body builder
├── TrendyolEFaturaModels.cs             # Request/Response DTO'lari
└── MockTrendyolEFaturaService.cs        # Dev/test mock
```

### Abstract Interfaces

```
Business/Abstract/
├── ITrendyolEFaturaApiClient.cs         # e-Fatura platform HTTP client
└── ITrendyolEFaturaService.cs           # Ana fatura islem interface'i
```

### Background Services

```
Business/BackgroundServices/
└── TrendyolEFaturaStatusPollingService.cs  # Fatura durum takibi + PDF indirme + link gonderme
```

### Entity

```
Entity/Invoices/
├── EFaturaRecord.cs                     # Fatura kaydi entity (OrderId, UUID, status, PDF URL vb.)
└── EFaturaStatus.cs                     # Enum: Processing, Created, Sent, Approved, Cancelled, Error
```

---

## 4. Detayli Tasarim

### 4.1. ITrendyolEFaturaApiClient

Marketplace `ITrendyolApiClient`'tan tamamen bagimsiz. e-Fatura platformuna ozgu 2-asamali token yonetimi yapar.

```csharp
public interface ITrendyolEFaturaApiClient
{
    /// Partner sign-in → partner token al
    Task<IResult> AuthenticatePartnerAsync(CancellationToken ct = default);

    /// Customer sign-in → musteri token al (userId, companyId, accessToken)
    Task<IResult> AuthenticateCustomerAsync(CancellationToken ct = default);

    /// Authenticated GET istegi
    Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default);

    /// Authenticated POST istegi
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
}
```

**Token Cache (Multi-Tenant Ready):**
- `ConcurrentDictionary<int, TokenInfo>` ile tenant/marketplace basina token cache
- Partner token + Customer token ayri ayri saklanir
- Token suresi dolunca otomatik yenileme

**Credential Storage:**
- Partner email/password: MarketPlace tablosunda (yeni alanlar veya ayri tablo)
- Customer email/password/taxId: DB'de ayri konfigürasyon olarak

### 4.2. ITrendyolEFaturaService

Ana fatura islem interface'i:

```csharp
public interface ITrendyolEFaturaService
{
    /// VKN ile mukellef sorgula — e-fatura mukellefiyse true
    Task<IDataResult<bool>> CheckTaxPayerAsync(string taxId, CancellationToken ct = default);

    /// Siparis icin e-fatura/e-arsiv olustur
    Task<IDataResult<EFaturaRecord>> CreateInvoiceForOrderAsync(Guid orderId, CancellationToken ct = default);

    /// Fatura durumunu sorgula
    Task<IDataResult<EFaturaRecord>> CheckInvoiceStatusAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// Fatura iptal et (sadece e-arsiv)
    Task<IResult> CancelInvoiceAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// Fatura PDF indirme URL'i al
    Task<IDataResult<string>> GetInvoicePdfUrlAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// Kalan kontor/kredi sorgula
    Task<IDataResult<int>> GetRemainingCreditsAsync(CancellationToken ct = default);
}
```

### 4.3. EFaturaRecord Entity

```csharp
public sealed class EFaturaRecord : BaseEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string? InvoiceUuid { get; set; }      // Trendyol tarafindaki UUID
    public string? InvoiceId { get; set; }         // ABC2025000000001 formatinda
    public string? EnvelopeId { get; set; }        // GIB zarf UUID (e-fatura icin)

    public EFaturaType InvoiceType { get; set; }   // EArchive, EInvoice
    public EFaturaStatus Status { get; set; }       // Processing, Created, Sent, Approved, Cancelled, Error
    public int? ApiStatusCode { get; set; }         // 10, 20, 30, 40, 205, 305, 405

    public string? PdfDownloadUrl { get; set; }
    public string? LocalReferenceId { get; set; }   // Bizim taraftaki referans

    public long PayableAmountKurus { get; set; }    // Tutar (kurus)
    public long TaxAmountKurus { get; set; }        // KDV (kurus)

    public string? ErrorMessage { get; set; }
    public DateTimeOffset? SentToGibAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    /// Fatura linki Trendyol marketplace'e gonderildi mi?
    public bool InvoiceLinkSentToMarketplace { get; set; }
}

public enum EFaturaType { EArchive, EInvoice }
public enum EFaturaStatus { Pending, Processing, Created, Sent, Approved, Cancelled, Error }
```

### 4.4. Fatura Olusturma Akisi

```
1. Siparis "Shipped" durumuna gecer (TrendyolOrderPollingService tespiti)
2. TrendyolEFaturaStatusPollingService: Shipped + faturasi olmayan siparisleri bul
3. Alicinin VKN/TCKN'si ile mukellef sorgula (CheckTaxPayerAsync)
   → aliasType == INVOICE → e-Fatura mukellefiyse
   → Bulunamadi → e-Arsiv kullan
4a. e-Fatura mukellefiyse → createOutgoingEInvoice
4b. Degilse → createEArchive (paymentInfo + deliveryInfo zorunlu)
5. EFaturaRecord kaydet (Status = Processing)
6. Background service: Durum sorgula → status == 205 (onaylandi)?
7. Onaylaninca: PDF URL al
8. PDF URL'i Trendyol marketplace'e gonder (mevcut TrendyolInvoiceService.SendInvoiceLinkAsync)
9. EFaturaRecord guncelle (InvoiceLinkSentToMarketplace = true)
```

### 4.5. TrendyolEFaturaInvoiceBuilder

Order entity'sinden e-fatura request body'si olusturur:

- `Order` + `OrderItem`'lardan `invoiceLines` uretir
- Tutarlari kurus'a cevirir (decimal * 100 → long)
- KDV hesaplar (VatRate'den)
- `recipientInfo` (fatura adresi + musteri bilgileri)
- `paymentInfo` (marketplace satisi → CREDIT_CARD, purchaseUrl)
- `deliveryInfo` (kargo bilgisi)
- `orderInfo` (siparis numarasi + tarih)

### 4.6. Background Service: TrendyolEFaturaStatusPollingService

```
Her 5 dakikada:
1. "Shipped" durumunda + EFaturaRecord'u olmayan siparisleri bul → fatura olustur
2. Status = Processing/Created/Sent olan kayitlari poll et → status guncelle
3. Status = Approved olan + InvoiceLinkSentToMarketplace = false → PDF URL al + marketplace'e gonder
```

---

## 5. Mevcut Akisa Entegrasyon Noktalari

### 5.1. Mevcut TrendyolInvoiceService (Marketplace API)

Bu servis **marketplace** tarafindaki fatura link/dosya gonderme islemleri icindir. e-Fatura platformuyla ilgisi yoktur. Mevcut haliyle korunur — e-Fatura akisinin son adiminda kullanilir (PDF linkini marketplace'e bildirmek icin).

### 5.2. TrendyolOrderPollingService

Mevcut siparis polling servisi degismez. Siparisleri import etmeye devam eder. e-Fatura servisi, import edilen siparislerin durumunu takip eder.

### 5.3. OrderManager

Degisiklik gerekmez. Siparis import akisi aynen devam eder.

---

## 6. DI Kayitlari

```csharp
// ApplicationDependencyExtension.cs icinde:

// Trendyol e-Fatura servisleri
var useEFaturaMock = configuration.GetValue<bool>("TrendyolEFatura:UseMock", true);
if (useEFaturaMock)
{
    services.AddScoped<ITrendyolEFaturaApiClient, MockTrendyolEFaturaApiClient>();
    services.AddScoped<ITrendyolEFaturaService, MockTrendyolEFaturaService>();
}
else
{
    services.AddScoped<ITrendyolEFaturaApiClient, TrendyolEFaturaApiClient>();
    services.AddScoped<ITrendyolEFaturaService, TrendyolEFaturaService>();
}
services.AddScoped<TrendyolEFaturaInvoiceBuilder>();

// Background service
services.AddHostedService<TrendyolEFaturaStatusPollingService>();
```

---

## 7. DB Migration

```
Yeni tablo: EFaturaRecords
- Id (Guid, PK)
- OrderId (Guid, FK → Orders)
- InvoiceUuid (string, nullable)
- InvoiceId (string, nullable)
- EnvelopeId (string, nullable)
- InvoiceType (int — enum)
- Status (int — enum)
- ApiStatusCode (int, nullable)
- PdfDownloadUrl (string, nullable)
- LocalReferenceId (string, nullable)
- PayableAmountKurus (long)
- TaxAmountKurus (long)
- ErrorMessage (string, nullable)
- SentToGibAt (DateTimeOffset, nullable)
- ApprovedAt (DateTimeOffset, nullable)
- InvoiceLinkSentToMarketplace (bool)
- BaseEntity alanlari (CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
```

---

## 8. Test Stratejisi

### Unit Tests

```
Test/Entegrasyon.Test/Trendyol/
├── TrendyolEFaturaServiceTests.cs       # Ana servis testleri
├── TrendyolEFaturaInvoiceBuilderTests.cs # Builder testleri (tutar cevirme, KDV hesaplama)
└── TrendyolEFaturaApiClientTests.cs      # API client testleri (token yonetimi)
```

**Test senaryolari:**
1. `CreateInvoiceForOrderAsync` — mukellef → e-Fatura path
2. `CreateInvoiceForOrderAsync` — mukellef degil → e-Arsiv path
3. `CheckInvoiceStatusAsync` — status 205 → Approved
4. `CheckInvoiceStatusAsync` — status 405 → Error
5. `CancelInvoiceAsync` — basarili iptal
6. `CheckTaxPayerAsync` — mukellef bulundu
7. `CheckTaxPayerAsync` — mukellef bulunamadi
8. `InvoiceBuilder` — tutar dogru kurus'a cevriliyor
9. `InvoiceBuilder` — KDV dogru hesaplaniyor
10. `InvoiceBuilder` — paymentInfo ve deliveryInfo zorunlu alanlari dolu

### Faz 1 Scope (Bu Implementation)

Ilk implementasyonda sadece core servisler + builder + temel testler:
- `ITrendyolEFaturaApiClient` + mock
- `ITrendyolEFaturaService` + mock
- `TrendyolEFaturaInvoiceBuilder`
- `EFaturaRecord` entity
- Unit testler
- DI kayitlari
- Background service (status polling)

---

## 9. UI Gereksinimleri (Faz 2 — Bu Spec Disinda)

Siparis detay sayfasinda:
- Fatura durumu gosterimi
- Manuel fatura olusturma butonu
- Fatura PDF indirme linki
- Fatura iptal butonu
- Kalan kontor bilgisi

---

## 10. Multi-Tenant Uyumluluk

- `TrendyolEFaturaApiClient`: Token cache `ConcurrentDictionary<int, EFaturaTokenInfo>` (key = tenantId)
- `SemaphoreSlim`: Tenant basina izole lock
- DB sorgulari: Tenant filtresi uygulanabilir yapida
- Credential'lar: DB'den okunur, hardcode degil
