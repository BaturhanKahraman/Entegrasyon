# Kategori Matching Optimizasyonu - Implementasyon Plani

## Mevcut Durum Analizi

### Mevcut Akis (Pain Points)

1. **Tek tek eslestirme:** `CategorySync.razor` sayfasinda kullanici her kategori icin ayri ayri "Eslestir" butonuna basiyor, `CategoryMappingDialog` aciliyor, marketplace seciyor, kategori ariyor, kaydediyor.
2. **Attribute eslestirmesi kopuk:** Kategori eslestirmesinden sonra kullanici ayri bir sayfaya (`/marketplace/sync/attributes`) gidip attribute eslestirmesini yapmak zorunda. Otomatik yonlendirme yok.
3. **Her marketplace icin tekrar:** Ayni kategori icin farkli marketplace'lerde tekrar tekrar ayni islemi yapmak gerekiyor.
4. **Sifirdan baslama:** `MatchedEntityImportManager` + `MatchedEntityPackage` altyapisi var ama sadece admin panelinden, kullanici tarafinda template olusturma/kullanma yok.
5. **Dogrulama eksik:** Eslestirme sonrasi marketplace'in zorunlu attribute'larinin eslestirilip eslestirilmedigini kontrol eden bir uyari yok (`TrendyolMappingValidator` sadece urun publish asamasinda calisiyor).
6. **Hardcoded marketplace:** `CategoryMatchService.GetCategoryMatchSummaryAsync()` hardcoded `TrendyolMarketPlaceId` kullaniyor.

### Mevcut Dosya Yapisi

| Katman | Dosya | Aciklama |
|--------|-------|----------|
| Entity | `Entity/Matches/CategoryMarketPlaceMatch.cs` | Eski match entity (int composite key) |
| Entity | `Entity/Categories/CategoryMarketplace.cs` | Yeni match entity (BaseEntity, IsActive, ExternalId) |
| Entity | `Entity/Matches/CategoryAttributeMarketPlaceMatch.cs` | Attribute match (+ string ExternalId) |
| Entity | `Entity/Matches/CategoryAttributeValueMarketPlaceMatch.cs` | Attribute value match |
| Entity | `Entity/Dtos/Category/CreateCategoryMarketplaceMatchDto.cs` | Tek kategori eslestirme DTO |
| Entity | `Entity/Dtos/Category/CategoryMatchSummaryDto.cs` | Ozet istatistik DTO |
| Entity | `Entity/Dtos/Marketplace/MarketplaceSearchResult.cs` | Arama sonuc record'lari |
| Business | `Business/Abstract/ICategoryMatchService.cs` | 4 metod (summary, list, create, remove) |
| Business | `Business/Concrete/CategoryMatchService.cs` | Implementasyon (tek tek CRUD) |
| Business | `Business/Abstract/IMarketplaceSearchService.cs` | Kategori/brand/attribute arama |
| Business | `Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs` | Trendyol API ile kategori arama |
| Business | `Business/Concrete/Trendyol/TrendyolMappingValidator.cs` | Urun bazli dogrulama |
| Business | `Business/Abstract/IMatchedEntityImportManager.cs` | Template import altyapisi |
| Business | `Business/Concrete/MatchedEntityImportManager.cs` | Template import implementasyonu |
| Blazor | `Features/MarketplaceSync/CategorySync.razor(.cs)` | Kategori listesi + tek tek eslestirme |
| Blazor | `Features/MarketplaceSync/CategoryMappingDialog.razor(.cs)` | Tek kategori eslestirme dialog |
| Blazor | `Features/MarketplaceSync/AttributeSync.razor(.cs)` | Attribute listesi + tek tek eslestirme |
| Blazor | `Features/MarketplaceSync/AttributeMappingDialog.razor(.cs)` | Tek attribute eslestirme dialog |
| Test | `IntegrationTest/Business/MatchedEntityImportIntegrationTests.cs` | Template import testleri |

**Not:** CategoryMatchService/CategorySync icin spesifik unit test dosyasi mevcut degil.

---

