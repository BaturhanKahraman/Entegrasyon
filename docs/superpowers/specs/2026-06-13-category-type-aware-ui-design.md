# Kategori Tip-Duyarlı UI Tasarımı

**Tarih:** 2026-06-13  
**Kapsam:** Category detail/edit sayfaları + ürün ekleme/düzenleme kategori seçici

---

## Problem

Mevcut UI, "Ana Kategori" (parent/super) ile "Yaprak Kategori" (leaf/child) arasındaki farkı kullanıcıya iletmiyor. Sonuçlar:

- Detail sayfasında her kategoride "Özellikleri Düzenle" butonu görünür (parent'ta anlamsız)
- Edit formunda tüm kategoriler parent dropdown'a girer (özellikli kategoriler parent olamaz ama UI bunu söylemez)
- Ürün ekleme wizard'ında parent kategoriler de seçilebilir (ürün sadece leaf'e gitmeli)
- Eş zamanlı iki sekme aynı kategoriyi hem leaf hem parent yapabilir (xmin eksik)

---

## Tanımlar

| Tip | Kural |
|-----|-------|
| **Ana Kategori (Parent)** | En az 1 alt kategorisi var. Özellik bağlanamaz. Ürün içeremez. |
| **Yaprak Kategori (Leaf)** | Alt kategorisi yok. Özellik bağlanabilir. Ürün içerir. |

Kurallar business layer'da `AddCategory` / `UpdateCategory` içinde zaten enforce ediliyor. Bu spec sadece **UI katmanını** kural-duyarlı hale getirir.

`IsLeaf` = `!Categories.Any(c => c.SuperCategoryId == id && !c.IsDeleted)` — `GetCategoryEditPageData` zaten hesaplar.

---

## Tasarım Kararları

### 1. Detail Sayfası — Tip Gösterimi (C yaklaşımı)

**Subtitle'da tip metni:**
- Leaf: `"{ÜstKategori} · Yaprak kategori"`
- Parent (kök): `"Ana kategori · {N} alt kategori"`
- Parent (nested): `"{ÜstKategori} · Ana kategori · {N} alt kategori"`

**Bağlamsal butonlar (PageActions):**
- Leaf: "Özellikleri Düzenle" butonu **görünür**
- Parent: "Özellikleri Düzenle" butonu **gizlenir**

`CategoryDetailPageVm`'e `IsLeaf` ve `SubCategoryCount` zaten var (`SubCategoryCount == 0` → leaf).

---

### 2. Edit Sayfası

**Tip badge:** Form başlığına `IsLeaf ? "Yaprak Kategori" : "Ana Kategori"` badge eklenir.

**Üst kategori dropdown:** `GetValidParentCandidatesAsync(excludeId: id)` zaten kullanılıyor (`GetCategoryEditPageData` içinde). Sadece geçerli parent adayları listelenir — özelliği olan veya aktif marketplace sync'i olan kategoriler çıkar.

**"Ana Kategoriye Dönüştür" aksiyonu:** Yok. Alt kategori eklemek istenince server business rule hatası verir.

**Optimistic Concurrency — PostgreSQL `xmin`:**

`xmin`, PostgreSQL'in her satır için tuttuğu transaction id'dir. Satır her güncellendiğinde otomatik değişir. Migration gerektirmez.

```csharp
// Category entity
public uint RowVersion { get; set; }
```

```csharp
// CategoryEntityConfiguration
builder.Property(x => x.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

```csharp
// EditCategoryDto — yeni alan
public record EditCategoryDto(int Id, string Name, int? SuperCategoryId,
    bool IsFavorite, bool IsImported, decimal? DefaultVatRate,
    uint RowVersion);  // ← eklendi
