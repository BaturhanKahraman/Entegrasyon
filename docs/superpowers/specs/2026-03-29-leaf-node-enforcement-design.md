# Leaf Node Enforcement — Kategori Yaprak Kısıtlaması

**Tarih:** 2026-03-29
**Durum:** Onaylandı
**Öncelik:** Medium

## Amaç

Tüm kategori eşleştirmeleri (marketplace sync) ve attribute atamaları yalnızca **leaf (yaprak) kategorilerde** yapılabilsin. Leaf olmayan kategoriler (çocuğu olanlar) asla attribute veya sync alamasın; attribute veya sync'i olan kategoriler ise asla üst kategori olarak seçilemesin.

## Leaf Node Tanımı

Bir kategori **leaf**'tir ancak ve ancak `SubCategories` koleksiyonu boşsa (çocuğu yoksa).

**Sonuç kuralları:**
- Leaf kategoriye attribute eklenebilir, marketplace sync yapılabilir
- Attribute veya sync'i olan kategori **parent olarak seçilemez** (altına çocuk eklenemez)
- Çocuğu olan kategoriye (non-leaf) attribute eklenemez, sync yapılamaz

## Yaklaşım

**Business Layer Guard + UI Filter** — Merkezi koruma business katmanında, UI'da filtreleme. API üzerinden de korumalı, sadece UI'a güvenmez. Mevcut pattern'e uygun (validation → business rules → execution).

## Merkezi Guard Metodu

`CategoryManager`'daki mevcut `GetValidParentCandidatesAsync` güncellenir:

```csharp
// Mevcut filtre (attribute kontrolü)
.Where(c => !c.CategoryAttributes.Any())

// Eklenen filtre (sync kontrolü)
.Where(c => !c.MarketplaceLinks.Any(m => m.IsActive))
```

Ek olarak `IsLeafCategoryAsync(int categoryId)` metodu eklenir — çocuğu olup olmadığını kontrol eder.

## Guard Uygulanacak Noktalar

### A. Kategori Ekleme/Düzenleme (`CategoryManager`)

| İşlem | Guard |
|-------|-------|
| `AddCategory` — parent seçimi | Parent'ın attribute'u veya sync'i varsa → hata |
| `UpdateCategory` — parent değiştirme | Aynı guard |

### B. Attribute Ekleme (`CategoryAttributeCategoryManager`)

| İşlem | Guard |
|-------|-------|
| `AddAttributeToCategory` | Kategori leaf değilse (çocuğu varsa) → hata |
| `BulkAddAttributes` | Aynı guard |

### C. Marketplace Eşleştirme (`CategoryMatchService`)

| İşlem | Guard |
|-------|-------|
| `CreateCategoryMappingAsync` | Kategori leaf değilse → hata |
| `BulkCreateCategoryMappingsAsync` | Her mapping için leaf kontrolü |
| `ApplyTemplateAsync` | Template item'daki kategori leaf değilse → skip + uyarı |

### D. Auto Match (`CategoryAutoMatchService`)

| İşlem | Guard |
|-------|-------|
| `GetAutoMatchSuggestionsAsync` | Input listesini leaf kategorilere filtrele |

### E. UI Filtreleme

| Sayfa | Değişiklik |
|-------|-----------|
| `CategoryDialog` — parent dropdown | `GetValidParentCandidatesAsync` (sync filtresi eklendi) |
| `BulkCategoryMatchPage` — kategori listesi | Sadece leaf kategoriler gösterilecek |
| `CategoryTreePanel` — seçim | Non-leaf'lere eşleştirme işlemi disabled |
| `CategorySync` sayfası | Sadece leaf kategoriler listelenecek |
| Kategori detay paneli | Non-leaf ise attribute bölümü gizli + bilgi mesajı |

## Hata Mesajları (Türkçe)

