# Attribute Eslestirme Sayfasi + Duplicate Matching Prevention

**Tarih:** 2026-03-29
**Durum:** Onaylandi
**Oncelik:** High
**Iliskili:** `docs/tasks/marketplace-product-lifecycle-strategy.md`

## Amac

1. Kategori eslestirme sayfalarinda zaten eslestirilen marketplace/attribute/value'larin tekrar secilmesini engellemek
2. Yeni `/marketplace-sync/attributes` sayfasi ile attribute + value eslestirme UI'i olusturmak
3. Ollama destekli otomatik eslestirme onerisi sunmak
4. Tum eslestirme degisiklikleri icin grandfathering uyarisi alt yapisini hazirlamak

---

## Parca 1: Duplicate Prevention (Kategori Eslestirme)

### CategorySync Sayfasi

- Eslestirilen kategorilerde "Eslestir" butonu yerine "Duzenle/Kaldir" butonlari gosterilir
- Eslestirme durumu marketplace bazinda bagimsiz — Trendyol'da eslesmis ama N11'de Eşleşmemis olabilir

### BulkCategoryMatchPage

- Marketplace select'inde her marketplace yaninda eslestirme sayaci: "Trendyol (45/120)"
- Zaten eslestirilen kategoriler listeden cikarilir (mevcut davranis korunur)

### Etkilenen Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `Features/MarketplaceSync/CategorySync.razor` | Eslestirme durumuna gore buton degisimi |
| `Features/MarketplaceSync/CategorySync.razor.cs` | Buton handler'lari |
| `Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor` | Marketplace sayac gosterimi |
| `Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs` | Sayac hesaplama |

---

## Parca 2: Attribute Eslestirme Sayfasi

### Route

`/marketplace-sync/attributes`

Query parametreleri:
- `marketplaceId` (int?) — marketplace otomatik secilir
- `categoryId` (int?) — kategori otomatik secilir

### Sayfa Yapisi — 3 Bolum

