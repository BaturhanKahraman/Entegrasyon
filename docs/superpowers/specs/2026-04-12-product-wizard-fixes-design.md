# Product Wizard — 5 Fix Spec

**Tarih:** 2026-04-12
**Kapsam:** Wizard state koruma, validation düzeltmeleri, entity migration, detay sayfası mağaza ayarları

---

## Fix 1 — BackToStep2 State Koruma

### Sorun
Step 3'ten Step 2'ye geri gidildiğinde önceki özellik seçimleri (dropdown, text input) sıfırlanıyor.

### Root Cause
`_CreateStep2Attributes.cshtml` view'ında model'den gelen `ValueId` ve `CustomValue` değerleri form elementlerine yansıtılmıyor:
- `<select>` elementlerinde eşleşen `<option>`'a `selected` attribute'u eklenmiyor
- `<input type="text">` (AllowCustom) elementlerinde `value` attribute'u boş

### Çözüm
**Dosya:** `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml`

1. AllowCustom input'larına (satır 49-51): `value="@Model.CategoryAttributes[i].CustomValue"` ekle — ancak model'deki `CategoryAttributes` index'leri ile view'daki `attrs` listesi farklı sıralanabilir. Eşleştirme `CategoryAttributeId` üzerinden yapılmalı.

2. Select dropdown'larında (satır 55-62): Her `<option>` için model'deki `ValueId` ile karşılaştırma yapılıp `selected` eklenmeli.

3. **Eşleştirme stratejisi:** View'da `attrs` listesi DB'den geliyor (ViewBag), `Model.CategoryAttributes` ise Session'dan. Index'ler farklı olabilir. Her `attr` için `Model.CategoryAttributes.FirstOrDefault(a => a.CategoryAttributeId == attr.Id)` ile eşleştir.

### Doğrulama
- Step 1 → Step 2 (özellik seç) → Step 3 → BackToStep2 → seçimler korunmuş olmalı
- Hem dropdown hem custom text input için test

---

## Fix 2 — Varsayılan Stok Alanını Kaldır

### Sorun
Step 3'te "Varsayılan Stok" input'u var ama stok yönetimi şube bazlı yapılıyor, bu alan gereksiz.

### Çözüm

**View:** `_CreateStep3Variants.cshtml` satır 134-138 kaldır. Grid'i `col-md-2` → `col-md-3` genişlet (4 alan kaldı, daha dengeli).

**ViewModel:** `DefaultVariantValuesVm.DefaultStock` property'sini kaldır.