| Guard noktası | Hata mesajı |
|---------------|-------------|
| Parent'a attribute/sync varken çocuk ekleme | "Seçilen üst kategori özellik veya pazar yeri eşleştirmesi içerdiğinden alt kategori eklenemez." |
| Non-leaf'e attribute ekleme | "Bu kategorinin alt kategorileri olduğu için özellik eklenemez." |
| Non-leaf'e sync ekleme | "Bu kategorinin alt kategorileri olduğu için pazar yeri eşleştirmesi yapılamaz." |
| Template apply — non-leaf skip | Log + summary'de "X kategori yaprak olmadığı için atlandı" |

## UI Açıklama Metinleri

- Parent dropdown yardım metni: "Yalnızca özellik veya eşleştirme içermeyen kategoriler üst kategori olabilir."
- Sync sayfasında non-leaf tooltip: "Alt kategorileri olan kategoriler eşleştirilemez"

## Mevcut Veri Migasyonu

Veritabanında 4 ihlal eden kategori mevcut:

### Attribute ihlalleri
- **"İlk Kategori"** (id=1, 4 çocuk, 2 attr) → attribute'lar 4 çocuğuna kopyalanacak, parent'tan silinecek
- **"Gömlek"** (id=10, 4 çocuk, 3 attr) → attribute'lar 4 çocuğuna kopyalanacak, parent'tan silinecek

### Sync ihlalleri
- **"Akıllı Telefon"** (id=35, 4 çocuk, 1 sync) → sync kaydı parent'tan silinecek (marketplace kategori ID'si farklı olabilir, çocuklar için kullanıcı yeniden eşleştirecek)
- **"Akıllı Saat"** (id=37, 4 çocuk, 1 sync) → aynı şekilde parent'tan silinecek

### Attribute taşıma stratejisi
1. Parent'ın `CategoryAttributeCategory` kayıtlarını oku (IsRequired, IsVarianter, IsSlicer dahil)
2. Her leaf çocuk için: eğer o attribute zaten yoksa, aynı konfigürasyonla ekle
3. Parent'tan attribute'ları sil

### Sync taşıma stratejisi
- Sync kayıtları marketplace-specific kategori ID'si içerdiğinden çocuklara birebir kopyalanamaz
- Parent'tan silinecek, çocuklar için kullanıcı yeniden eşleştirecek

### Uygulama
EF Core migration içinde SQL ile tek seferlik data fix yapılacak.

## Etkilenen Dosyalar

| Dosya | Değişiklik |
|-------|-----------|
| `CategoryManager.cs` | `IsLeafCategoryAsync`, `GetValidParentCandidatesAsync` güncelleme, `AddCategory`/`UpdateCategory` guard |
| `ICategoryManager.cs` | `IsLeafCategoryAsync` interface'e ekleme |
| `CategoryAttributeCategoryManager.cs` | Leaf guard ekleme |
| `CategoryMatchService.cs` | `CreateCategoryMappingAsync`, `BulkCreateCategoryMappingsAsync`, `ApplyTemplateAsync` guard |
| `CategoryAutoMatchService.cs` | Input filtreleme |
| `CategoryDialog.razor` | Parent dropdown yardım metni güncelleme |
| `CategoryDialog.razor.cs` | Mevcut — `GetValidParentCandidatesAsync` zaten çağrılıyor |
| `BulkCategoryMatchPage.razor.cs` | Leaf filtresi |
| `CategorySync.razor` / `.razor.cs` | Leaf filtresi + tooltip |
| `CategoryDetailsPanel.razor` | Non-leaf uyarı mesajı |
| Migration dosyası | Data fix SQL |

## Test Stratejisi

### Unit testler
- Leaf kategoriye attribute ekleme → başarılı
- Non-leaf kategoriye attribute ekleme → hata
- Leaf kategoriye sync ekleme → başarılı
- Non-leaf kategoriye sync ekleme → hata
- Attribute'u olan kategoriyi parent olarak seçme → hata
- Sync'i olan kategoriyi parent olarak seçme → hata
- `GetValidParentCandidatesAsync` — sync'li kategorilerin filtrelenmesi

### Integration testler
- Data migration sonrası ihlal eden kayıt kalmadığını doğrula
- End-to-end kategori ekleme + attribute + sync akışı