## Faz 1: Bulk Category Matching

**Oncelik:** Yuksek | **Bagimsizlik:** Bagimsiz | **Tahmini Efor:** 3-4 gun

### 1.1 Yeni DTO'lar

**Dosya:** `Application/Entegrasyon.Entity/Dtos/Category/BulkCategoryMatchDto.cs` (yeni)

```
BulkCategoryMatchDto
  - int MarketPlaceId
  - List<BulkCategoryMatchItemDto> Items

BulkCategoryMatchItemDto
  - int ApplicationCategoryId
  - int MarketPlaceCategoryId
  - string? ExternalCategoryId
  - string? MarketPlaceCategoryName

BulkCategoryMatchResultDto
  - int TotalRequested
  - int SuccessCount
  - int FailedCount
  - int SkippedCount (zaten eslesmis)
  - List<BulkCategoryMatchErrorDto> Errors

BulkCategoryMatchErrorDto
  - int ApplicationCategoryId
  - string CategoryName
  - string ErrorMessage
```

### 1.2 Business Layer

**Dosya:** `Application/Entegrasyon.Business/Abstract/ICategoryMatchService.cs` (guncelle)

Yeni metod: `Task<IDataResult<BulkCategoryMatchResultDto>> BulkCreateCategoryMappingsAsync(BulkCategoryMatchDto dto);`

**Dosya:** `Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs` (guncelle)

Pipeline:
1. **Validation:** MarketPlaceId > 0, Items bos degil, max 500 item limiti (FluentValidation)
2. **Business Rules:** Her item icin kategori var mi, zaten eslesmis mi kontrol. Eslesmis olanlari SkippedCount'a ekle.
3. **Execution:** `AddRange` ile toplu kayit, tek `SaveChangesAsync`, transaction icerisinde.

**Dosya:** `Application/Entegrasyon.Business/Validation/FluentValidation/BulkCategoryMatchDtoValidator.cs` (yeni)

### 1.3 UI - Bulk Eslestirme Sayfasi

**Klasor:** `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/` (yeni)

Component'lar:
- `BulkCategoryMatchPage.razor(.cs)` -- Ana sayfa (`/marketplace/sync/categories/bulk`)
- `BulkMatchCategorySelector.razor(.cs)` -- Sol panel: checkbox'li kategori listesi (MudDataGrid + multi-select)
- `BulkMatchMarketplaceMapper.razor(.cs)` -- Sag panel: secilen kategorilerin marketplace eslestirme tablosu
- `BulkMatchProgressDialog.razor(.cs)` -- Toplu kayit ilerleme dialog

Akis:
1. Kullanici marketplace secer (MudSelect)
2. Sol panelde eslestirilmemis kategoriler listelenir (filtrelenebilir)
3. Kullanici checkbox ile kategorileri secer (toplu sec/kaldir)
4. Sag panelde secilen her kategori icin marketplace kategori autocomplete gosterilir
5. "Toplu Eslestir" butonu ile tek seferde kaydedilir
6. Sonuc dialog'u gosterilir (basarili/basarisiz/atlanan)

`CategorySync.razor`'a "Toplu Eslestirme" butonu eklenir.

### 1.4 Test Stratejisi

**Unit Test:** `Test/Entegrasyon.Test/CategoryMatch/BulkCategoryMatchServiceTests.cs` (yeni)
- Bos liste ile cagirildiginda hata donmeli
- Gecerli liste ile basarili kayit
- Zaten eslesmis kategorileri skip etmeli
- Gecersiz kategori ID'si icin hata donmeli
- Max limit asiminda hata
- FluentValidation testleri

**Integration Test:** `Test/Entegrasyon.IntegrationTest/Business/BulkCategoryMatchIntegrationTests.cs` (yeni)
- DB'ye gercek kayit testi
- Transaction rollback testi
- Concurrent eslestirme cakismasi testi

---

## Faz 2: Auto-Matching (LLM ile Otomatik Oneri)

**Oncelik:** Yuksek | **Bagimsizlik:** Faz 1'e bagimli (bulk API gerekli) | **Tahmini Efor:** 4-5 gun

