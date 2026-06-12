# Category Attribute — Kontrollü Kelime Dağarcığı (Tam Sökme)

**Tarih:** 2026-06-12
**Durum:** Onaylandı (tasarım) → writing-plans
**Kapsam:** B — `AllowCustom` + `CustomValue` tam sökme (canlı veri yok, migration ağrısı yok)

## Problem

Category attribute'larda serbest düz yazıya izin veriyoruz (`CategoryAttribute.AllowCustom = true` → kullanıcı `AttributeKeyValue.CustomValue`'ya serbest string basıyor). Kullanıcı "Sarı", "SARI", "sari" gibi aynı şeyin üç farklı yazımını girebiliyor. Bu:

- Raporları, filtreleri böler (üç ayrı değermiş gibi sayılır).
- Variant'ları bölünür / kirletir.
- Pazaryeri eşleştirmesini imkânsızlaştırır.

Sistem henüz **canlıda değil**; tüm kategori/attribute/value verisi master DB'den çekilmiş halde duruyor. Veri migration ağrısı olmadan modeli temizleyebileceğimiz pencere bu.

## Karar Özeti

| Konu | Karar |
|---|---|
| Serbest yazı | **Kalkar.** Her attribute = büyüyen kapalı liste. |
| Yazılan yeni değer | TR-kanonik normalize → attribute içinde tekilleştir → gerçek `CategoryAttributeValue`'ya terfi. |
| `AllowCustom` (Trendyol kuralı) bilgisi | **Tamamen silinir** (seçenek A). Gönderimde reddi o an hata olarak gösteririz; arka planda flag tutmayız. |
| Kanonikleştirme gücü | **Kanonik-tekilleştirme** (trim + iç boşluk teke + TR-duyarlı büyük/küçük katlama). Fuzzy "bunu mu demek istedin" YOK (sonraya). |
| `CustomValue` kolon kapsamı | **Tam sökme (B).** İki entity'den de düşür, ~30 okuma noktasını yeniden yaz. |

## Hedef Davranış

1. Kullanıcı ürün eklerken attribute değerini **selectbox**'tan seçer (Tom Select).
2. Listede olmayan değer girmek isterse **inline ekleme** (marka ekleme tarzı, ilk adımda) → değer normalize edilip tekilleştirilir, gerçek `CategoryAttributeValue` olarak eklenir, seçili gelir, sonraki üründe listede hazır.
3. Ayrı bir **attribute değer yönetim sayfası** ile değerler listelenir/eklenir/düzenlenir/soft-delete edilir.
4. Pazaryerine gönderimde değerin pazaryeri-match'i varsa mapped id, yoksa string basılır; pazaryeri reddederse hata yüzeye çıkar ve kullanıcı eşlemeye yönlendirilir.

## Mimari

### 1. Veri Modeli (IntegrationDbContext)

**Düşen alanlar:**
- `CategoryAttribute.AllowCustom` (bool) — düşer.
- `AttributeKeyValue.CustomValue` (string?) — düşer. `AttributeValueId` artık **zorunlu** (gerçek değere işaret eder; mevcut `AttributeValueId == 0` sentinel mantığı biter).
- `ProductVariantAttribute.CustomValue` (string?) — düşer. `CategoryAttributeValue` (string snapshot) + `CategoryAttributeValueId` kalır.
- `TemplateCategoryAttributeData.AllowCustom` — düşer (tutarlılık; matched-entity template).

**Eklenen alanlar:**
- `CategoryAttributeValue.NormalizedName` (string) — kanonik anahtar.
- Unique index `(CategoryAttributeId, NormalizedName)`, **filtreli** `IsDeleted = false` (soft-delete edilmiş değer aynı isimle yeniden eklenebilsin; aktif kümede tekillik garanti).

**Migration (tek):**
1. `AllowCustom` kolonlarını düşür (CategoryAttribute, TemplateCategoryAttributeData).
2. `CustomValue` kolonlarını düşür (AttributeKeyValue, ProductVariantAttribute).
3. `AttributeKeyValue.AttributeValueId` → non-null.
4. `CategoryAttributeValue.NormalizedName` ekle + mevcut seed değerleri backfill (normalize fonksiyonu ile).
5. Filtreli unique index oluştur.
6. Strict-rule: `migrations add` → gözden geçir → `database update` → `has-pending-model-changes` doğrula.

