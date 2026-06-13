# Category Type-Aware UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kategori detay/düzenleme sayfaları ve ürün kategori seçicisini "Ana Kategori" vs "Yaprak Kategori" ayrımına duyarlı hale getir; xmin optimistic concurrency ile eş zamanlı edit'i engelle.

**Architecture:** Backend'de `CategorySelectDto`'ya `ParentName` eklenir, `GetLeafCategoriesWithParentAsync` yeni metod olarak eklenir, PostgreSQL `xmin` system column `IsConcurrencyToken` olarak `Category` entity'e bağlanır. UI katmanı `IsLeaf` = `SubCategoryCount == 0` türetmesiyle bağlamsal buton/badge gösterir.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core + Npgsql (xmin concurrency), HTMX, Tom Select (optgroup), Tabler UI

---

## Dosya Haritası

| Dosya | İşlem | Açıklama |
|-------|-------|----------|
| `Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs` | Modify | `ParentName` optional param eklenir |
| `Entegrasyon.Business/Abstract/ICategoryManager.cs` | Modify | `GetLeafCategoriesWithParentAsync` imzası |
| `Entegrasyon.Business/Concrete/CategoryManager.cs` | Modify | Metod impl + `UpdateCategory` xmin |
| `Entegrasyon.Entity/Categories/Category.cs` | Modify | `uint RowVersion` property |
| `Entegrasyon.DataAccess/.../CategoryEntityConfiguration.cs` | Modify | xmin concurrency token mapping |
| `Entegrasyon.Entity/Dtos/Category/EditCategoryDto.cs` | Modify | `uint RowVersion` param |
| `Entegrasyon.MVC/Features/Categories/CategoryController.cs` | Modify | Edit POST concurrency catch |
| `Entegrasyon.MVC/Features/Categories/ViewModels/CategoryDetailPageVm.cs` | Modify | `IsLeaf` computed property |
| `Entegrasyon.MVC/Features/Categories/Views/Detail.cshtml` | Modify | Subtitle tip metni + bağlamsal buton |
| `Entegrasyon.MVC/Features/Categories/Views/Edit.cshtml` | Modify | Tip badge + hidden RowVersion field |
| `Entegrasyon.MVC/Features/Categories/Views/Partials/_CreateStep1.cshtml` | Modify | Root kategori ℹ notu |
| `Entegrasyon.MVC/Features/Products/ProductController.cs` | Modify | `LoadCreateDropdowns` → with-parent metod |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml` | Modify | optgroup + `CategorySelectDto` tipi |
| `Entegrasyon.MVC/Features/Products/Views/Edit.cshtml` | Modify | optgroup + `CategorySelectDto` tipi |
| `Test/.../CategoryManagerIntegrationTests.cs` | Modify | Concurrency + leaf-only testler |

---

## Task 1: CategorySelectDto — ParentName ekleme

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs`

- [ ] **Step 1: CategorySelectDto'ya ParentName ekle**

```csharp
namespace Entegrasyon.Entity.Dtos.Category;

public sealed record CategorySelectDto(int Id, string Name, string? ParentName = null);
```

`ParentName = null` default — mevcut tüm `new CategorySelectDto(id, name)` çağrıları bozulmaz.

- [ ] **Step 2: Build et, hata yok mu kontrol et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep -E "error|warning" | head -20
```

Beklenen: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs
git commit -m "feat(category): CategorySelectDto'ya optional ParentName eklendi"
```

---

## Task 2: GetLeafCategoriesWithParentAsync metodu

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ICategoryManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`

- [ ] **Step 1: Integration test yaz (RED)**

`Test/Entegrasyon.IntegrationTest/` altındaki `CategoryManagerIntegrationTests.cs` dosyasını aç, aşağıdaki testi ekle:

```csharp
[Fact]
public async Task GetLeafCategoriesWithParentAsync_ReturnsOnlyLeafCategories_WithParentName()
{
    // Arrange — parent + leaf oluştur
    var parent = new Category { Name = "ParentCat" };
    await DbContext.Categories.AddAsync(parent);
    await DbContext.SaveChangesAsync();

    var leaf = new Category { Name = "LeafCat", SuperCategoryId = parent.Id };
    var orphanParent = new Category { Name = "OrphanLeaf" }; // parent'sız leaf
    await DbContext.Categories.AddRangeAsync(leaf, orphanParent);
    await DbContext.SaveChangesAsync();

    // Act
    var result = await CategoryService.GetLeafCategoriesWithParentAsync();

    // Assert
    result.Should().NotContain(x => x.Id == parent.Id); // parent dışarıda
    var leafDto = result.Should().ContainSingle(x => x.Id == leaf.Id).Subject;
    leafDto.ParentName.Should().Be("ParentCat");
    var orphanDto = result.Should().ContainSingle(x => x.Id == orphanParent.Id).Subject;
    orphanDto.ParentName.Should().BeNull();
}
```

- [ ] **Step 2: Testi çalıştır, RED olduğunu doğrula**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~GetLeafCategoriesWithParentAsync_ReturnsOnlyLeafCategories" \
  -v n 2>&1 | tail -10
```

