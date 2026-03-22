# Pazarama Marketplace Entegrasyonu — Faz 1 Tasarım Dokümanı

## Amaç

Pazarama pazaryeri desteğini sisteme eklemek. Faz 1, altyapı katmanını (API client, OAuth2 token yönetimi, DB seed), kategori ağacı import'unu, kategori özellik import'unu ve marka listesi çekmeyi kapsar.

## Kapsam

- PazaramaApiClient: OAuth2 client_credentials token bazlı REST client
- MarketPlace seed: Id=4, Name="Pazarama"
- Match tablolarına GUID string alanları (migration — sadece attribute/value/brand match tabloları)
- Kategori ağacı import (flat-to-tree dönüşüm)
- Kategori özellik import (marketplace-aware dedup)
- Marka listesi çekme ve import
- Blazor UI: CategoryImport sayfasına Pazarama tab
- DI kayıtları ve mock toggle (`Pazarama:UseMock`)
- Unit testler (TDD)

## Kapsam Dışı

- Ürün publish/güncelleme (Faz 2)
- Stok/fiyat senkronizasyonu (Faz 2)
- Sipariş yönetimi (Faz 3)
- İade/iptal yönetimi (Faz 3)
- Fatura/finans/soru-cevap (Faz 4)

---

## Mimari Kararlar

### 1. MarketPlaceId = 4
Trendyol=1, N11=2, Hepsiburada için 3 ayrılmış. Pazarama 4 olarak atanır.

### 2. OAuth2 Token Yönetimi
- In-memory cache (`_accessToken`, `_tokenExpiresAt`)
- Thread-safe refresh: `SemaphoreSlim(1,1)`
- Expire'a 5 dk kala lazy refresh (55 dk'da yenile, 60 dk ömür)
- Auth URL: MarketPlace entity'ye eklenen `TokenUrl` alanından okunur (default: `https://isortagimgiris.pazarama.com/connect/token`). `BaseUrl` gibi DB'den yönetilebilir — staging/prod geçişlerinde kod değişikliği gerektirmez.
- API base URL: MarketPlace tablosundan (`https://isortagimapi.pazarama.com`)

### 3. GUID ID Mapping
Pazarama tüm ID'leri GUID formatında döner. Mevcut match tabloları int alan kullanır.

**Kategori mapping:** `CategoryMarketplace` entity'si zaten `ExternalCategoryId` (string) alanına sahip. `BaseCategoryImporterService.CreateMarketplaceLinkAsync()` bu alana GUID string'i otomatik yazar, `MarketPlaceCategoryId=0` kalır. **Ek migration gerekmez.**

**Attribute/Value/Brand mapping:** `CategoryAttributeMarketPlaceMatch`, `CategoryAttributeValueMarketPlaceMatch` ve `BrandMarketPlaceMatch` tablolarında sadece int alan var. Bu 3 tabloya nullable `string?` alanlar eklenir.

