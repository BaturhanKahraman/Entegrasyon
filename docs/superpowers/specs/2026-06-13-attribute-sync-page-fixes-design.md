# Spec: Özellik Eşleme Sayfası — 3 Bug Düzeltmesi

**Tarih:** 2026-06-13
**Durum:** TL onayladı (tasarım) — spec review bekleniyor
**Sayfa:** `/marketplace/sync/attributes` (`AttributeSyncController`)
**İlgili spec:** [[2026-06-11-marketplace-sync-credential-disabled]] (Overview sayfasının credential mantığı — aynı `IsCredentialComplete()` tek kaynak)

---

## Problem (kullanıcı raporu)

`/marketplace/sync/attributes?mp=6` sayfasında üç ayrı bug:

1. **Credential gating yok.** API anahtarı olmayan pazaryeri tab'ları aktif görünüyor ve tıklanabiliyor. Hiçbir pazaryerinin credential'ı yoksa sayfa yine de açık. Beklenti: credential'sız pazaryeri pasif olsun; hiçbiri yoksa tüm sayfa pasif + "Lütfen en az bir pazaryerinin API bilgisini girin" hatası.
2. **Pazaryeri özelliği gelmiyor.** "Pazaryeri Özelliği" dropdown'ı boş. Trendyol için stage API'den gelmeli. (Aynı sorun değer eşleme modal'ındaki "Pazaryeri Değeri" dropdown'ında da var.)
3. **Eşleşme sayacı güncellenmiyor.** Üstteki `X / 44 özellik eşlendi` özet barı, farklı pazaryeri tab'ı seçilince değişmiyor.

---

## Kök Neden Analizi (systematic-debugging Phase 1)

### Bug #1 — Credential gating
`AttributeSyncController.Index` pazaryerlerini `marketPlaceManager.GetAllAsync()` ile çekip hepsini koşulsuz tab olarak basıyor. `MarketPlace.IsCredentialComplete()` (saf fonksiyon, `MarketPlaceCredentialExtensions`) zaten var ve `SyncOverviewController` + `IntegrationHealthVm` bunu kullanıyor — ama bu sayfa kullanmıyor.

### Bug #2 — Pazaryeri özelliği gelmiyor
Dropdown'lar `SuggestionsController` → `IMarketplaceSearchService` üzerinden besleniyor:
- `SearchAttributesAsync` **bizim** `dbContext.CategoryAttributes` tablosunu sorguluyor (pazaryerinin değil, kendi özelliklerimizi). Üstüne PostgreSQL `Contains` **case-sensitive** → `renk` araması `Renk`'i bulamıyor, boş dönüyor.
- `SearchAttributeValuesAsync` aynı şekilde **bizim** `CategoryAttributeValues`'u sorguluyor.

Kavramsal hata: eşleme = `bizim özellik → Trendyol özelliği` çevirisi; dropdown Trendyol'un özelliklerini göstermeli.

Trendyol gerçeği: attribute'lar **kategori-bazlı** (`GET /product/product-categories/{id}/attributes`), global "tüm attribute" endpoint'i **yok**. Response attribute + değerlerini birlikte döner (bkz. `TrendyolCategoryAttributeProvider`, WireMock `catalog/category-attributes.json`).

### Bug #3 — Sayaç güncellenmiyor
`Index.cshtml`'de özet bar (`mappedCount / totalCount` + progress) `#attribute-list-container` **dışında**. Tab linki `hx-target="#attribute-list-container"` sadece `_AttributeList` partial'ını swap ediyor → özet bar bayat kalıyor. `_AttributeList` partial'ında aggregate sayaç yok.

---

## Karar: Eşleşme Kardinalitesi

**1:1 her iki yönde** (kullanıcı kararı):
- Bizim her özelliğimiz, pazaryeri başına en fazla bir pazaryeri özelliğine eşlenir — şema zaten zorluyor (`HasKey(MarketPlaceId, ApplicationCategoryAttributeId)`).
- **Ters yön de zorlanacak:** bir Trendyol özelliği en fazla bizim bir özelliğimize. Yeni unique index + iş kuralı guard.

---

## Çözüm Tasarımı

### Bug #1 — Credential Gating (View + Controller)

**`AttributeSyncVm`** alanları:
```csharp
public bool AnyCredentialed { get; set; }     // en az bir pazaryeri credential tam mı
public bool SelectedHasCredentials { get; set; } // seçili mp credential tam mı
```
`MarketPlaces` listesi tüm pazaryerlerini taşımaya devam eder; her tab credential durumuna göre render edilir.

**`AttributeListItemVm`** — değişiklik yok.

