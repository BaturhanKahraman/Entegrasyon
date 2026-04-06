# Product Adding Wizard — Design Spec

**Tarih:** 2026-04-06
**Durum:** Onaylandı

## Genel Bakış

Mevcut 3 adımlı ürün ekleme wizard'ını 6 adımlı, kapsamlı bir akışa genişletiyoruz. Yeni wizard: aranabilir marka seçimi (yoksa anında ekleme), kategori özellikleri yönetimi, varianter/slicer bazlı otomatik varyant oluşturma, toplu görsel yönetimi ve kayıt sonrası yönlendirme içerecek.

## Wizard Adımları

### Adım 1 — Genel Bilgiler

**Alanlar:**
- Ürün Adı (zorunlu)
- Stok Kodu (opsiyonel, benzersiz olmalı)
- Marka (zorunlu, aranabilir selectbox)
- Kategori (zorunlu, aranabilir selectbox, sadece leaf kategoriler)
- Sezon (opsiyonel)
- Yıl (opsiyonel)
- Açıklama (opsiyonel, textarea)

**Marka Aranabilir Selectbox Davranışı:**
1. Kullanıcı yazmaya başlayınca mevcut markalar filtrelenir
2. Eşleşen marka varsa seçer, akış devam eder
3. Eşleşen marka YOKSA ve kullanıcı Enter'a basar veya selectbox'tan çıkarsa:
   - **Modal 1:** "X markası bulunamadı. Eklemek ister misiniz?" (Evet/Hayır)
   - Evet → `IBrandService.AddBrand()` çağrılır (validation + business rules dahil)
   - Başarılıysa marka eklenir ve selectbox'ta seçili hale gelir
   - **Modal 2:** "Bu marka yeni eklendiği için eşleştirilmemiş olacak. Sonradan pazaryerleriyle eşleştirebilirsiniz." (Tamam butonu)

**Kategori Selectbox Davranışı:**
- Aranabilir dropdown, sadece leaf kategoriler listelenir
- Business rule: `LogicRunner` içinde "Seçilen kategori leaf olmalıdır" kuralı eklenir
- `GetLeafCategoriesAsync()` mevcut metod kullanılır

**Geçiş kuralı:** Title, BrandId, CategoryId zorunlu. Validasyon geçmeden Adım 2'ye geçilemez.

### Adım 2 — Kategori Özellikleri

Seçilen kategorinin **varianter ve slicer OLMAYAN** attribute'leri dinamik olarak yüklenir.

**Yükleme:**
- `ICategoryAttributeManager.GetCategoryAttributesByCategory(categoryId)` çağrılır
- Dönen listeden `IsVarianter == false && IsSlicer == false` olanlar filtrelenir
- Her attribute için `IsRequired` flag'ine göre zorunlu/opsiyonel işaretlenir

**UI Davranışı:**
- Her attribute bir satır olarak gösterilir
- `AllowCustom == false` → Dropdown (CategoryAttributeValues listesinden)
- `AllowCustom == true` → Serbest text input
- Zorunlu alanlar kırmızı yıldız ile işaretlenir

**Geçiş kuralı:** Tüm `IsRequired == true` attribute'ler doldurulmadan Adım 3'e geçilemez.

**Veri modeli:** Her attribute değeri `AttributeKeyValue` olarak saklanacak:
- `CategoryAttributeId` → attribute ID
- `AttributeValueId` → seçilen predefined değer (AllowCustom=false ise)
- `CustomValue` → serbest giriş (AllowCustom=true ise)

### Adım 3 — Varyant Oluşturma

Seçilen kategorinin **varianter ve slicer** attribute'leri yüklenir ve kullanıcı değer seçer.

**Attribute Yükleme:**
- Aynı `GetCategoryAttributesByCategory(categoryId)` sonucundan `IsVarianter == true || IsSlicer == true` olanlar filtrelenir

