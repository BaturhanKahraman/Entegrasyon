# Trendyol Ürün Gönderme Sayfası — Tasarım Dokümanı

**Tarih:** 2026-03-31  
**Kapsam:** ProductSyncFullDetailPage'den Trendyol'a ürün gönderme akışı  
**Yaklaşım:** Kompozit sayfa + alt component'lar (Yaklaşım B)

---

## 1. Genel Bakış

Ürün sync detay sayfasında (`/products/{id}/sync`) pazaryeri durum kartlarına tıklandığında, ürün henüz gönderilmemişse (NeverSynced) ve o pazaryerinin API credential'ları tanımlıysa, kullanıcıyı marketplace-specific gönderme sayfasına yönlendiren akış. İlk faz sadece **Trendyol** için implement edilecek. Diğer marketplace'ler sonraki fazlarda aynı pattern ile eklenecek.

## 2. Akış

```
ProductSyncFullDetailPage (/products/{id}/sync)
    ↓
NeverSynced + credential'lı karta tıklar
    ↓
Navigasyon → /products/{id}/sync/trendyol/send
    ↓
TrendyolProductSendPage açılır
    ↓
Sayfa yüklenirken preflight kontroller çalışır:
  ✅ Kategori eşleşmiş → yeşil check
  ❌ Marka eşleşmemiş → kırmızı alert + tıklanabilir link → /marketplace/brands
  ❌ Zorunlu özellik eksik → kırmızı alert + tıklanabilir link → /marketplace/attributes
    ↓
Tüm kontroller geçerse:
  - Override formları aktif
  - Fiyat/komisyon paneli aktif  
  - Payload ön izleme aktif
  - "Trendyol'a Gönder" butonu aktif
    ↓
Gönder → SyncManager.SyncProductAsync() → Event → Background service
    ↓
Başarılı → Navigasyon /products/{id}/sync (Waiting/Processing durumunda)
```

## 3. Credential Kontrolü (MarketplaceStatusCards)

`MarketplaceSyncItemDto`'ya `bool HasCredentials` property eklenir. `ProductSyncManager.GetProductSyncDetailAsync` içinde MarketPlace tablosundan credential bilgisi kontrol edilir.

Credential'sız kartlar:
- Soluk/disabled görünüm (`Disabled="true"`)
- Tooltip: "Bu pazaryeri için API bilgileri tanımlı değil"
- Tıklanamaz

Credential kontrolü marketplace türüne göre:
- Trendyol: `ApiKey != null && ApiSecret != null && SellerId != null`
- OAuth2 tabanlı (Pazarama, Amazon): `ApiKey != null && ApiSecret != null && TokenUrl != null`
- Diğerleri: `ApiKey != null && ApiSecret != null`

## 4. Dosya Yapısı

### Yeni Dosyalar

```
Features/MarketplaceSync/ProductSend/
├── TrendyolProductSendPage.razor           ← /products/{id}/sync/trendyol/send
├── TrendyolProductSendPage.razor.cs        ← Orchestrator code-behind
├── SendPreflightChecks.razor               ← Eşleştirme kontrol paneli
├── SendPreflightChecks.razor.cs
├── SendOverrideForm.razor                  ← Başlık/açıklama override
├── SendOverrideForm.razor.cs
├── SendPricingPanel.razor                  ← Fiyat override + komisyon hesaplayıcı
├── SendPricingPanel.razor.cs
├── SendPayloadPreview.razor                ← Gönderilecek payload özeti
├── SendPayloadPreview.razor.cs
```

### Mevcut Dosyalarda Değişiklikler

| Dosya | Değişiklik |
|-------|-----------|
| `MarketplaceSyncItemDto` | `bool HasCredentials` property eklenir |
| `ProductSyncManager.GetProductSyncDetailAsync` | MarketPlace tablosundan credential bilgisi kontrol edilir |
| `MarketplaceStatusCards.razor` | Credential'sız kartlar disabled + tooltip |
| `MarketplaceStatusCards.razor.cs` | NeverSynced + credential'lı kart tıklamasında navigasyon event |
| `ProductSyncFullDetailPage.razor.cs` | `SetActiveTab` → NeverSynced ise send sayfasına navigasyon |
| `IProductSyncManager` | `GetSendPreflightAsync()` metodu eklenir |
| `ProductSyncManager` | `GetSendPreflightAsync()` implementasyonu |
| `ITrendyolProductService` | `GetSendPreviewAsync()` metodu eklenir |
| `TrendyolProductService` | `GetSendPreviewAsync()` implementasyonu |