### 2.1 LLM Entegrasyonu Stratejisi

**Model:** `entegrasyon-coder` (Ollama, qwen3.5:9b, localhost:11434, RTX 4080 12GB)

**Prompt Stratejisi:**
- Dusuk temperature (0.1) -- deterministik sonuclar
- JSON format zorunlulugu -- parse hatasi azaltir
- Few-shot ornekler -- prompt'a 2-3 ornek eslestirme ekle
- Hiyerarsi bilgisi -- sadece isim degil, parent path de gonder (ornegin "Elektronik > Telefon > Akilli Telefon")
- Batch boyutu: max 50 kategori/istek (context window dengesi)

### 2.2 Yeni Servis

**Dosya:** `Application/Entegrasyon.Business/Abstract/ICategoryAutoMatchService.cs` (yeni)
```csharp
public interface ICategoryAutoMatchService
{
    Task<IDataResult<List<CategoryAutoMatchSuggestionDto>>> SuggestMatchesAsync(
        int marketPlaceId, List<int>? categoryIds = null, CancellationToken ct = default);
}
```

**Dosya:** `Application/Entegrasyon.Business/Concrete/CategoryAutoMatchService.cs` (yeni)

Pipeline:
1. Validation: MarketPlaceId gecerli mi
2. Business Rules: Eslestirilmemis kategorileri filtrele, marketplace kategorilerini cek
3. Execution: Ollama API'ye batch istek gonder, sonucu parse et, confidence score'a gore sirala

**Dosya:** `Application/Entegrasyon.Business/Concrete/OllamaAutoMatchClient.cs` (yeni)
- HttpClient ile `http://localhost:11434/api/generate`
- Timeout: 120s, Retry: 1x
- Scoped servis (multi-tenant uyumlu)

**DTO:** `Application/Entegrasyon.Entity/Dtos/Category/CategoryAutoMatchSuggestionDto.cs` (yeni)
```
- ApplicationCategoryId, ApplicationCategoryName
- SuggestedMarketPlaceCategoryId, SuggestedMarketPlaceCategoryName, SuggestedMarketPlaceCategoryFullPath
- Confidence (0.0-1.0), Reason
```

### 2.3 UI

**Dosya:** `Features/MarketplaceSync/BulkCategoryMatch/AutoMatchSuggestionPanel.razor(.cs)` (yeni)

BulkCategoryMatchPage icerisinde entegre:
- "Otomatik Oneri Al" butonu
- Progress bar + "Model dusunuyor..." mesaji
- Oneriler tablosu (kategori, oneri, guven skoru, sebep)
- Her oneri kabul/ret (checkbox)
- Kabul edilenleri bulk API'ye gonder

Guven skoru gorsellestirmesi:
- >= 0.8: Yesil chip, otomatik secili
- 0.5-0.8: Sari chip, kullanici secmeli
- < 0.5: Kirmizi chip, secili degil

### 2.4 Fallback Stratejisi

- Ollama cagrisi basarisiz: Hata mesaji, manuel akis calismaya devam eder
- JSON parse hatasi: Retry 1x, hala basarisizsa hata
- Timeout: "Model yanitlamiyor, lutfen tekrar deneyin"
- Dusuk guven skorlu sonuclar: Gosterilir ama otomatik secilmez

### 2.5 Test Stratejisi

**Unit Test:** `Test/Entegrasyon.Test/CategoryMatch/CategoryAutoMatchServiceTests.cs` (yeni)
- Mock Ollama response ile basarili eslestirme
- Ollama hatasi durumunda graceful degradation
- Bos kategori listesi, confidence filtering, JSON parse hatasi

**Not:** Ollama'ya gercek istek atan integration test yazilmaz (CI/CD'de Ollama yok). Mock ile test edilir.

---

## Faz 3: Attribute Redirect

**Oncelik:** Orta | **Bagimsizlik:** Bagimsiz | **Tahmini Efor:** 1-2 gun

### 3.1 Degisiklikler