**Değer Seçim UI'ı (AllowCustom'a göre dinamik):**
- `AllowCustom == false` → **Multi-select checkbox listesi:** Mevcut `CategoryAttributeValues` gösterilir, kullanıcı istediğini işaretler
- `AllowCustom == true` → **Tag-input:** Kullanıcı yazıp virgül veya Enter'la tag ekler. Her tag bir değer olur. Tag'ler silinebilir (X butonu)

**Toplu Varsayılan Değerler:**
Attribute seçimlerinin altında tek bir form bloğu:
- Liste Fiyatı (decimal)
- Satış Fiyatı (decimal)
- Maliyet Fiyatı (decimal)
- KDV Oranı (decimal, default %20)
- Stok (int)

**Varyant Oluşturma:**
- "Varyantları Oluştur" butonu → seçilen tüm varianter + slicer attribute değerlerinin kartezyen çarpımı hesaplanır
- Her kombinasyon bir varyant satırı olur
- Tüm varyantlar toplu varsayılan değerlerle doldurulur
- Varyant sayısı buton üzerinde gösterilir: "Varyantları Oluştur (3×2 = 6 varyant)"

**Varyant Tablosu (oluşturulduktan sonra):**
| Kolon | Açıklama |
|-------|----------|
| Varianter/Slicer attribute değerleri | Readonly, kombinasyondan gelir |
| Liste Fiyatı | Editable, varsayılandan gelir |
| Satış Fiyatı | Editable |
| Maliyet Fiyatı | Editable |
| KDV Oranı | Editable |
| Stok | Editable |
| Barkod | Editable, boş bırakılırsa otomatik üretilir |

Kullanıcı her varyant için değerleri tek tek override edebilir.

**Geçiş kuralı:** En az 1 varyant oluşturulmuş olmalı.

### Adım 4 — Görsel Yönetimi

**Toplu Yükleme Alanı:**
- Drag-and-drop veya file picker ile birden fazla görsel yüklenir
- Yüklenen görseller thumbnail olarak listelenir
- Mevcut MinIO + ImageSharp altyapısı kullanılır

**Varyanta Atama:**
- Her varyant bir satır olarak gösterilir (attribute değerleriyle)
- Yüklenen görseller havuzdan varyanta atanır (dropdown/seçim ile)
- Bir görsel birden fazla varyanta atanabilir
- Her varyant için bir "ana foto" seçilebilir (yıldız/star ikonu)

**Geçiş kuralı:** Görsel ekleme opsiyonel — görselsiz de ileri geçilebilir.

### Adım 5 — Önizleme & Onay

**Gösterilen bilgiler:**
- Genel bilgiler özeti (ürün adı, marka, kategori, stok kodu)
- Kategori özellikleri listesi
- Varyant tablosu (attribute değerleri + fiyat + stok + görsel sayısı)
- Her varyantın thumbnail görselleri

**Aksiyon:** "Ürünü Kaydet" butonu → `ProductManager.AddProduct()` çağrılır

**Hata durumu:** Kayıt başarısızsa hata mesajı gösterilir, kullanıcı ilgili adıma geri dönebilir.

### Adım 6 — Başarı & Yönlendirme

Kayıt başarılı olduğunda gösterilen başarı sayfası:
- Ürün adı + varyant sayısı + görsel sayısı özeti
- 3 buton:
  1. **"Senkronizasyona Git"** → İlgili ürünün sync sayfasına yönlendirir
  2. **"Ürün Listesine Dön"** → Ürün listesine redirect
  3. **"Yeni Ürün Ekle"** → Wizard'ı sıfırdan başlatır

## Teknik Tasarım

### HTMX Wizard Akışı

Mevcut HTMX wizard pattern'i korunur ve genişletilir:
- Her adım bir partial view (`_CreateStepX.cshtml`)
- Step geçişleri `hx-post` ile controller action'a gider
- Controller partial view döner, `#wizard-content` hedefine swap edilir
- Wizard state `TempData` üzerinden JSON serialize/deserialize ile taşınır

### Controller Actions