> **Not:** Backfill, mevcut seed değerlerinde kanonik çakışma üretebilir (örn. master'dan gelen "Sarı" + "SARI"). Migration'dan ÖNCE veya backfill adımında çakışan aktif değerleri **tekilleştirme** stratejisi gerekir (en eski id'yi koru, diğerlerini soft-delete + `AttributeKeyValue`/`ProductVariantAttribute` referanslarını koruyan id'ye yönlendir). Bu, plan'da ayrı bir adım olarak ele alınır.

### 2. Kanonikleştirme (dedup motoru)

`NormalizeAttributeValue(string raw) → string`:
- `Trim()`
- İç ardışık boşlukları tek boşluğa indir.
- TR-duyarlı invariant büyük harf katlama: `İ/i` ve `I/ı` çiftlerini doğru ele al (`tr-TR` culture veya elle eşleme; `ToUpperInvariant`'ın İ/ı bug'ına dikkat).
- Sonuç = `NormalizedName`. Saklanan görünen `Name` = trim'lenmiş ham giriş (ilk görülen yazım korunur).

Karşılaştırma/tekillik **daima** `NormalizedName` üzerinden. "Sarı" = " sarı " = "SARI" = "sari" → tek değer. "Açık Sarı" farklı kanonik → ayrı değer (kasıtlı).

### 3. Business Katmanı

- **`CategoryAttributeValueManager`** (genişlet):
  - `GetOrCreate(int categoryAttributeId, string rawName) → int valueId` — terfi motoru. Pipeline: Validation (boş/uzunluk) → BusinessRules (LogicRunner) → Execution. Normalize → `(attrId, normalized)` lookup → varsa id, yoksa insert (AsTracking/Update ile persist; unique-index race → catch + re-lookup).
  - Yönetim CRUD: liste (attribute başına), ekle, düzenle (Name güncelle → NormalizedName yeniden hesapla, çakışırsa hata), soft-delete.
  - **EF Mutasyon Persist strict-rule:** her mutasyonda `.AsTracking()` veya `Update`/`ExecuteUpdate`; RED-first persist testi.
- **`AttributeKeyValueManager.ClearEmptyAttributes`:** `AttributeValueId` yoksa düşür (CustomValue dalı kalkar).
- **`AttributeKeyValueDto`:** `CustomValue` alanı kalkar.
- **`CustomValue` referans sökümü (~30 nokta):** her `X ?? CustomValue` → sadece `X` (`CategoryAttributeValue`):
  - `IVariantNamingService` / `VariantAttributeLite` (CustomValue alanı kalkar) / `VariantNamingService`
  - `ProductVariantManager`, `ProductManager`, `SaleManager`, `LabelManager`, `DiscountManager`, `MarketplaceOverrideManager`, `OfficeStockManager`, `StockTransferRequestManager`, `BranchOfficeManager`
- **Importer'lar (7):** `AllowCustom` ataması kalkar — `TrendyolCategoryImporter`, `TrendyolCategoryImporterService`, `CiceksepetiCategoryImporter`, `HepsiburadaCategoryImporter`, `N11CategoryImporter`, `N11RestCategoryImporter`, `PazaramaCategoryImporter`, `MatchedEntityImportManager`. (Pazaryeri DTO'larındaki `allowCustom` okuması da — örn. `AttributeAutoMatchService` JSON serialize — gözden geçir; match için gerekiyorsa pazaryeri-DTO seviyesinde kalabilir, bizim entity'den kalkar.)
- **Validatorlar:** `AddCategoryAttributeDtoValidator`, `EditCategoryAttributeDtoValidator` — AllowCustom kuralları kalkar.
- **Mapper:** `CategoryAttributeMapper` — AllowCustom satırı kalkar.
- **DTO'lar:** `AddCategoryAttributeDto`, `EditCategoryAttributeDto`, `CategoryAttributeDto` — AllowCustom kalkar.

### 4. MVC Katmanı

- **Yeni: Attribute Değer Yönetim Sayfası** (`Features/Attributes` veya `Features/Categories` altında):
  - Attribute seç → değerlerini listele (Tabler datagrid).
  - Ekle (inline/modal) → `GetOrCreate` → dedup geri bildirimi ("zaten var, mevcut kullanıldı" veya "eklendi").
  - Düzenle (Name) / soft-delete.
  - Tabler strict-rule: bileşen class'ları `docs.tabler.io`'dan doğrulanır.
- **Ürün Sihirbazı (`CreateProductVm` + view):**
  - Attribute girişi **her zaman Tom Select selectbox** (değerlere bağlı). `AllowCustom`-tetikli text-input dalı **silinir** (`CreateProductVm.AllowCustom` ×2 + view koşulları).
  - Inline "+ yeni değer" (marka ekleme tarzı): HTMX/JS POST → value-create endpoint → dönen id ile option eklenir + seçilir.
- **Kategori Attribute Create/Edit:** `CategoryController` (satır 126, 144, 248, 305), `CategoryCreateVm`, `EditAttributeVm`, `Features/Attributes/Views/Index`, `_AttributeDetail` partial — AllowCustom checkbox/kolon/koşul kalkar.

### 5. Pazaryeri Gönderim Yolu

- Ürün publish kod yolunda (spec implementasyonunda tam haritalanacak — Trendyol product send mapper) her `AttributeKeyValue` için:
  - `CategoryAttributeValueMarketPlaceMatch` ile değer→pazaryeri-value-id ara.
  - **Match varsa:** `{ attributeId: <trendyol attr id>, attributeValueId: <trendyol value id> }`.
  - **Match yoksa:** `{ attributeId: <trendyol attr id>, customAttributeValue: <value.Name> }`.
  - Trendyol `allowCustom=false` + eşleşmesiz değer → API reddi → hata yüzeye çıkar; kullanıcı eşleme ekranına yönlendirilir. (Seçenek A — önden flag yok.)

### 6. Test Stratejisi (TDD-First, RED→GREEN)

- **Unit:**
  - `NormalizeAttributeValue`: Sarı/SARI/sari/" sarı " → aynı kanonik; İ/ı/I/i doğru; "Açık Sarı" ≠ "Sarı".
  - `GetOrCreate`: aynı kanonik → aynı id (yeni satır YOK); yeni kanonik → yeni satır.
  - `ClearEmptyAttributes`: AttributeValueId yoksa düşer.
- **Integration (RED-first persist):**
  - Unique-index dedup: aynı normalized iki kez insert → ikinci catch + mevcut id.
  - Ürün create: seçilen attribute `AttributeValueId` persist olur (no-tracking footgun testi).
  - Variant naming: CustomValue'suz doğru isim.
  - Migration uygulanır + `has-pending-model-changes` temiz.
- **E2E:**
  - Ürün ekle → değer seç → inline yeni değer ekle → görünür + tekrar üründe listede.

## Sıra & Risk

Geniş blast radius. Önerilen sıra:
1. Entity değişiklikleri (alan düşür/ekle).
2. Backfill/tekilleştirme stratejisi + migration (strict-rule).
3. Business consumer fix (~30 nokta) + importer + validator + mapper + DTO.
4. MVC (yönetim sayfası + sihirbaz + kategori formları).
5. Pazaryeri gönderim mapping.
6. Build yeşil → unit → integration → E2E.
7. Bağımsız review gate: `ecc:csharp-reviewer`, `ecc:database-reviewer` (migration/index/unique), `ecc:security-reviewer` (değer-create endpoint, kullanıcı girdisi).

**Regresyon yüzeyi:** variant/sale/label/discount/stock-transfer/branch-office yolları — hepsi `CustomValue` fallback'i okuyor. Söküm sonrası bu yolların testleri koşulmalı.

## YAGNI / Kapsam Dışı

- Fuzzy "bunu mu demek istedin" (Levenshtein/trigram) — sonraya.
- Pazaryeri `allowCustom` önden-uyarı katmanı (seçenek B) — sonraya.
- Çok-attribute toplu değer import/CSV — bu spec'te yok.