**`CategoryMappingDialog.razor.cs` (guncelle):** Eslestirme basarili oldugunda "Attribute eslestirmeye git" vs "Burada kal" secenegi (MudDialog ile sor). Kabul edilirse `NavigationManager` ile `/marketplace/sync/attributes?categoryId={id}&marketPlaceId={mpId}` yonlendirmesi.

**`BulkMatchProgressDialog.razor.cs` (guncelle):** Toplu eslestirme bittiginde sonuc ozeti + "Simdi attribute eslestirmeye git" butonu.

**`AttributeSync.razor.cs` (guncelle):** `[SupplyParameterFromQuery]` ile `categoryId`, `marketPlaceId`, `unmappedOnly` parametreleri. Sayfa acildiginda filtre otomatik uygulanir.

**`CategorySync.razor` (guncelle):** Kategori eslestirmesi olan ama attribute eslestirmesi eksik olan kategoriler icin uyari ikonu (MudIcon + Tooltip).

### 3.2 Test Stratejisi

**bUnit Test:** `Test/Entegrasyon.BunitTest/Features/MarketplaceSync/CategoryMappingDialogTests.cs` (yeni)
- Dialog kapanisinda NavigationManager.NavigateTo cagrildi mi
- Query parameter'larin dogru gonderildigi

---

## Faz 4: Template Kaydetme ve Tekrar Kullanma

**Oncelik:** Orta | **Bagimsizlik:** Faz 1'e bagimli | **Tahmini Efor:** 3-4 gun

### 4.1 Mevcut Altyapi

`MatchedEntityImportManager` + `MatchedEntityPackage` + `TemplateCategoryData` altyapisi zaten mevcut. Eksik olan:
- Kullanici tarafinda template olusturma
- Mevcut eslestirmeleri template olarak export etme

### 4.2 Business Layer

**`ICategoryMatchService.cs` (guncelle):** Yeni metodlar:
- `SaveCurrentMappingsAsTemplateAsync(CreateMatchingTemplateDto dto)`
- `GetSavedTemplatesAsync()`
- `ApplyTemplateAsync(int templateId, int targetMarketPlaceId)`
- `DeleteTemplateAsync(int templateId)`

**`CategoryMatchService.cs` (guncelle):**
- SaveCurrentMappings: Mevcut CategoryMarketplace + attribute match verilerini MatchedEntityPackage formatina donustur
- ApplyTemplate: Onceden cakisma tespiti, kullaniciya gosterme, skip/overwrite secenegi

### 4.3 UI

**Klasor:** `Features/MarketplaceSync/Templates/` (yeni)
- `MatchingTemplateList.razor(.cs)` -- Kayitli template listesi
- `SaveTemplateDialog.razor(.cs)` -- Yeni template kaydetme
- `ApplyTemplateDialog.razor(.cs)` -- Template uygulama (cakisma gosteren)

`CategorySync.razor`'a "Template Kaydet" ve "Template Uygula" butonlari eklenir.

### 4.4 Test Stratejisi

**Unit + Integration Test:** Template olusturma, listeleme, uygulama roundtrip testi, cakisma tespiti

---

## Faz 5: Eslestirme Dogrulama (Validation)

**Oncelik:** Yuksek | **Bagimsizlik:** Bagimsiz | **Tahmini Efor:** 2-3 gun

### 5.1 Yeni Servis

**`ICategoryMatchValidationService.cs` (yeni):**
```csharp
Task<IDataResult<CategoryMatchValidationResultDto>> ValidateCategoryMatchAsync(int categoryId, int marketPlaceId);
Task<IDataResult<List<CategoryMatchValidationResultDto>>> ValidateAllMatchesAsync(int marketPlaceId);
```

Kontroller:
1. Kategori eslestirmesi var mi
2. Zorunlu attribute'lar (`IsRequired=true`) eslestirilmis mi
3. Varianter attribute'lar (`IsVarianter=true`) eslestirilmis mi
4. Attribute value eslestirmeleri tamam mi (AllowCustom=false icin)
5. Marka eslestirmesi var mi