Beklenen: FAIL — metod tanımlı değil.

- [ ] **Step 3: ICategoryManager'a imza ekle**

`ICategoryManager.cs` dosyasında `GetLeafCategoriesAsync()` satırının altına:

```csharp
Task<List<CategorySelectDto>> GetLeafCategoriesWithParentAsync();
```

- [ ] **Step 4: CategoryManager'a implementasyonu ekle**

`CategoryManager.cs` dosyasında `GetLeafCategoriesAsync()` metodunun hemen altına:

```csharp
public async Task<List<CategorySelectDto>> GetLeafCategoriesWithParentAsync()
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    return await dbContext.Categories
        .AsNoTracking()
        .Where(c => !dbContext.Categories.Any(child => child.SuperCategoryId == c.Id && !child.IsDeleted))
        .OrderBy(c => c.Name)
        .Select(c => new CategorySelectDto(
            c.Id,
            c.Name,
            c.SuperCategory != null ? c.SuperCategory.Name : null))
        .ToListAsync();
}
```

- [ ] **Step 5: Testi çalıştır, GREEN olduğunu doğrula**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~GetLeafCategoriesWithParentAsync_ReturnsOnlyLeafCategories" \
  -v n 2>&1 | tail -10
```

Beklenen: PASS.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ICategoryManager.cs \
        Application/Entegrasyon.Business/Concrete/CategoryManager.cs \
        Test/Entegrasyon.IntegrationTest/
git commit -m "feat(category): GetLeafCategoriesWithParentAsync — parent adıyla leaf listesi"
```

---

## Task 3: Category xmin Optimistic Concurrency

**Files:**
- Modify: `Application/Entegrasyon.Entity/Categories/Category.cs`
- Modify: `Application/Entegrasyon.DataAccess/.../CategoryEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Category/EditCategoryDto.cs`

- [ ] **Step 1: Integration test yaz (RED)**

`CategoryManagerIntegrationTests.cs` dosyasına ekle:

```csharp
[Fact]
public async Task UpdateCategory_ThrowsOrReturnsError_WhenRowVersionStale()
{
    // Arrange
    var cat = new Category { Name = "OriginalName" };
    await DbContext.Categories.AddAsync(cat);
    await DbContext.SaveChangesAsync();

    // İlk form yüklendiğindeki RowVersion (stale simülasyonu için 0 gönderiyoruz)
    var staleRowVersion = 0u;

    var dto = new EditCategoryDto(cat.Id, "UpdatedName", null, false, false, null, staleRowVersion);

    // Act
    var result = await CategoryService.UpdateCategory(dto);

    // Assert — xmin 0 olmayacağından concurrency hatası bekleniyor
    result.Success.Should().BeFalse();
    result.Message.Should().Contain("değiştirildi");
}
```

- [ ] **Step 2: Testi çalıştır, RED olduğunu doğrula**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~UpdateCategory_ThrowsOrReturnsError_WhenRowVersionStale" \
  -v n 2>&1 | tail -10