## 5. Component Sorumlulukları

### TrendyolProductSendPage (orchestrator)

- Route: `/products/{id}/sync/trendyol/send`
- RenderMode: InteractiveServer
- Authorize: `AppPermissions.Marketplace.Edit`
- Ürün bilgilerini yükler (`IProductSyncManager.GetProductSyncDetailAsync`)
- Preflight sonuçlarını toplar (`GetSendPreflightAsync`)
- Alt component'lara veri dağıtır
- "Gönder" butonunu kontrol eder (tüm preflight geçerse aktif)
- Override kaydı → `IMarketplaceOverrideManager.SaveOverridesAsync()`
- Gönderme → `SyncManager.SyncProductAsync(productId, marketPlaceId: 1)`

### SendPreflightChecks

**Parameters:**
- `ProductSendPreflightDto Preflight`
- `EventCallback<bool> OnAllPassed`

**Kontrol tablosu:**

| Kontrol | Nasıl Kontrol Edilir | Eksikse Link |
|---------|---------------------|--------------|
| Trendyol kategorisi eşleşmiş mi? | `CategoryMarketPlaceMatch` tablosu (MarketPlaceId=1) | `/marketplace/categories` |
| Trendyol markası eşleşmiş mi? | `BrandMarketPlaceMatch` tablosu | `/marketplace/brands` |
| Zorunlu özellikler eşleşmiş mi? | `CategoryAttributeMarketPlaceMatch` (IsRequired=true) | `/marketplace/attributes` |
| Ürünün en az 1 varyantı var mı? | `Product.Variants.Count > 0` | `/products/{id}` |
| Varyantların barkodu var mı? | Tüm varyantlarda `Barcode != null` | `/products/{id}` |

**UI:**
- Her satır: ✅/❌ ikon + açıklama + tıklanabilir "Düzelt →" linki (eksikse)
- Örnekler:
  - ✅ **Kategori Eşleştirmesi** — Giyim > T-Shirt → Trendyol: Giyim > Tişört
  - ❌ **Marka Eşleştirmesi** — Eşleşme bulunamadı → [Düzelt →](/marketplace/brands)
  - ❌ **Zorunlu Özellikler** — Renk, Beden eksik → [Düzelt →](/marketplace/attributes)

### SendOverrideForm

**Parameters:**
- `string? CurrentTitle`
- `string? CurrentDescription`
- `EventCallback<(string? Title, string? Description)> OnOverrideChanged`

**UI:**
- Başlık override: `MudTextField` (boş bırakılırsa ürünün kendi başlığı)
- Açıklama override: `MudTextField` (multiline, boş bırakılırsa ürünün kendi açıklaması)
- Bilgilendirme: "Boş bırakırsanız ürünün mevcut değerleri kullanılır"

### SendPricingPanel

**Parameters:**
- `Guid ProductId`
- `int MarketPlaceId` (1 = Trendyol)
- `int? CategoryId`
- `List<VariantPricingRowDto> Variants`
- `EventCallback<List<VariantPriceOverrideDto>> OnPriceOverridesChanged`

**UI:**
```
┌──────────┬────────────┬────────────┬──────────────┬──────────┬──────────┐
│ Barkod   │ Liste Fiy. │ Satış Fiy. │ Override Fiy │ Komisyon │ Net Kar  │
├──────────┼────────────┼────────────┼──────────────┼──────────┼──────────┤
│ ABC-S    │ 299,00 ₺   │ 249,00 ₺   │ [    249,00] │  24,90 ₺ │ 180,10 ₺│
│ ABC-M    │ 299,00 ₺   │ 249,00 ₺   │ [    249,00] │  24,90 ₺ │ 180,10 ₺│
│ ABC-L    │ 299,00 ₺   │ 249,00 ₺   │ [         ] │  24,90 ₺ │ 180,10 ₺│
└──────────┴────────────┴────────────┴──────────────┴──────────┴──────────┘
```