**DTO:** `CategoryMatchValidationResultDto` -- CategoryId, CategoryName, IsValid, Warnings, Errors, TotalRequiredAttributes, MappedRequiredAttributes

### 5.2 UI

**`CategorySync.razor` (guncelle):** Her satir icin "Dogrulama Durumu" kolonu (yesil/sari/kirmizi ikon + tooltip)

**`MatchValidationPanel.razor(.cs)` (yeni):** Detayli dogrulama sonuclari + "Simdi eslestir" butonu

### 5.3 Test Stratejisi

Unit + Integration: Tum eslestirmeler tamam = IsValid, zorunlu attribute eksik = Error, opsiyonel eksik = Warning

---

## Faz 6: Multi-Marketplace Yonetim

**Oncelik:** Orta | **Bagimsizlik:** Faz 1-5 sonrasi | **Tahmini Efor:** 2-3 gun

### 6.1 Degisiklikler

**`CategoryMatchService.cs` (guncelle):** `GetCategoryMatchSummaryAsync()` parametrik hale (marketplace bazli veya tumu). Hardcoded TrendyolMarketPlaceId kaldirilir.

**`CategoryMatchSummaryDto` (guncelle):** `List<MarketplaceMatchSummaryItemDto> PerMarketplace` eklenir.

**`CategorySync.razor(.cs)` (guncelle):** Marketplace tab'lari (MudTabs), ozet kartlar marketplace bazli, DataGrid'de tum marketplace durumu.

**`MarketplaceComparisonPanel.razor(.cs)` (yeni):** Yan yana karsilastirma, tutarsizlik vurgulama, "Bu eslestirmeyi diger marketplace'lere uygula" onerisi.

---

## Bagimsizlik Sirasi

```
Faz 5 (Validation)  ─────────────────────────────┐
                                                   │
Faz 3 (Attribute Redirect) ──────────────────────┐│
                                                  ││
Faz 1 (Bulk Matching) ──> Faz 2 (Auto-Match) ────┼┼──> Faz 6 (Multi-MP)
                       └──> Faz 4 (Templates) ────┘│
                                                    │
                                                    └──> Final Test & QA
```

**Onerilen baslangic sirasi:**
1. Faz 1 + Faz 5 + Faz 3 paralel baslayabilir (bagimsiz)
2. Faz 2 + Faz 4, Faz 1 tamamlandiktan sonra (birbirine paralel)
3. Faz 6 tum fazlar tamamlandiktan sonra

---

## Risk Analizi

| Risk | Olasilik | Etki | Azaltma |
|------|----------|------|---------|
| Ollama yanlis eslestirme yapar | Yuksek | Dusuk | Guven skoru + kullanici onay, otomatik uygulama yok |
| Ollama timeout (buyuk liste) | Orta | Orta | Batch (50/istek), 120s timeout, retry 1x |
| Ollama cevrimdisi | Orta | Dusuk | Graceful degradation, manuel akis calismaya devam |
| Bulk eslestirmede race condition | Dusuk | Yuksek | Transaction + unique constraint |
| Template cakisma | Orta | Orta | Onceden tespit + kullaniciya gosterme |
| Performans (cok fazla kategori) | Orta | Orta | Pagination + MudDataGrid virtualization |
| Multi-tenant izolasyonu | Yuksek | Yuksek | Tum query'lerde tenant filter, template'ler tenant-scoped |

---

## Toplam Dosya Ozeti

**Yeni dosyalar:** ~25 (6 DTO, 2 interface, 4 concrete, ~12 Blazor component, ~7 test dosyasi)

**Guncellenecek dosyalar:** ~10 (`ICategoryMatchService`, `CategoryMatchService`, `ApplicationDependencyExtension`, `CategorySync.razor(.cs)`, `CategoryMappingDialog.razor.cs`, `AttributeSync.razor.cs`, `CategoryMatchSummaryDto`, `NavMenu.razor`)

**Toplam tahmini efor:** 15-21 gun (paralel calismayla ~12 gun)