### 4. Flat-to-Tree Kategori Dönüşümü
Pazarama `getCategoryTree` endpoint'i tüm kategorileri düz liste olarak döner (parentId ilişkili). Client-side'da ağaç yapısına dönüştürülür. Lazy-load gerektirmez (N11'den farklı).

**Algoritma:** Single-pass dictionary-based yaklaşım:
1. Tüm kategori DTO'larını `Dictionary<Guid, ExternalCategoryDto>` ile indexle
2. Her item'ı `parentId`'sine göre parent'ın `Children` listesine ekle
3. Root node'lar: `parentId` null olan veya dictionary'de eşleşmeyen item'lar
4. `HasChildren = !Leaf` (Pazarama `leaf` boolean sağlıyor)

**Edge case:** Orphaned kategoriler (parentId mevcut ama parent listede yok) root olarak ele alınır ve loglanır.

### 5. DbContext Kullanım Kuralı
`PazaramaApiClient` ve tüm Pazarama servisleri `IDbContextFactory<IntegrationDbContext>` inject eder (scoped `IntegrationDbContext` DEĞİL). Her credential lookup için kısa ömürlü context oluşturulur ve dispose edilir. Bu, background service'lerde scoped DbContext çakışmasını önler — `TrendyolApiClient` ile aynı pattern.

---

## Bileşenler

### A. PazaramaApiClient

**Interface:** `IPazaramaApiClient` — GetAsync, PostAsync<T>, PutAsync<T>, DeleteAsync
**Sınıf:** `PazaramaApiClient`
**DI:** `IDbContextFactory<IntegrationDbContext>`, `IHttpClientFactory`, `ILogger<PazaramaApiClient>`

- Her çağrıda `IDbContextFactory` ile kısa ömürlü context oluşturur, MarketPlace tablosundan (Id=4) clientId (ApiKey), clientSecret (ApiSecret), BaseUrl ve TokenUrl çeker
- Token geçerliyse cache'den kullanır, değilse `SemaphoreSlim` ile thread-safe yeniler
- `Authorization: Bearer {token}` header'ı ekler

### B. PazaramaCategoryImporter

**Base:** `BaseCategoryImporterService`
**Source:** `ImportSource.Pazarama`
**DI kayıt:** Concrete class olarak (`services.AddScoped<PazaramaCategoryImporter>()`) — N11 pattern'ı ile aynı, interface arkasında DEĞİL. Blazor page'de concrete type inject edilir.

1. `GetExternalCategoriesAsync()` — `/category/getCategoryTree` çağırır, flat listeyi dictionary-based single-pass ile ağaca dönüştürür
2. `ImportCategoriesAsync()` — N11 pattern: `LoadMarketPlaceAsync(dbContext, "Pazarama")` çağır, sonra base class transaction döngüsü
3. `ImportCategoryAttributesAsync()` — Leaf kategoriler için `/category/getCategoryWithAttributes?Id={guid}` çağırır, attribute ve value'ları marketplace-aware dedup ile import eder. GUID eşleme: `MarketPlaceCategoryAttributeIdString` alanı kullanılır, int alan 0 kalır.

### C. PazaramaBrandService

**Interface:** `IPazaramaBrandService` — GetBrandsAsync, ImportBrandsAsync
**Sınıf:** `PazaramaBrandService`

- `/brand/getBrands` endpoint'inden sayfalı marka çekme
- İsim bazlı dedup: aynı isimde marka varsa sadece `BrandMarketPlaceMatch` entry oluşturur
- Match entry: `MarketPlaceBrandId=0`, `MarketPlaceBrandIdString=guid`

### D. Blazor UI

**PazaramaCategoryTreeView** — N11CategoryTreeView'dan basit (lazy-load yok, tüm veri tek API çağrısında gelir)
**CategoryImport sayfası** — yeni Pazarama tab (N11 tab pattern'ı kopyalanır)

### E. Response DTO'ları

`PazaramaResponse<T>` generic wrapper + domain-specific DTO'lar (Category, Brand, Attribute). `System.Text.Json` ile `JsonPropertyName` attribute'ları kullanılır.

### F. Mock Stratejisi

**Config key:** `Pazarama:UseMock` (default: `true`)

**Mock sınıflar:**
- `MockPazaramaApiClient : IPazaramaApiClient` — statik JSON yanıtları döner (kategori listesi, marka listesi, attribute listesi). Gerçek API olmadan geliştirme ve test yapılabilir.

DI toggle'ı:
```
var usePazaramaMock = configuration.GetValue<bool>("Pazarama:UseMock", true);
if (usePazaramaMock)
    services.AddScoped<IPazaramaApiClient, MockPazaramaApiClient>();
else
    services.AddScoped<IPazaramaApiClient, PazaramaApiClient>();
```

---

## Veritabanı Değişiklikleri

### Migration 1: AddPazaramaMarketplaceSupport
`CategoryAttributeMarketPlaceMatch` ve `CategoryAttributeValueMarketPlaceMatch` zaten `MarketPlaceCategoryAttributeExternalId` ve `MarketPlaceCategoryAttributeValueExternalId` (string) alanlarına sahip (Hepsiburada entegrasyonu ile eklenmiş). Pazarama bu mevcut alanları kullanır.

Sadece **1** entity'ye yeni alan eklenir:
- `BrandMarketPlaceMatch.MarketPlaceBrandExternalId` (string?, nullable)

Ayrıca `MarketPlace` entity'ye `TokenUrl` (string?, max 200) eklenir.

### Migration 2: AddMarketPlaceTokenUrl
`MarketPlace` entity'ye `TokenUrl` (string?, max 200) alanı eklenir. Pazarama seed'inde `https://isortagimgiris.pazarama.com/connect/token` olarak set edilir.

### Migration 3: SeedPazaramaMarketPlace
MarketPlace tablosuna Id=4 kaydı eklenir:
- Name="Pazarama"
- BaseUrl="https://isortagimapi.pazarama.com"
- TokenUrl="https://isortagimgiris.pazarama.com/connect/token"
- ApiKey, ApiSecret boş (Settings sayfasından girilir)

**Not:** Migration 2 ve 3 tek migration olarak birleştirilebilir.

---

## Sprint Yapısı

| Sprint | Kapsam | Tahmini Test |
|--------|--------|-------------|
| 1 - Infrastructure | Constants, ImportSource, migrations, MarketPlace entity güncelleme, PazaramaApiClient + MockPazaramaApiClient, DTO'lar, DI | ~12 |
| 2 - Category Import | PazaramaCategoryImporter (flat-to-tree), attribute import, Blazor UI tab, TreeView | ~8 |
| 3 - Brand Service | PazaramaBrandService, DI güncelleme | ~5 |

---

## Dosya Envanteri

### Yeni Dosyalar (~15)
1. `Business/Abstract/IPazaramaApiClient.cs`
2. `Business/Abstract/IPazaramaBrandService.cs`
3. `Business/Concrete/Pazarama/PazaramaApiClient.cs`
4. `Business/Concrete/Pazarama/MockPazaramaApiClient.cs`
5. `Business/Concrete/Pazarama/PazaramaResponseModels.cs`
6. `Business/Concrete/Pazarama/PazaramaBrandService.cs`
7. `Business/Concrete/Import/PazaramaCategoryImporter.cs`
8. `Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor`
9. `Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor.cs`
10. Migration: `AddMarketPlaceStringIdsAndTokenUrl.cs`
11. Migration: `SeedPazaramaMarketPlace.cs`
12. `Test/Pazarama/PazaramaConstantsTests.cs`
13. `Test/Pazarama/PazaramaApiClientTests.cs`
14. `Test/Pazarama/PazaramaCategoryImporterTests.cs`
15. `Test/Pazarama/PazaramaBrandServiceTests.cs`

### Değişecek Dosyalar (~6)
1. `Business/Utility/Constants/MarketPlaceConstants.cs` — PazaramaMarketPlaceId = 4
2. `Entity/Categories/ImportSource.cs` — Pazarama = 103
3. `Entity/MarketPlace.cs` — TokenUrl property ekleme
4. `Entity/Matches/BrandMarketPlaceMatch.cs` — MarketPlaceBrandExternalId string alan
5. `ApplicationBootstrap/ApplicationDependencyExtension.cs` — Pazarama DI block
6. `Blazor/Features/CategoryImport/CategoryImport.razor` + `.razor.cs` — Pazarama tab

**Not:** `CategoryAttributeMarketPlaceMatch` ve `CategoryAttributeValueMarketPlaceMatch` zaten string alanlarına sahip (Hepsiburada ile eklenmiş: `MarketPlaceCategoryAttributeExternalId`, `MarketPlaceCategoryAttributeValueExternalId`). Değişiklik gerekmez.

---

## Doğrulama

1. `dotnet build Entegrasyon.sln` — hatasız build
2. `dotnet test --filter "Pazarama"` — tüm yeni testler yeşil
3. Mevcut testler kırılmamış
4. Migration uygulandığında DB'de yeni kolonlar ve seed data mevcut
5. UI'da Pazarama tab görünür