```

```csharp
// CategoryManager.UpdateCategory — xmin kontrolü
dbCategory.RowVersion = dto.RowVersion;  // EF, WHERE xmin=? üretir
// DbUpdateConcurrencyException → "Kategori bu sırada başkası tarafından değiştirildi..."
```

Edit form'a hidden field:
```html
<input type="hidden" name="RowVersion" value="@Model.Category.RowVersion" />
```

Controller'da `DbUpdateConcurrencyException` yakalanır → `TempData.SetError(...)` + `RedirectToAction(nameof(Edit), new { id })`.

---

### 3. Create Wizard — Adım 1

Üst kategori dropdown değişmez (`GetValidParentCandidatesAsync` zaten filtreliyor).

**Eklenen not:** "Üst kategori yok" seçilince dropdown altında:

> ℹ Bu kategori kök düzeyde olacak. Özellik bağlanamaz; ürünler alt kategorilere gider.

**Leaf ise:** "Üst kategori var" seçilince not gizlenir.

---

### 4. Ürün Ekleme/Düzenleme — Kategori Seçici

**Kural:** Sadece yaprak kategoriler (`GetLeafCategoriesAsync`).

**Görünüm:** Tom Select + optgroup — üst kategoriye göre gruplu.

Razor:
```html
<select name="CategoryId" id="category-select">
  <option value="">Kategori seçin...</option>
  @foreach (var group in categories.GroupBy(c => c.SuperCategoryName ?? "Diğer"))
  {
    <optgroup label="@group.Key">
      @foreach (var c in group)
      {
        <option value="@c.Id" selected="@(Model.CategoryId == c.Id)">@c.Name</option>
      }
    </optgroup>
  }
</select>
```

Tom Select optgroup'ları destekler — ek JS gerekmez.

**Controller değişikliği:**
- `ProductController.Create()`: `ViewBag.Categories = await categoryService.GetLeafCategoriesAsync()` (şu an `GetAllCategoriesWithoutAttributesAsync` veya benzeri kullanılıyor)
- `ProductController.Edit()`: `Model.LeafCategories` zaten var — değişmez

**DTO:** `ProductEditPageDto.LeafCategories` zaten `List<Category>` — `SuperCategoryName` için join gerekebilir ya da client-side group yeterli (category name'den çıkarılamaz, `SuperCategoryId`'den ilişki kurulur).

Tercih: `CategorySelectWithParentDto` → `{ int Id, string Name, string? ParentName }` — lightweight projection.

---

## Etkilenen Dosyalar

| Dosya | Değişiklik |
|-------|-----------|
| `Category.cs` | `RowVersion` property eklendi |
| `CategoryEntityConfiguration.cs` | xmin concurrency token mapping |
| `EditCategoryDto.cs` | `RowVersion` parametresi |
| `CategoryManager.UpdateCategory()` | RowVersion set + exception handling |
| `CategoryController.Edit()` | ConcurrencyException catch |
| `CategoryController.Create()` | Products için LeafCategories |
| `CategoryDetailPageVm.cs` | `IsLeaf` türetilecek (SubCategoryCount == 0) |
| `Detail.cshtml` | Subtitle tip metni, bağlamsal buton |
| `Edit.cshtml` | Tip badge, hidden RowVersion field |
| `_CreateStep1.cshtml` (Categories) | Root seçince ℹ notu |
| `_CreateStep1.cshtml` (Products) | LeafCategories + optgroup |
| `Edit.cshtml` (Products) | Optgroup (LeafCategories zaten var) |
| `ProductController` | `GetLeafCategoriesAsync` kullanımı |
| `CategorySelectWithParentDto.cs` | Yeni lightweight DTO |

---

## Test Kapsamı

- **Unit:** `CategoryManager.UpdateCategory` — xmin mismatch → `DbUpdateConcurrencyException`
- **Integration:** Edit form concurrent submit → 2. istek uygun hata döner
- **Integration:** `GetLeafCategoriesAsync` → parent kategoriler listelenmez
- **Unit:** `GetValidParentCandidatesAsync` → özellikli kategori parent adayı değil

---

## Kapsam Dışı

- "Ana Kategoriye Dönüştür" sihirbazı — scope dışı, server hata verir
- SEO slug otomasyonu — ayrı ticket
- Kategori sıralaması / drag-drop — ayrı ticket