**`AttributeSyncController.Index`:**
- `marketPlaces` çekildikten sonra `AnyCredentialed = marketPlaces.Any(m => !m.IsDeleted && m.IsCredentialComplete())`.
- `AnyCredentialed == false` → attribute/match sorgularını **atla** (gereksiz iş), `vm` boş listeyle dönsün, view full-page empty-state göstersin.
- Seçili `mp` credential'sız VE en az bir credential'lı pazaryeri varsa → ilk credential'lı pazaryerine yönlendir (`RedirectToAction(Index, new { mp = firstCredentialedId })`). Bu PRG-uyumlu, HTMX değilse normal redirect; HTMX tab tıklamasında zaten credential'lı tab'a tıklanamayacağı için bu yol nadir.
- `SelectedHasCredentials = marketPlaces.FirstOrDefault(m => m.Id == mp)?.IsCredentialComplete() ?? false`.

**`Index.cshtml`:**
- `@if (!Model.AnyCredentialed)` → Tabler `empty` bileşeni (doc: https://tabler.io/docs/ui/empty): ikon + başlık "Aktif pazaryeri yok" + alt metin "Lütfen en az bir pazaryerinin API bilgilerini girin." + birincil buton → `/settings/integrations` (Entegrasyon ayarları). Tab barı, özet bar, iki kolon **render edilmez**.
- `AnyCredentialed == true` ise tab barı: credential'sız tab `nav-link disabled` + `hx-*` attribute'ları yok + `title="API bilgisi eksik"` + soluk rozet. Credential'lı tab eskisi gibi.

### Bug #3 — Sayaç swap edilen bölgeye taşı

- Özet bar (progress + `X / Y`) `Index.cshtml`'den çıkarılıp **`_AttributeList.cshtml`** partial'ının başına taşınır (kart üstünde). Hesaplama partial içinde `Model.Attributes` üzerinden yapılır.
- `Index.cshtml` iki-kolon `row`'undan ÖNCE özet barı kaldırılır; partial zaten `#attribute-list-container` içinde olduğundan tab değişiminde sayaç + progress birlikte güncellenir.
- Not: `_AttributeList` `AttributeSyncVm` alıyor (zaten öyle) — ekstra parametre gerekmez.

### Bug #2 — Pazaryeri Özelliği/Değeri stage API'den

**Yeni servis: `ITrendyolAttributeCatalog` (Business/Concrete/Trendyol)**
Trendyol'a eşli kategorilerin attribute+değerlerini stage API'den toplayıp cache'ler.

```csharp
public interface ITrendyolAttributeCatalog
{
    Task<IReadOnlyList<MarketplaceAttributeSearchResult>> SearchAttributesAsync(string query, CancellationToken ct);
    Task<IReadOnlyList<MarketplaceOption>> SearchValuesAsync(int trendyolAttributeId, string query, CancellationToken ct);
}
```

İç işleyiş:
1. `TenantMemoryCache` (mevcut tenant-aware cache) anahtarı `"TrendyolAttributeCatalog"`, TTL 1 saat.
2. Cache miss → `categoryMatchService.GetAllCategoryMappingsAsync(1)` ile eşli Trendyol kategorilerini al (`MarketPlaceCategoryId` = Trendyol kategori ID).
3. Her kategori için `IMarketplaceCategoryAttributeProvider.GetAttributesForCategoryAsync(catId)` (zaten var, credential-aware `TrendyolApiClient` kullanır, WireMock'ta mock'lu). Dış API çağrıları sınırlı paralellikle (örn. `SemaphoreSlim(4)`).
4. Sonuçları **attribute id ile dedupe** et: `Dictionary<int attrId, (string name, Dictionary<int valId, string valName> values)>`. Değerler de id ile dedupe.
5. Cache'e yaz. Arama: `name.Contains(query, OrdinalIgnoreCase)`, ilk 20.
6. Hiç eşli kategori yoksa boş liste (caller boş-durum gösterir).

**`TrendyolMarketplaceSearchService` güncellemesi:**
- `SearchAttributesAsync(mp, q)`: `mp == 1` → `ITrendyolAttributeCatalog.SearchAttributesAsync`. Diğer mp → şimdilik boş `SuccessDataResult` (gerçek implementasyon yok; credential gating zaten bu tab'ları kapatır).
- `SearchAttributeValuesAsync(mp, attributeId, q)`: burada `attributeId` artık **bizim** application attribute id. Trendyol değerlerini getirmek için **eşli Trendyol attribute id**'sine ihtiyaç var → değer modal'ı zaten bizim value'yu Trendyol value'ya eşliyor; modal Trendyol attribute id'sini bilmeli.

**Değer dropdown'ı için akış düzeltmesi:** Trendyol değerleri ancak özellik bir Trendyol attribute'una eşliyse anlamlı (değerler o attribute'a ait). Bu yüzden:
- Özellik **eşli değilse** (`Model.AttributeMatch is null`): değer "Eşle" butonları **disabled**, üstte ipucu "Önce bu özelliği bir pazaryeri özelliğine eşleyin."
- Özellik **eşliyse**: modal `data-trendyol-attr-id="@Model.AttributeMatch.MarketPlaceCategoryAttributeId"` taşır; değer arama bu id ile Trendyol değerlerini getirir.

Suggestions endpoint imzası:
`GET /marketplace/sync/suggestions/attributes/values?mp=1&mpAttributeId={trendyolAttrId}&q=...`
(eski `{attributeId}/values` route'u yerine; controller + JS güncellenir.)

**1:1 zorlama:**
- `CategoryAttributeMarketPlaceMatchEntityConfiguration`: `builder.HasIndex(x => new { x.MarketPlaceId, x.MarketPlaceCategoryAttributeId }).IsUnique();`
- `AttributeMatchManager.SaveAttributeMatchAsync`: mevcut "bu özellik zaten eşli" guard'ına ek olarak — aynı `(MarketPlaceId, MarketPlaceCategoryAttributeId)` başka bir application attribute'a eşliyse `ErrorResult("Bu pazaryeri özelliği başka bir özelliğinize zaten eşlenmiş.")`.
- Migration: `dotnet ef migrations add UniqueTrendyolAttributeMatch ...` + update + has-pending-model-changes doğrula.

---

## Dosya Değişiklikleri

| Dosya | Değişiklik |
|---|---|
| `Features/MarketplaceSync/ViewModels/AttributeSyncVm.cs` | `AnyCredentialed`, `SelectedHasCredentials` |
| `Features/MarketplaceSync/AttributeSyncController.cs` | credential hesap + erken çıkış + redirect; value suggestions param |
| `Features/MarketplaceSync/Views/AttributeSync/Index.cshtml` | full-page empty-state; tab disabled; özet barı kaldır |
| `Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeList.cshtml` | özet bar (progress + sayaç) buraya taşı |
| `Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeMatchPanel.cshtml` | değer modal'ı Trendyol attr id + yeni suggestions URL |
| `Features/MarketplaceSync/SuggestionsController.cs` | value endpoint imza değişimi (`mpAttributeId`) |
| `Business/Abstract/ITrendyolAttributeCatalog.cs` | **yeni** |
| `Business/Concrete/Trendyol/TrendyolAttributeCatalog.cs` | **yeni** |
| `Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs` | attribute/value aramayı catalog'a yönlendir |
| `Business/Concrete/AttributeMatchManager.cs` | 1:1 ters-yön guard |
| `DataAccess/.../EntityConfigurations/CategoryAttributeMarketPlaceMatchEntityConfiguration.cs` | unique index |
| `DataAccess/.../Migrations/*` | yeni migration |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | `ITrendyolAttributeCatalog` DI kaydı |

---

## Test Stratejisi (TDD-First)

**Unit (Entegrasyon.UnitTest):**
- `TrendyolAttributeCatalog`: eşli kategorilerden attribute toplama + dedupe; boş kategori → boş liste; query filtresi case-insensitive. Provider + categoryMatchService + TenantMemoryCache mock'lanır.
- `AttributeMatchManager.SaveAttributeMatchAsync`: ters-yön 1:1 guard (aynı Trendyol attr id başka bizim özelliğe eşliyse hata).
- Credential VM hesabı: `AnyCredentialed`/`SelectedHasCredentials` (controller'ı ince tutup mantık test edilebilirse).

**Integration (Entegrasyon.IntegrationTest):**
- Unique index migration: aynı `(mp, MarketPlaceCategoryAttributeId)` ikinci insert → DbUpdateException.
- `SaveAttributeMatchAsync` persist (no-tracking footgun): kayıt gerçekten yazılıyor mu (RED-first).

**E2E (Entegrasyon.E2E — stage/prod, develop'ta değil):**
- Credential'sız mp → empty-state.
- Tab değişiminde sayaç güncelleniyor.
- (E2E develop'ta koşulmaz; manuel dev doğrulaması + integration yeterli — bkz. [[e2e-explore-first-verify]].)

**Manuel dev doğrulama (192.168.1.78:8085, Trendyol stage):**
- Trendyol tab'ı: özellik seç → "Pazaryeri Özelliği" dropdown'a "renk" yaz → Trendyol attribute'ları gelmeli.
- Tab değiştir → sayaç değişmeli.
- Credential'sız tab disabled.

---

## Riskler / Notlar

- **WireMock dev davranışı:** `category-attributes.json` her kategori id için aynı 3 attribute'u döner → dev'de dedupe sonrası 3 attribute görünür; bu beklenen (gerçek API farklı kategoriye farklı set döner).
- **Hiç kategori eşli değilse** Trendyol attribute listesi boş olur → dropdown boş + form-hint "Önce kategori eşleştirin" mesajı. Bu bir bug değil, doğru durum.
- **Multi-tenant:** `TenantMemoryCache` zaten tenant-izole; yeni catalog onun üstünde, global state yok.
- **Push:** iş bitince `git push origin develop && git push gitea develop` (Gitea runner dev deploy) — bkz [[push-both-remotes]].