```

Beklenen: compile error — `EditCategoryDto` henüz `RowVersion` almıyor.

- [ ] **Step 3: Category entity'e RowVersion ekle**

`Application/Entegrasyon.Entity/Categories/Category.cs` dosyasında sınıf gövdesine ekle (diğer property'lerin sonuna):

```csharp
public uint RowVersion { get; set; }
```

- [ ] **Step 4: CategoryEntityConfiguration'a xmin mapping ekle**

`CategoryEntityConfiguration.cs` dosyasında `Configure` metodunun sonuna:

```csharp
builder.Property(x => x.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

- [ ] **Step 5: EditCategoryDto'ya RowVersion ekle**

`EditCategoryDto.cs` dosyasını aşağıdaki gibi güncelle:

```csharp
namespace Entegrasyon.Entity.Dtos.Category;

public sealed record EditCategoryDto(
    int Id,
    string Name,
    int? SuperCategoryId,
    bool IsFavorite,
    bool IsImported,
    decimal? DefaultVatRate = null,
    uint RowVersion = 0
);
```

- [ ] **Step 6: Build et, hata yok mu doğrula**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep -E "error" | head -20
```

Beklenen: 0 error.

- [ ] **Step 7: Testi çalıştır, artık compiles & RED (method içinde henüz xmin check yok)**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~UpdateCategory_ThrowsOrReturnsError_WhenRowVersionStale" \
  -v n 2>&1 | tail -10
```

Beklenen: FAIL (test compile'lanır ama `result.Success` true dönüyor).

- [ ] **Step 8: CategoryManager.UpdateCategory'ye xmin check ekle**

`CategoryManager.cs` dosyasında `UpdateCategory` metodunu bul. `dbCategory` null check'inden sonra, diğer field atamaları ÖNCE şunu ekle:

```csharp
// Optimistic concurrency: formdaki xmin DB'dekiyle eşleşmeli
dbContext.Entry(dbCategory).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;
```

Ve `await dbContext.SaveChangesAsync();` satırını try/catch'e al:

```csharp
try
{
    await dbContext.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    return new ErrorResult("Kategori bu sırada başkası tarafından değiştirildi. Sayfayı yenileyin.");
}
```

`using Microsoft.EntityFrameworkCore;` using'i dosyanın başında zaten mevcut.

- [ ] **Step 9: Testi çalıştır, GREEN olduğunu doğrula**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~UpdateCategory_ThrowsOrReturnsError_WhenRowVersionStale" \
  -v n 2>&1 | tail -10
```

Beklenen: PASS.

- [ ] **Step 10: Tüm integration testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v n 2>&1 | tail -15
```

Beklenen: tüm testler PASS.

- [ ] **Step 11: Commit**

```bash
git add Application/Entegrasyon.Entity/Categories/Category.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/CategoryEntityConfiguration.cs \
        Application/Entegrasyon.Entity/Dtos/Category/EditCategoryDto.cs \
        Application/Entegrasyon.Business/Concrete/CategoryManager.cs \
        Test/Entegrasyon.IntegrationTest/
git commit -m "feat(category): xmin optimistic concurrency — eş zamanlı edit koruması"
```

---

## Task 4: CategoryController Edit POST — concurrency hata yönetimi + RowVersion

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Categories/CategoryController.cs`

- [ ] **Step 1: Edit POST metoduna rowVersion parametresi ekle**

`CategoryController.cs` dosyasında `Edit` POST action imzasını bul:

```csharp
[HttpPost("/categories/{id:int}/edit")]
public async Task<IActionResult> Edit(int id, [FromForm] string name, [FromForm] int? superCategoryId,
    [FromForm] bool isFavorite, [FromForm] decimal? defaultVatRate, [FromForm] bool isImported)
```

Şu hale getir (sadece imza değişiyor):

```csharp
[HttpPost("/categories/{id:int}/edit")]
public async Task<IActionResult> Edit(int id, [FromForm] string name, [FromForm] int? superCategoryId,
    [FromForm] bool isFavorite, [FromForm] decimal? defaultVatRate, [FromForm] bool isImported,
    [FromForm] uint rowVersion = 0)
```

- [ ] **Step 2: EditCategoryDto oluştururken rowVersion geçir**

Aynı metodun içinde `EditCategoryDto` oluşturma satırını bul:

```csharp
var dto = new EditCategoryDto(id, name, superCategoryId, isFavorite, isImported, defaultVatRate);
```

Şu hale getir:

```csharp
var dto = new EditCategoryDto(id, name, superCategoryId, isFavorite, isImported, defaultVatRate, rowVersion);
```

- [ ] **Step 3: Concurrency hata durumunu handle et**

`result.Success` check bloğunu bul:

```csharp
if (result.Success)
    TempData.SetSuccess("Kategori basariyla guncellendi.");
else
    TempData.SetError(result.Message ?? "Kategori guncellenemedi.");

return RedirectToAction(nameof(Index));
```

Şu hale getir:

```csharp
if (result.Success)
{
    TempData.SetSuccess("Kategori başarıyla güncellendi.");
    return RedirectToAction(nameof(Index));
}

// Concurrency hatası → edit sayfasına dön (kullanıcı formu yenileyerek tekrar dener)
TempData.SetError(result.Message ?? "Kategori güncellenemedi.");
return RedirectToAction(nameof(Edit), new { id });
```

- [ ] **Step 4: Build et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -10
```

Beklenen: 0 error.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Categories/CategoryController.cs
git commit -m "feat(category): edit POST rowVersion + concurrency hata yönetimi"
```

---

## Task 5: Detail sayfası — IsLeaf + bağlamsal buton/subtitle

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Categories/ViewModels/CategoryDetailPageVm.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Categories/Views/Detail.cshtml`

- [ ] **Step 1: CategoryDetailPageVm'e IsLeaf ekle**

`CategoryDetailPageVm.cs` dosyasını aç. Mevcut property'lerin sonuna computed property ekle:

```csharp
public bool IsLeaf => SubCategoryCount == 0;
```

- [ ] **Step 2: Detail.cshtml subtitle güncellemesi**

`Detail.cshtml` dosyasında `subtitleText` değişkenini bulup şu şekilde güncelle:

```csharp
var isLeaf = Model.IsLeaf;
var subtitleText = isLeaf
    ? (!string.IsNullOrEmpty(Model.SuperCategoryName)
        ? $"{Model.SuperCategoryName} · Yaprak kategori"
        : "Yaprak kategori")
    : (!string.IsNullOrEmpty(Model.SuperCategoryName)
        ? $"{Model.SuperCategoryName} · Ana kategori · {Model.SubCategoryCount} alt kategori"
        : $"Ana kategori · {Model.SubCategoryCount} alt kategori");
ViewData.SetPageSubtitle(subtitleText);
```

- [ ] **Step 3: PageActions bölümünde "Özellikleri Düzenle" butonunu koşullu yap**

`Detail.cshtml` dosyasında `@section PageActions` bloğunda dropdown menüsü içindeki "Özellikleri Düzenle" satırını bul:

```html
<a class="dropdown-item" href="/categories/@Model.CategoryId/attributes">
    <i class="ti ti-list-details dropdown-item-icon"></i> Özellikleri Düzenle
</a>
```

Şu hale getir:

```html
@if (Model.IsLeaf)
{
    <a class="dropdown-item" href="/categories/@Model.CategoryId/attributes">
        <i class="ti ti-list-details dropdown-item-icon"></i> Özellikleri Düzenle
    </a>
}
```

- [ ] **Step 4: Sağ panel Özellikler kartındaki "Özellikleri Düzenle" butonunu da koşullu yap**

`Detail.cshtml` dosyasında sağ paneldeki `card-actions` içindeki "Özellikleri Düzenle" butonunu bul:

```html
<a href="/categories/@Model.CategoryId/attributes" class="btn btn-primary btn-sm">
    <i class="ti ti-list-details me-1"></i> Özellikleri Düzenle
</a>
```

Şu hale getir:

```html
@if (Model.IsLeaf)
{
    <a href="/categories/@Model.CategoryId/attributes" class="btn btn-primary btn-sm">
        <i class="ti ti-list-details me-1"></i> Özellikleri Düzenle
    </a>
}
```

- [ ] **Step 5: Build et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -10
```

Beklenen: 0 error.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Categories/ViewModels/CategoryDetailPageVm.cs \
        Application/Entegrasyon.MVC/Features/Categories/Views/Detail.cshtml
git commit -m "feat(category): detail sayfası tip-duyarlı subtitle + bağlamsal Özellikleri Düzenle butonu"
```

---

## Task 6: Edit sayfası — tip badge + hidden RowVersion

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Categories/Views/Edit.cshtml`

`CategoryEditPageDto` zaten `IsLeaf` ve `Category.RowVersion` içeriyor (`GetCategoryEditPageData` bunu doldurur; `Category` entity'e RowVersion Task 3'te eklendi).

- [ ] **Step 1: Edit.cshtml'de tip badge ekle**

`Edit.cshtml` dosyasında `@section PageActions` bloğunu bul. Mevcut "Detaya Dön" butonunun **önüne** badge ekle:

```html
@section PageActions {
    @if (Model.IsLeaf)
    {
        <span class="badge bg-green-lt me-2">
            <i class="ti ti-leaf me-1"></i> Yaprak Kategori
        </span>
    }
    else
    {
        <span class="badge bg-azure-lt me-2">
            <i class="ti ti-folder me-1"></i> Ana Kategori
        </span>
    }
    <a href="/categories/@Model.Category.Id" class="btn btn-outline-secondary">
        <i class="ti ti-arrow-left icon"></i> Detaya Dön
    </a>
}
```

- [ ] **Step 2: Form'a hidden RowVersion field ekle**

`Edit.cshtml` dosyasında form içindeki `@Html.AntiForgeryToken()` satırından hemen sonra:

```html
<input type="hidden" name="RowVersion" value="@Model.Category.RowVersion" />
```

- [ ] **Step 3: Build et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -10
```

Beklenen: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Categories/Views/Edit.cshtml
git commit -m "feat(category): edit sayfası tip badge + xmin hidden RowVersion field"
```

---

## Task 7: Create wizard — root kategori seçilince bilgi notu

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Categories/Views/Partials/_CreateStep1.cshtml`

- [ ] **Step 1: Root kategori seçilince gösterilecek ℹ notu ekle**

`_CreateStep1.cshtml` dosyasında üst kategori `<select>` bloğunu bul. Mevcut:

```html
<div class="col-md-6 mb-3">
    <label class="form-label">Üst Kategori</label>
    <select id="parent-category-select" name="SuperCategoryId" class="form-select">
        ...
    </select>
</div>
```

Şu hale getir:

```html
<div class="col-md-6 mb-3">
    <label class="form-label">Üst Kategori</label>
    <select id="parent-category-select" name="SuperCategoryId" class="form-select">
        <option value="">— Üst kategori yok (kök kategori) —</option>
        @foreach (var parent in parents.OrderBy(p => p.Name))
        {
            <option value="@parent.Id"
                    selected="@(parent.Id == Model.SuperCategoryId)">
                @parent.Name
            </option>
        }
    </select>
    <div id="root-category-note" class="alert alert-info mt-2 py-2 small d-none">
        <i class="ti ti-info-circle me-1"></i>
        Kök kategoriler <strong>özellik içeremez</strong>; ürünler alt kategorilere gider.
    </div>
</div>
```

- [ ] **Step 2: JavaScript'e root seçim toggle'ı ekle**

`_CreateStep1.cshtml` dosyasındaki `<script>` bloğunda `TomSelect` init sonrasına ekle:

```javascript
var rootNote = document.getElementById('root-category-note');
function toggleRootNote() {
    var val = el.value || (el.tomselect ? el.tomselect.getValue() : '');
    if (rootNote) rootNote.classList.toggle('d-none', val !== '');
}
// Tom Select value change event
if (el && el.tomselect) {
    el.tomselect.on('change', toggleRootNote);
}
toggleRootNote(); // Sayfa yüklendiğinde mevcut değere göre
```

- [ ] **Step 3: Build et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -10
```

Beklenen: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Categories/Views/Partials/_CreateStep1.cshtml
git commit -m "feat(category): wizard adım1 root kategori seçilince bilgi notu"
```

---

## Task 8: Ürün kategori seçici — leaf + optgroup

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Edit.cshtml`

### 8a. ProductController — LoadCreateDropdowns güncelle

- [ ] **Step 1: LoadCreateDropdowns metodunu güncelle**

`ProductController.cs` dosyasında `LoadCreateDropdowns` private metodunu bul:

```csharp
var categories = await categoryService.GetLeafCategoriesAsync();
ViewBag.Categories = categories;
```

Şu hale getir:

```csharp
var categories = await categoryService.GetLeafCategoriesWithParentAsync();
ViewBag.Categories = categories;
```

- [ ] **Step 2: Build et (tip hatasını bul)**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -20
```

`_CreateStep1.cshtml` view'ının `List<Category>` cast'i bozulacak — sonraki adımda düzelteceğiz.

### 8b. Products _CreateStep1.cshtml — optgroup

- [ ] **Step 3: _CreateStep1.cshtml kategori select'i optgroup'a çevir**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml` dosyasında başındaki cast satırını güncelle:

```csharp
var categories = ViewBag.Categories as List<Entegrasyon.Entity.Dtos.Category.CategorySelectDto> ?? [];
```

Sonra kategori `<select>` bloğunu bul (aşağıdaki gibi görünür):

```html
<select name="CategoryId" id="category-select" ...>
    <option value="">Kategori seçin...</option>
    @foreach (var c in categories)
    {
        <option value="@c.Id" selected="@(Model.CategoryId == c.Id)">@c.Name</option>
    }
</select>
```

Şu hale getir:

```html
<select name="CategoryId" id="category-select" class="@(ViewData.ModelState["CategoryId"]?.Errors.Count > 0 ? "is-invalid" : "")" required>
    <option value="">Kategori seçin...</option>
    @foreach (var group in categories.GroupBy(c => c.ParentName ?? "Diğer").OrderBy(g => g.Key))
    {
        <optgroup label="@group.Key">
            @foreach (var c in group.OrderBy(c => c.Name))
            {
                <option value="@c.Id" selected="@(Model.CategoryId == c.Id)">@c.Name</option>
            }
        </optgroup>
    }
</select>
```

### 8c. Products Edit.cshtml — optgroup

- [ ] **Step 4: ProductEditPageDto.LeafCategories tipini kontrol et ve optgroup ekle**

`Application/Entegrasyon.Entity/Dtos/Product/ProductEditPageDto.cs` dosyasını aç. `LeafCategories` property'si şu halde:

```csharp
List<CategorySelectDto> LeafCategories,
```

Bu zaten `CategorySelectDto` — `ParentName` Task 1'de eklendi, hiçbir şey değişmez. ProductController'daki Edit action'ın `LeafCategories`'i nasıl doldurduğunu bul ve `GetLeafCategoriesWithParentAsync` kullandığından emin ol. Eğer hâlâ `GetLeafCategoriesAsync()` ile dolduruyorsa:

```csharp
// ProductController.cs içindeki Edit action'da — LeafCategories oluşturma yerini bul
// List<Category>'den CategorySelectDto'ya çeviren map varsa aşağıdaki ile değiştir:
var leafCategories = await categoryService.GetLeafCategoriesWithParentAsync();
// ProductEditPageDto(..., LeafCategories: leafCategories, ...)
```

`Edit.cshtml` (Products) dosyasında kategori `<select>` bloğunu bul:

```html
<select class="form-select" name="CategoryId" required>
    @foreach (var cat in Model.LeafCategories)
    {
        <option value="@cat.Id" selected="@(cat.Id == Model.Product.CategoryId)">
            @cat.Name
        </option>
    }
</select>
```

Şu hale getir:

```html
<select id="product-edit-category-select" class="form-select" name="CategoryId" required>
    @foreach (var group in Model.LeafCategories
        .GroupBy(c => c.ParentName ?? "Diğer")
        .OrderBy(g => g.Key))
    {
        <optgroup label="@group.Key">
            @foreach (var cat in group.OrderBy(c => c.Name))
            {
                <option value="@cat.Id" selected="@(cat.Id == Model.Product.CategoryId)">
                    @cat.Name
                </option>
            }
        </optgroup>
    }
</select>
```

Tom Select `Edit.cshtml`'de henüz init edilmiyorsa, sayfanın `<script>` bloğuna ya da script section'ına ekle:

```javascript
(function initProductEditCatSelect() {
    if (typeof TomSelect === 'undefined') { setTimeout(initProductEditCatSelect, 50); return; }
    var el = document.getElementById('product-edit-category-select');
    if (el && !el.tomselect) {
        new TomSelect(el, { maxOptions: null });
    }
})();
```

- [ ] **Step 5: Build et**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep "error" | head -20
```

Beklenen: 0 error.

- [ ] **Step 6: Tüm testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n 2>&1 | tail -5
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v n 2>&1 | tail -5
```

Beklenen: tümü PASS.

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ProductController.cs \
        Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml \
        Application/Entegrasyon.MVC/Features/Products/Views/Edit.cshtml
git commit -m "feat(product): kategori seçici leaf-only + üst kategoriye göre optgroup"
```

---

## Task 9: Son doğrulama

- [ ] **Step 1: Tüm testler yeşil**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --no-build -v n 2>&1 | tail -5
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --no-build -v n 2>&1 | tail -5
```

- [ ] **Step 2: Build temiz**

```bash
dotnet build Entegrasyon.sln -nologo -v q 2>&1 | grep -c "error" && echo "Hata yok"
```

- [ ] **Step 3: Dev'e push**

```bash
git push origin develop
```

---

## Spec Kapsamı Kontrolü

| Gereksinim | Task |
|-----------|------|
| Detail: subtitle'da tip metni | Task 5 |
| Detail: "Özellikleri Düzenle" sadece leaf'te | Task 5 |
| Edit: tip badge | Task 6 |
| Edit: parent dropdown sadece geçerli adaylar | ✅ Zaten mevcut (`GetCategoryEditPageData`) |
| Edit: xmin optimistic concurrency | Task 3 + 4 + 6 |
| Create wizard: root kategori notu | Task 7 |
| Ürün kategori seçici: sadece leaf | Task 8 |
| Ürün kategori seçici: optgroup | Task 8 |
