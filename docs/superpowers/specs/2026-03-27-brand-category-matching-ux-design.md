# Marka & Kategori Marketplace Eşleştirme UX Tasarımı

**Tarih:** 2026-03-27
**Durum:** Onaylandı

---

## Özet

İki ilişkili iyileştirme:

1. **Marka eşleştirme sayfası:** Mevcut modal tabanlı tek tek eşleştirme yerine, Master-Detail layout ile tüm marketplace eşleştirmelerini tek ekranda yönetme
2. **Kategori eşleştirme tablosu:** Sadece son eşleştirmeyi göstermek yerine, tüm marketplace eşleştirmelerini chip olarak gösterme

---

## Görev 1: Kategori Eşleştirme Tablosu Düzeltmesi

### Sorun
`CategorySync.razor` DataGrid'inde kategori satırları sadece son eşleştirilen marketplace'i gösteriyor. Kullanıcı, bir kategorinin kaç marketplace ile eşleştiğini veya hangilerinin eksik olduğunu göremez.

### Çözüm
Marketplace Detayları kolonunda tüm eşleştirilmiş marketplace'leri chip/badge olarak göster:
- Her marketplace için küçük chip (logo/renk + isim)
- Hiç eşleşme yoksa "Eşleştirilmemiş" uyarı badge'i
- Birden fazla eşleşme varsa yan yana chip'ler: `Trendyol | Hepsiburada | N11`
- Chip'lerin yanında toplam sayı

### Etkilenen Dosyalar
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor` — DataGrid kolonunu güncelle

---

## Görev 2: Marka Eşleştirme Master-Detail Sayfası

### Yaklaşım
Master-Detail layout — `AttributesPage` pattern'ini takip eder. Sol tarafta marka listesi, sağ tarafta seçili markanın tüm marketplace eşleştirmeleri.

### Sol Panel: Marka Listesi

- **Arama:** Text input ile marka adı araması
- **Filtreler:** Chip tabanlı — "Tümü", "Eksik Eşleştirme", "Tamamlanan"
- **Liste öğesi:** Her satırda:
  - Marka adı
  - Marketplace durum noktaları (yeşil = eşleşti, gri = bekliyor) — her marketplace için küçük renkli dot
- **Seçili marka:** Vurgulanmış arka plan
- **Sayfalama:** DataGrid pager (200+ marka desteği)
- **Özet kartlar:** Sayfanın en üstünde mevcut summary cards korunur (Toplam, Eşleşen, Eksik, %)

### Sağ Panel: Detay Paneli

- **Başlık:** Marka adı + durum badge'i ("2/5 Eşleşti")
- **Marketplace satırları:** Sistemdeki her aktif marketplace için bir satır:
  - Marketplace logosu/adı (sol, sabit genişlik)
  - Eşleşme durumu:
    - Eşleşmişse: Yeşil arka plan + marketplace marka adı + ID + silme butonu
    - Eşleşmemişse: Autocomplete arama input'u + kaydet butonu
  - Autocomplete `IMarketplaceSearchService.SearchBrandsAsync()` kullanır
- **Bilgi notu:** Panelin altında rehber satırı
- **Boş state:** "Soldaki listeden bir marka seçin" mesajı

### Teknik Mimari

**Entity/DB — değişiklik yok:**
- `BrandMarketPlaceMatch` composite key `(MarketPlaceId, ApplicationBrandId)` kalıyor
- `CategoryMarketplace` composite key `(CategoryId, MarketPlaceId)` kalıyor

**Business Layer:**
- Mevcut servisler yeterli: `IBrandMatchService`, `IMarketplaceSearchService`, `IMarketPlaceManager`
- Yeni metod: `GetBrandMappingsByBrandIdAsync(int brandId)` — tek marka için tüm marketplace eşleştirmelerini döner

**Blazor — yeni/değişen dosyalar:**
- `Features/MarketplaceSync/BrandMappingPage.razor(.cs)` — master-detail olarak yeniden yazılır
- `Features/MarketplaceSync/BrandMappingDetailPanel.razor(.cs)` — sağ panel (yeni)
- `Features/MarketplaceSync/BrandMappingListPanel.razor(.cs)` — sol liste (yeni)
- `Features/MarketplaceSync/CategorySync.razor` — marketplace chip'leri düzeltmesi

### Veri Akışı

1. Sayfa yüklendiğinde → tüm markalar + mevcut eşleştirmeler çekilir
2. Sol listeden marka seçilir → sağ panel o markanın eşleştirmelerini gösterir
3. Autocomplete'den marketplace markası seçilir → `CreateBrandMappingAsync` çağrılır → UI güncellenir
4. Silme butonuna basılır → `RemoveBrandMappingAsync` çağrılır → UI güncellenir

### Test Stratejisi

- **Unit test:** `GetBrandMappingsByBrandIdAsync` yeni metodu
- **Integration test:** Eşleştirme CRUD akışı (oluştur, oku, sil)
- Mevcut testler korunur
