# Spec: Ürün Detay Sayfası — Storefront Ayırma + Performans Odağı

**Tarih:** 2026-06-11  
**Durum:** TL onayı bekleniyor  
**Bağlı task'lar:** "Ürün Detay — Storefront Ayarlarını Ayrı Sayfaya Taşı" + "Ürün Detay — Performans KPI + Header Redesign"  
**Intel güncellemesi (SWE-Ahmet, 2026-06-11):** Endpoint isimleri teyit edildi. EcommerceEnabled flag etkisi eklendi.

---

## Problem

`/products/{id}` sayfası şu an iki farklı sorunu tek ekranda birleştiriyor:

1. **Ürünün genel durumu / performansı** — stok, satış, pazaryeri durumu, aktivite
2. **E-ticaret sitesi (storefront) ayarları** — SEO başlığı, slug, açıklama, varyant fiyatları, yayın durumu

Bu karışım esnafın zihinsel modeliyle uyuşmuyor: "Ürünü inceleyelim" ile "Bu ürünü siteye yayınlayayım" farklı niyetler. Şu anki yapıda storefront ayarları sayfanın altında HTMX ile lazy-load ediliyor ve kaybolmuş durumda.

---

## Hedef

| Sayfa | URL | İçerik |
|-------|-----|---------|
| Ürün Detay | `/products/{id}` | Performans KPI kartları + ürün meta + pazaryeri + aktivite |
| Storefront Ayarları | `/products/{id}/storefront` | SEO + mağaza fiyatları + yayın durumu/aksiyonu |

---

## Kapsam Dışı

- Yeni backend entity / migration gerekmez — mevcut `ProductVariant.ECommercePrice`, `Product.SeoTitle/Slug/Description/Keywords`, `Product.IsPublished` alanları zaten var
- `_Product360` partial'ı içindeki sekmeler değişmez
- `ProductDetailDto` değişmez (sadece controller action + yeni view)

---

## Mevcut Kaynak — Teyit Edildi

**`ProductController.cs` satır numaraları (SWE-Ahmet teyidi):**

| Action adı | HTTP | Route | Satır |
|-----------|------|-------|-------|
| `StoreSettings` | GET | `/products/{id}/store-settings` | 1150 |
| `SaveStoreSettings` | POST | `/products/{id}/store-settings` | 1185 |
| `PublishToStore` | POST | `/products/{id}/store-settings/publish` | 1200 |

View: `Partials/_StoreSettings.cshtml`  
ViewModel: `ViewModels/StoreSettingsVm.cs`

**Taşıma işi:** Bu 3 action + view + VM aynı `ProductController` içinde kalabilir; sadece route'lar ve view dosyası yeniden adlandırılır.

---

## EcommerceEnabled Feature Flag — Önemli

`ProductActivityController.cs` (satır 45, 68) `pageManager.IsEcommerceEnabledAsync()` ile ecommerce durumunu kontrol ediyor. Sonuç `ProductActivityTimelineVm.EcommerceEnabled` ve `ProductMarketplaceCardsVm.EcommerceEnabled` alanlarına aktarılıyor.

**Storefront sayfası için etki:**  
`EcommerceEnabled = false` ise `/products/{id}/storefront` sayfası:
- Ya erişimi engeller (redirect + uyarı: "E-ticaret modülü aktif değil")
- Ya da formu gösterir ama "Yayınla" butonunu disable eder + banner: "Storefront ayarları için e-ticaret modülünün aktif olması gerekir"

**SWE kararı:** Hangi davranışın uygulanacağını TL/SWE belirler. Spec minimum gereksinim: `EcommerceEnabled` kontrolünü storefront sayfasına ekle, sessizce başarı simüle etme.

---

## Mevcut Storefront Alanları Tespiti (`_StoreSettings.cshtml`'den)

### SEO Grubu
| Alan | Model field | Kural |
|------|-------------|-------|
| SEO Başlık | `SeoTitle` | Boş = ürün adı kullanılır, max 70 karakter |
| SEO URL (Slug) | `SeoSlug` | Boş = otomatik oluşturulur |
| SEO Açıklama | `SeoDescription` | Maks 160 karakter |
| SEO Anahtar Kelimeler | `SeoKeywords` | Virgülle ayrılmış |

### Mağaza Fiyatları Grubu (per-variant)
| Alan | Model field | Kural |
|------|-------------|-------|
| Mağaza Fiyatı | `VariantPrices[i].ECommercePrice` | Boş = satış fiyatı (SalePrice) kullanılır |

### Yayın Durumu + Aksiyonlar
| Öğe | Endpoint |
|-----|----------|
| IsPublished badge | — |
| "Kaydet" | `POST /products/{id}/store-settings` |
| "Kaydet ve Yayınla" | `POST /products/{id}/store-settings/publish` |

---

## Yeni Mimari

### `/products/{id}` — Ürün Detay (Performans Odağı)