**Ust bar:**
- Marketplace dropdown (tum aktif marketplace'ler)
- Kategori dropdown (sadece secilen marketplace ile eslestirilen kategoriler)

**Sol panel — Attribute listesi:**
- Secilen kategorinin uygulama attribute'lari
- Her attribute yaninda eslestirme durumu:
  - Yesil chip + marketplace attr adi → eslestirili
  - Kirmizi chip "Eslestrilmedi" → eslestrilmemis
  - Her attribute'in zorunlu/varianter/slicer badge'leri
- Zaten eslestirilen attribute'a tiklandiginda: bilgi + "Kaldir" butonu
- Eslestrilmemis attribute'a tiklandiginda: sag panelde eslestirme akisi

**Sag panel — Eslestirme:**
- Otomatik oneri: Marketplace'ten o kategorinin attribute'lari cekilir, isim benzerligi ile siralanir
- Ollama destegi: Benzerlik bulunamazsa Ollama ile capraz dil eslestirme (ornegin "Renk" <-> "Color")
- Manuel arama: Kullanici marketplace attribute adini yazip arayabilir
- Secim yapilinca "Onayla" butonu → `CategoryAttributeMarketPlaceMatch` tablosuna kaydedilir

**Duplicate prevention:**
- Zaten baska bir uygulama attribute'ina eslestirilen marketplace attribute'i oneri listesinde "kullaniliyor" olarak isaretlenir, secilemez
- Ayni uygulama attribute'i ayni marketplace'te iki kez eslestirilemez
- Her eslestirme marketplace bazinda bagimsiz — "Renk" Trendyol'da eslesmis ama Amazon'da Eşleşmemis olabilir

---

## Parca 3: Value Eslestirme (Ayni Sayfada Alt Bolum)

Attribute eslestirildikten sonra o attribute'in satirinda "Deger Eslestirme" butonu gorunur.

**Genisletilir panel:**
- Sol: Uygulama value'lari (ornegin Kirmizi, Mavi, Yesil)
- Sag: Marketplace value'lari (ornegin Red, Blue, Green)
- Otomatik oneri (isim benzerligi + Ollama)
- Manuel secim
- Onaylama → `CategoryAttributeValueMarketPlaceMatch` tablosuna kaydedilir

**Duplicate prevention:**
- Zaten eslestirilen marketplace value'i secilemez
- Marketplace bazinda bagimsiz

---

## Parca 4: Service Abstraction

### IMarketplaceCategoryAttributeProvider

```csharp
public interface IMarketplaceCategoryAttributeProvider
{
    int MarketPlaceId { get; }
    Task<List<MarketplaceAttributeDto>> GetAttributesForCategoryAsync(
        int marketplaceCategoryId, CancellationToken ct = default);
    Task<List<MarketplaceAttributeValueDto>> GetValuesForAttributeAsync(
        int marketplaceAttributeId, CancellationToken ct = default);
}
```

Her marketplace (Trendyol, Hepsiburada, N11, Amazon vb.) bu interface'i implement eder.

### IAttributeAutoMatchService

```csharp
public interface IAttributeAutoMatchService
{
    Task<List<AttributeMatchSuggestionDto>> SuggestAttributeMatchesAsync(
        List<AppAttributeDto> appAttributes,
        List<MarketplaceAttributeDto> marketplaceAttributes,
        CancellationToken ct = default);

    Task<List<ValueMatchSuggestionDto>> SuggestValueMatchesAsync(
        List<AppValueDto> appValues,
        List<MarketplaceValueDto> marketplaceValues,
        CancellationToken ct = default);
}
```

Ollama (192.168.1.78:11434) uzerinden isim benzerligi + anlam eslestirmesi. Mevcut `CategoryAutoMatchService` pattern'i kullanilir.

### Marketplace Implementasyon Durumu

| Marketplace | Attribute API | Mevcut Durum |
|-------------|--------------|-------------|
| Trendyol | `GET /product-categories/{id}/attributes` | Import sirasinda kullaniliyor |
| Hepsiburada | `GET /product/api/categories/{id}/attributes` | Mevcut ama kontrol edilmeli |
| N11 | SOAP `GetCategoryAttributes` | Mevcut |
| Amazon | SP-API Product Type Definitions | Mevcut |

---

## Parca 5: Navigasyon + Giris Noktalari

### Sidebar Menu

```
Pazar Yeri Sync
  ├── Kategori Eslestirme     (/marketplace-sync/categories)
  ├── Attribute Eslestirme    (/marketplace-sync/attributes)  ← YENI
  ├── Toplu Eslestirme        (/marketplace-sync/bulk)
  └── Sablonlar               (/marketplace-sync/templates)
```

### Chain Navigasyon

1. **Kategori wizard 3. adim** → "Eslestirme sayfasina git" checkbox → `/marketplace-sync/categories?categoryId=X`
2. **CategorySync sayfasi** → kategori eslestirildikten sonra satirda "Attribute Eslestir" butonu → `/marketplace-sync/attributes?marketplaceId=Y&categoryId=X`
3. **Sidebar** → dogrudan erisim

---

## Parca 6: Grandfathering Notu

Tum eslestirme degisiklikleri (kategori, attribute, value) ayni etki analizi pipeline'indan gececek. Bu, strateji dokumanindaki Parca 2 (Etki Analizi + Uyari) kapsaminda implement edilecek.

Eslestirme kaldirildiginda veya Değiştirildiginde:
- Etkilenen yayindaki urun Sayısı hesaplanir
- Kullaniciya uyari gosterilir: "Bu eslestirmeyi kaldirirseniz X urun etkilenir"
- Kullanici onaylarsa islem yapilir
- Etkilenen urunler "guncelleme gerekli" durumuna duser

Bu spec'te sadece duplicate prevention ve eslestirme UI'i implement edilir. Uyari sistemi ayri spec.

---

## Test Stratejisi

### Unit testler
- `IMarketplaceCategoryAttributeProvider` mock ile attribute yukleme
- `IAttributeAutoMatchService` oneri uretimi
- Duplicate prevention — zaten eslestirilen attribute/value filtrelenmesi
- Match kaydetme/kaldirma business logic

### bUnit testler
- Attribute listesi rendering — eslestirme durumu chip'leri
- Value genisletilir panel acilma/kapanma
- Marketplace/kategori dropdown filtreleme

### E2E testler
- Attribute eslestirme full flow: marketplace sec → kategori sec → attribute eslestir → value eslestir
- Duplicate prevention: eslestirilen attribute tekrar secilemez
- Chain navigasyon: wizard → category sync → attribute eslestirme