**Çalışma şekli:**
- Ürünün varyantları ve mevcut fiyatları read-only
- "Override Fiyat" sütunu düzenlenebilir — boş bırakılırsa orijinal fiyat kullanılır
- Fiyat girildiğinde debounce (300ms) ile `ICommissionCalculator.CalculateAsync()` çağrılır
- Komisyon tutarı, net gelir ve net kar anlık hesaplanıp satırda gösterilir
- Mevcut `ICommissionCalculator` servisi doğrudan kullanılır

### SendPayloadPreview

**Parameters:**
- `TrendyolSendPreviewDto? Preview`
- `bool IsLoading`

**UI:**
```
┌─────────────────┬───────────────────────────────┐
│ Başlık          │ Nike Erkek T-Shirt (override) │
│ Marka           │ Nike → Trendyol: NIKE         │
│ Kategori        │ Giyim > T-Shirt → 1234        │
│ Açıklama        │ (orijinal, 250 karakter...)    │
├─────────────────┴───────────────────────────────┤
│ Özellikler                                      │
│  Renk: Siyah  │  Beden: S, M, L  │  Cinsiyet: E│
├─────────────────────────────────────────────────┤
│ Varyantlar (3 adet)                             │
│  ABC-S │ Siyah/S │ 249,00 ₺ │ Stok: 15        │
│  ABC-M │ Siyah/M │ 249,00 ₺ │ Stok: 22        │
│  ABC-L │ Siyah/L │ 299,00 ₺ │ Stok: 8         │
└─────────────────────────────────────────────────┘
```

Tablo formatında, okunabilir. JSON gösterilmez.

## 6. Business Layer Yeni Metodlar

### IProductSyncManager

```csharp
Task<IDataResult<ProductSendPreflightDto>> GetSendPreflightAsync(Guid productId, int marketPlaceId);
```

### ProductSendPreflightDto

```csharp
public record ProductSendPreflightDto(
    bool CategoryMatched,
    string? MatchedCategoryName,       // eşleşen Trendyol kategori adı
    bool BrandMatched,
    string? MatchedBrandName,          // eşleşen Trendyol marka adı
    bool RequiredAttributesMatched,
    List<string> MissingAttributes,    // eksik zorunlu özellik isimleri
    bool HasVariants,
    bool AllVariantsHaveBarcodes,
    bool AllPassed);
```

### ITrendyolProductService

```csharp
Task<IDataResult<TrendyolSendPreviewDto>> GetSendPreviewAsync(
    Guid productId,
    MarketplaceOverrideDetailDto? overrides);
```

### TrendyolSendPreviewDto

```csharp
public record TrendyolSendPreviewDto(
    string Title,
    string BrandName,
    string TrendyolBrandName,
    string CategoryName,
    string TrendyolCategoryName,
    int TrendyolCategoryId,
    string? Description,
    List<TrendyolPreviewAttributeDto> Attributes,
    List<TrendyolPreviewVariantDto> Variants);

public record TrendyolPreviewAttributeDto(
    string Name,
    string Value);

public record TrendyolPreviewVariantDto(
    string Barcode,
    string Attributes,         // "Siyah / S" gibi birleştirilmiş
    decimal ListPrice,
    decimal SalePrice,
    int Quantity);
```

## 7. Gönderme Akışı

1. Kullanıcı "Trendyol'a Gönder" butonuna basar
2. Confirmation dialog: "Bu ürünü Trendyol'a göndermek istediğinize emin misiniz?"
3. Onay verilirse:
   a. Override varsa → `OverrideManager.SaveOverridesAsync()` çağrılır
   b. `SyncManager.SyncProductAsync(productId, marketPlaceId: 1)` çağrılır
   c. Event publish → `ProductCreatedForMarketplaceEvent`
   d. Background service tarafından async olarak Trendyol API'sine gönderilir
4. Başarılı → Snackbar success + navigasyon `/products/{id}/sync`
5. Başarısız → Snackbar error, sayfada kalır

## 8. Kapsam Dışı (Sonraki Fazlar)

- Hepsiburada, N11, Pazarama, Amazon, PttAVM, Çiçeksepeti gönderme sayfaları
- Toplu ürün gönderme (BulkProductSendPage)
- Gönderme şablonları (override template'leri)
- Ürün güncelleme akışı (OutOfSync → güncelle)