**Kaldırılacak:**
- `Detail.cshtml` satır 59-72: HTMX `hx-get="/products/{id}/store-settings"` bloğu

**Eklenecek:**
- Tabler `page-header` modernize (ürün adı başlık, badge, page-actions)
- `page-actions` dropdown: Düzenle, **Storefront Ayarları** (yeni sayfaya link), Varyantları Yönet, İndirim Uygula, Barkod Yazdır, ZPL İndir, Sil
- Performance KPI kartları (6 × `card-sm`, kategori detay şablonuna benzer — HTMX lazy-load ile ayrı endpoint'ten çekilebilir):
  - Son 30 gün: Satış Adedi, Ciro (₺), Sipariş Sayısı, İade Adedi, Toplam Stok, Aktif Varyant Sayısı
- `_Product360` partial (değişmez)

**Backend notu:** KPI verisi HTMX lazy-load olarak ayrı bir `GET /products/{id}/performance-summary` endpoint'inden çekilebilir — SWE tasarlar; DB Master hot-path query'yi onaylar.

### `/products/{id}/storefront` — Storefront Ayarları

**Yeni dosyalar:**
- `Features/Products/Views/StorefrontSettings.cshtml`

**İçerik:**
- Breadcrumb: `Ürünler → {ürün adı} → Storefront Ayarları`
- Yayın Durumu kartı — IsPublished badge + "Yayınla" / "Yayından Kaldır" butonu
- SEO Bilgileri kartı — SeoTitle, SeoSlug, SeoDescription, SeoKeywords
- Mağaza Fiyatları kartı — varyant başına ECommercePrice tablosu
- Kaydet butonu (full-page PRG: POST → TempData.SetSuccess → redirect)

**Route'lar:**
```
GET  /products/{id}/storefront        → StorefrontSettingsPage(id)
POST /products/{id}/storefront        → mevcut store-settings kaydetme mantığı buraya taşınır
POST /products/{id}/storefront/publish → mevcut publish mantığı buraya taşınır
```

**Eski endpoint'ler:** `/products/{id}/store-settings` ve `/products/{id}/store-settings/publish` kısa süre için 301 redirect olarak tutulabilir (geriye dönük uyumluluk) — SWE karar verir.

---

## Rol Dağılımı

| Adım | Kim | Ne |
|------|-----|----|
| 1 | **SWE** | `StorefrontSettings.cshtml` için controller action + `StoreSettingsVm` geçişi (POST/redirect) |
| 2 | **Designer** | `StorefrontSettings.cshtml` tasarla (SEO + fiyat + yayın kartları, Ürünler tarzı) |
| 3 | **Designer** | `Detail.cshtml` güncelle: HTMX store-settings bloğunu kaldır, KPI kartları + modern header ekle |
| 4 | **SWE** | KPI endpoint wire (performans aggregate — DB Master onayı gerekebilir) |
| 5 | **QA** | Her iki sayfa için manuel + integration test |

---

## Kabul Kriterleri

1. `/products/{id}` sayfasında "Mağaza Ayarları" kartı GÖRÜNMEZ.
2. `/products/{id}/storefront` URL'i açılıyor, SEO + fiyat + yayın alanları mevcut.
3. Ürün detay sayfasından bir buton/link ile Storefront Ayarları sayfasına gidiliyor.
4. Mevcut SEO kaydetme ve "Yayınla" fonksiyonu yeni sayfada çalışıyor.
5. Storefront sayfasında kaydedilen ECommercePrice'ın ürün detaydaki `_Product360` kartlarına yansıdığı görülüyor.
6. Breadcrumb / geri navigasyon storefront sayfasında mevcut.

---

## Manuel Test Adımları

1. `http://localhost:8085/products/{herhangi-bir-id}` aç — sayfa altına scroll et: "Mağaza Ayarları" kartı GÖRÜNMEMELI.
2. Header'da "Storefront Ayarları" linkini bul, tıkla → `/products/{id}/storefront` açılmalı.
3. Storefront sayfasında: SEO başlığı boş bırak, Slug = `test-slug-{id}` gir, Kaydet → başarı toast'u görünmeli.
4. `/products/{id}/storefront` adresine doğrudan git (bookmark testi) — sayfa açılmalı.
5. "Kaydet ve Yayınla" butonuna tıkla → IsPublished badge "Mağazada Yayında" olmalı; ürün detay sayfasında da badge güncellenmeli.
6. Varyantlı ürünün storefront sayfasında her varyant için ECommercePrice alanı görünmeli; bir fiyat gir, kaydet, ürün detaydaki bilgi yansımalı.
7. SEO slug alanına Türkçe karakter içeren değer gir → kayıt sonrası normalize (ASCII-only) edilmeli.
8. Storefront sayfasından "Ürüne Dön" / breadcrumb ile `/products/{id}` sayfasına dönülmeli.