**GenerateVariants:** `CreateProductVm.GenerateVariants()` static metodunda `DefaultStock` kullanımı varsa kaldır. Varyant kartlarında stok default 0 (zaten şube bazlı modal'dan giriliyor).

### Doğrulama
- Step 3'te "Varsayılan Stok" input'u görünmüyor
- Varyant oluşturma hala çalışıyor
- Build başarılı (kaldırılan property referansları temizlenmiş)

---

## Fix 3 — ListPrice Opsiyonel, SalePrice Zorunlu

### Sorun
ListPrice ve SalePrice ikisi de zorunlu. Müşteri karıştırıyor.

### Karar
- **ListPrice** = referans/tavsiye fiyat → opsiyonel (null veya >= 0)
- **SalePrice** = gerçek satış fiyatı → zorunlu (> 0)

### Çözüm

**Validator — `AddProductVariantValidator.cs`:**
```csharp
// ÖNCE:
RuleFor(x => x.ListPrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Liste fiyatı boş geçilemez");

// SONRA:
RuleFor(x => x.ListPrice)
    .Must(v => !v.HasValue || v.Value >= 0)
    .WithMessage("Liste fiyatı negatif olamaz.");
```

SalePrice kuralı aynı kalır: `Must(v => v.HasValue && v.Value > 0)`.

Karşılaştırma kuralı (satır 19-21) zaten `!x.ListPrice.HasValue` kontrolü var — ListPrice null ise atlanıyor, değişiklik gerekmez.

**Validator — `EditProductVariantValidator.cs`:**
```csharp
// ÖNCE:
RuleFor(x => x.ListPrice).GreaterThan(0).WithMessage("Liste fiyatı 0'dan büyük olmalıdır.");

// SONRA: Kaldır veya >= 0 yap (edit DTO'su nullable mı kontrol et)
```

**View — `_CreateStep3Variants.cshtml`:**
- ListPrice label'ına "(opsiyonel)" ekle
- Hint text: "Boş bırakılırsa yalnızca satış fiyatı kullanılır"

**DTO — `CreateVariantVm`:**
- `ListPrice` → `decimal?` (nullable yap)

**Mapping (DoSave):**
- `DoSave`'de `v.ListPrice` zaten `AddProductVariantDto.ListPrice`'a (nullable decimal?) atanıyor — uyumlu

**Entity — `ProductVariant.ListPrice`:**
- Entity'de `decimal` (not null) kalacak. DB'ye kaydederken null gelirse 0 olarak set edilecek (business layer'da).

### Doğrulama
- Wizard Step 3: ListPrice boş bırakılabilir, SalePrice boş bırakılınca hata
- ListPrice negatif girilince hata
- Build + birim testler geçer

---

## Fix 4 — "Atla, Kaydet" Exception

### Sorun
Step 6'da "Atla, Kaydet" basılınca `ValidationException`: ListPrice/SalePrice boş.

### Root Cause Analizi
`_CreateStep6Publish.cshtml`'de her iki buton da aynı form'u submit ediyor (`hx-post="/products/add/step6"`). `CreateStep6` action'ı:

1. Session'dan state okur (satır 376-380)
2. SEO + ECommercePrice merge eder
3. `DoSave(vm)` çağırır → `AddProduct` → validator

Session'daki `Variants[].ListPrice` ve `Variants[].SalePrice` Step 3'te girilip Session'a kaydedilmiş olmalı. Eğer Session timeout veya serialize sorunu varsa 0 (default decimal) kalır.

Hidden field'lar da taşıyor (`@v.ListPrice`, `@v.SalePrice`) ama `CreateStep6` bunları kullanmıyor — sadece `ECommercePrice` merge ediyor.

### Çözüm

1. **Fix 3 uygulandığında** ListPrice hatası zaten kalkar (opsiyonel olacak).

2. **Session fallback:** `CreateStep6`'da Session'dan gelen varyant fiyatları 0 ise, hidden field'lardan gelen `formVm.Variants` verilerini fallback olarak kullan:
```csharp
// Merge: Session variant'ında fiyat 0 ise form'daki hidden field değerini kullan
for (int i = 0; i < vm.Variants.Count && i < formVm.Variants.Count; i++)
{
    vm.Variants[i].ECommercePrice = formVm.Variants[i].ECommercePrice;
    
    // Fallback: Session'da fiyat kaybolmuşsa hidden field'dan al
    if (vm.Variants[i].SalePrice == 0 && formVm.Variants[i].SalePrice > 0)
        vm.Variants[i].SalePrice = formVm.Variants[i].SalePrice;
    if (vm.Variants[i].ListPrice == 0 && formVm.Variants[i].ListPrice > 0)
        vm.Variants[i].ListPrice = formVm.Variants[i].ListPrice;
}
```

3. **Barcode hidden field eksik:** `_CreateStep6Publish.cshtml`'deki hidden field'lara `Variants[@i].Barcode` ekle (şu an eksik).

### Doğrulama
- Step 6'da "Atla, Kaydet" → exception yok, ürün kaydedilir
- "Yayınla ve Kaydet" → aynı şekilde çalışır
- Session timeout senaryosu test edilmeli

---

## Fix 5 — Ürün Detay: Mağaza Ayarları

### Entity Değişikliği
**`Product.cs`'e ekle:**
```csharp
public bool IsPublished { get; set; }
```

**Migration:** `AddIsPublishedToProduct` — `Products` tablosuna `IsPublished` (bool, default false) kolonu.

### Yeni ViewModel
**`Features/Products/ViewModels/StoreSettingsVm.cs`:**
```csharp
public class StoreSettingsVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool IsPublished { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public List<VariantStorePriceVm> VariantPrices { get; set; } = [];
}

public class VariantStorePriceVm
{
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = "";
    public decimal SalePrice { get; set; }      // readonly referans
    public decimal ECommercePrice { get; set; }  // düzenlenebilir
}
```

### Yeni Partial View
**`Features/Products/Views/Partials/_StoreSettings.cshtml`:**
- Step 6'daki SEO + mağaza fiyatları bölümünü bu partial'a taşı
- Wizard Step 6 bu partial'ı `<partial name="_StoreSettings" model="..." />` ile kullanır
- Detay sayfası da aynı partial'ı kullanır
- Form action URL'leri partial'a parametre olarak geçilir

### Controller Action'ları
**`ProductController.cs`'e ekle:**

```csharp
[HttpGet("/products/{id:guid}/store-settings")]
public async Task<IActionResult> StoreSettings(Guid id)
// GET — mevcut SEO + ECommercePrice verilerini yükle, partial dön

[HttpPost("/products/{id:guid}/store-settings")]
public async Task<IActionResult> SaveStoreSettings(Guid id, StoreSettingsVm vm)
// POST — SEO + ECommercePrice güncelle, IsPublished değiştirmez

[HttpPost("/products/{id:guid}/store-settings/publish")]
public async Task<IActionResult> PublishToStore(Guid id, StoreSettingsVm vm)
// POST — güncelle + IsPublished = true
```

### Business Layer
**`IProductService`'e ekle:**
- `Task<Result> UpdateStoreSettings(Guid productId, StoreSettingsVm vm)`
- `Task<Result> PublishProduct(Guid productId, StoreSettingsVm vm)`

**Slug unique kontrolü:** `UpdateStoreSettings` ve `PublishProduct` içinde:
```csharp
var existing = await dbContext.Products
    .AnyAsync(p => p.SeoSlug == vm.SeoSlug && p.Id != productId);
if (existing) return Result.Fail("Bu SEO URL zaten başka bir ürün tarafından kullanılıyor.");
```

### Detay Sayfasına Entegrasyon
**`Detail.cshtml`'e ekle (Varyantlar card'ından sonra):**
- "Mağaza Ayarları" card — HTMX lazy load ile `/products/{id}/store-settings` endpoint'inden çekilir
- Card içinde: SEO alanları + varyant bazlı mağaza fiyatları + "Kaydet" / "Kaydet ve Yayınla" butonları
- Üstteki buton grubuna yayın durumu badge'i ekle (yeşil: yayında, gri: taslak)

### Doğrulama
- Detay sayfasında "Mağaza Ayarları" card'ı görünüyor
- SEO alanları mevcut verilerle dolu geliyor
- "Kaydet" → günceller, yayın durumu değişmez
- "Kaydet ve Yayınla" → günceller + IsPublished = true
- Aynı slug ile başka ürün varsa hata mesajı
- Wizard Step 6 hala çalışıyor (_StoreSettings partial'ını kullanarak)

---

## Sıralama ve Bağımlılıklar

```
Fix 2 (stok kaldır)     ─── bağımsız
Fix 3 (ListPrice opt)   ─── Fix 4'e ön koşul
Fix 4 (Atla Kaydet)     ─── Fix 3'e bağımlı
Fix 1 (BackToStep2)     ─── bağımsız
Fix 5 (Mağaza Ayarları) ─── migration gerektirir, en son
```

**Önerilen sıra:** Fix 2 → Fix 3 → Fix 4 → Fix 1 → Fix 5
