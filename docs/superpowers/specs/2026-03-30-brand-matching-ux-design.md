# Brand Matching UX — Arama + Auto-Match + Duplicate Prevention

**Tarih:** 2026-03-30
**Durum:** Onaylandi
**Oncelik:** Medium

## Amac

Mevcut brand matching sayfasini calistir hale getirmek: SearchBrandsAsync implement etmek, string matching + Ollama ile auto-match eklemek, duplicate prevention ile kullanici deneyimini iyilestirmek.

---

## Parca 1: SearchBrandsAsync Implementasyonu

### Trendyol (Gercek Zamanli API)

`TrendyolMarketplaceSearchService.SearchBrandsAsync` implement edilecek.

Trendyol API: `GET /brands/by-name?name={query}`

Response'u `List<MarketplaceBrandSearchResult>` olarak doner.

### Diger Marketplace'ler (DB Fallback)

Import sirasinda marketplace markalari `BrandMarketPlaceMatch` tablosuna kaydediliyor. Diger marketplace'ler icin DB'den `LIKE` filtresiyle arama yapilir.

Akis: marketplace secildiginde, o marketplace'in `BrandMarketPlaceMatch` kayitlarinda `MarketPlaceBrandName LIKE '%query%'` sorgusu.

### MockMarketplaceSearchService

Mevcut stub (bos liste) yerine test verisi donecek.

### Etkilenen Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs` | SearchBrandsAsync Trendyol API implementasyonu |
| `Business/Concrete/Trendyol/MockMarketplaceSearchService.cs` | Test verisi donecek |

---

## Parca 2: Auto-Match (String Matching + Ollama Fallback)

### Yeni Servis: IBrandAutoMatchService

`CategoryAutoMatchService` pattern'inde.

### Akis

1. Kullanici "Tumunu Otomatik Eslestir" butonuna basar
2. Eslesmemis markalar alinir (`GetUnmappedBrandsAsync`)
3. Secilen marketplace'in tum markalari cekilir (SearchBrandsAsync veya DB)

**Round 1 — String Matching:**
- Exact match (case-insensitive, trim)
- Normalized match (Turkce karakter normalizasyonu: s→s, c→c, g→g, u→u, o→o, i→i)
- Contains match (marketplace markasi uygulama markasini iceriyor veya tersi)
- Eslesen markalar otomatik kaydedilir

**Round 2 — Ollama Fallback (kalan eslesmemisler icin):**
- Ollama'ya "Bu uygulama markalarini marketplace markalariyla esle" prompt'u
- Confidence >= 0.8 → otomatik kaydet
- Confidence < 0.8 → kullaniciya goster, onay iste

**Sonuc:** "X otomatik eslestirildi, Y oneri var, Z eslestirilemedi"

### Yeni Dosyalar

| Dosya | Icerik |
|-------|--------|
| `Business/Abstract/IBrandAutoMatchService.cs` | Interface |
| `Business/Concrete/BrandAutoMatchService.cs` | String match + Ollama logic |

---

## Parca 3: Mevcut UI Iyilestirmeleri

### BrandMappingPage

- Summary bolumune "Tumunu Otomatik Eslestir" butonu eklenir
- Marketplace secili olmalidir (yoksa buton disabled)
- Butona basildiginda auto-match akisi baslar
- Sonuc dialog'u gosterilir: "X eslestirildi, Y oneri, Z eslestirilemedi"
- Oneri olanlari kullaniciya gosterir (confidence bari + onay/red butonlari)

### BrandMappingDetailPanel

- Arama calismaya baslar (SearchBrandsAsync fix)
- Zaten eslesmis marketplace markalari aramaresultlarinda "Kullaniliyor: {marka adi}" chip'i gosterilir, secilemez
- Bire-bir kisitlama: ayni uygulama markasi ayni marketplace'te tekrar eslestirilemez (service'te var, UI'da da gosterilecek)

### Etkilenen Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `Blazor/Features/MarketplaceSync/BrandMappingPage.razor` | "Tumunu Otomatik Eslestir" butonu + sonuc dialog |
| `Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs` | Auto-match akis logic |
| `Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor` | Duplicate prevention chip'leri |
| `Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs` | Eslesmis marka filtreleme |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | IBrandAutoMatchService DI kaydi |

---

## Duplicate Prevention Kurallari

1. **Marketplace arama sonuclarinda:** Zaten baska bir uygulama markasina eslestirilen marketplace markasi → disabled + "Kullaniliyor: {AppBrandName}" chip
2. **Ayni eslestirme tekrari:** Service katmaninda composite PK (ApplicationBrandId, MarketPlaceId) zaten engelliyor — UI'da da gri chip ile gosterilecek
3. **Marketplace bazinda bagimsiz:** Nike → Trendyol Nike ve Nike → N11 Nike ayri kayitlar, birbirini etkilemez

---

## Test Stratejisi

### Unit Testler
- `SearchBrandsAsync` Trendyol API mock ile
- `BrandAutoMatchService` — string matching (exact, normalized, contains)
- `BrandAutoMatchService` — Ollama fallback (mock HTTP)
- Duplicate prevention — eslesmis marka filtrelenmesi

### Entegrasyon Testler
- Auto-match full flow: eslesmemis markalar → string match → Ollama → sonuc

---

## Uygulama Sirasi

1. SearchBrandsAsync implementasyonu (Trendyol API + DB fallback)
2. BrandAutoMatchService (string matching + Ollama)
3. UI: "Tumunu Otomatik Eslestir" butonu + sonuc dialog
4. UI: Duplicate prevention chip'leri
5. Testler