```
GET  /products/add              → Create()         → Create.cshtml (Step 1)
POST /products/add/step1        → CreateStep1()     → _CreateStep1.cshtml → _CreateStep2.cshtml
POST /products/add/step2        → CreateStep2()     → _CreateStep2.cshtml → _CreateStep3.cshtml
POST /products/add/step3        → CreateStep3()     → _CreateStep3.cshtml → _CreateStep4.cshtml
POST /products/add/step4        → CreateStep4()     → _CreateStep4.cshtml → _CreateStep5.cshtml
POST /products/add/save         → CreateSave()      → _CreateStep6.cshtml (success)
```

**Ek HTMX Endpoint'leri:**
```
GET  /products/add/brand-search?q=  → BrandSearch()          → JSON (marka arama)
POST /products/add/brand-quick-add  → BrandQuickAdd()        → JSON (hızlı marka ekleme)
GET  /products/add/category-search?q= → CategorySearch()     → JSON (kategori arama)
GET  /products/add/attributes/{categoryId} → GetAttributes()  → _AttributeFields partial
POST /products/add/generate-variants → GenerateVariants()     → _VariantTable partial
POST /products/add/upload-image     → UploadImage()          → JSON (görsel yükleme)
```

### ViewModel Yapısı

`CreateProductVm` genişletilir:

```csharp
// Step 1
public string Title { get; set; }
public string StockCode { get; set; }
public int BrandId { get; set; }
public int CategoryId { get; set; }
public string? BrandName { get; set; }
public string? CategoryName { get; set; }
public string? Season { get; set; }
public string? Year { get; set; }
public string? Description { get; set; }

// Step 2 - Category Attributes (non-varianter, non-slicer)
public List<AttributeValueVm> CategoryAttributes { get; set; }

// Step 3 - Variant Generation
public List<VariantAttributeSelectionVm> VariantAttributeSelections { get; set; }
public DefaultVariantValuesVm DefaultValues { get; set; }
public List<CreateVariantVm> Variants { get; set; }

// Step 4 - Images (temporary upload references)
public List<VariantImageAssignmentVm> ImageAssignments { get; set; }
```

### Business Rules (LogicRunner)

Yeni kurallar:
1. **LeafCategoryRule:** Seçilen kategorinin alt kategorisi olmamalı (leaf olmalı)
2. **RequiredAttributeRule:** Zorunlu category attribute'lerin hepsinin değeri olmalı
3. **MinimumOneVariantRule:** En az 1 varyant oluşturulmuş olmalı
4. **VariantUniquenessRule:** Aynı attribute kombinasyonuyla iki varyant olamaz

### Mevcut Kodla Uyumluluk

- `ProductManager.AddProduct()` mevcut pipeline korunur (validation → business rules → execution)
- `AddProductDto` genişletilir: attribute'ler ve görsel referansları eklenir
- `BarcodeService.GenerateAsync()` boş barkodlar için mevcut mantık korunur
- `IBrandService.AddBrand()` mevcut servis kullanılır (hızlı ekleme için)
- MinIO + ImageSharp mevcut altyapı görsel yükleme için kullanılır
- `AttributeKeyValueManager.ClearEmptyAttributes()` mevcut temizleme korunur

### JavaScript

Vanilla JS, ~100 satır sınırı gözetilir. Yeni JS fonksiyonları:
- Aranabilir selectbox (marka + kategori) — debounced fetch
- Tag-input (AllowCustom=true attribute'ler için)
- Kartezyen çarpım hesaplama (client-side sayı gösterimi)
- Görsel drag-and-drop + varyanta atama
- Ana foto seçim toggle

### Test Stratejisi

1. **Unit Tests:** Yeni business rule'lar, kartezyen çarpım hesaplama, ViewModel mapping
2. **Integration Tests:** Tam wizard flow (step1 → save), marka ekleme + ürün ekleme birlikte
3. **E2E Tests (Playwright):** Her wizard adımını tıklayarak geçen tam senaryo:
   - Marka yazıp bulamayıp ekleme
   - Kategori seçip attribute doldurma
   - Varyant oluşturup fiyat girme
   - Görsel yükleyip atama
   - Review edip kaydetme
   - Başarı sayfasını doğrulama
