# Pazaryeri-Bazlı Ürün Gönderim Özelleştirmesi — Design Spec

**Tarih:** 2026-06-10
**Hazırlayan:** PA (Product Analyst) Agent
**Durum:** Taslak — TL + Kullanıcı Onayı Bekleniyor
**İlgili:** DB Master (veri modeli + migration), SWE (transform motoru + override kullanımı + UI wiring), Designer (per-marketplace override/edit UI)
**Kaynak:** Kullanıcı isteği (TL üzerinden) + 3 paralel kod-önce keşif (bu spec'teki tüm ✅/❌ koda dayalı, dosya:satır kanıtlı)

---

## 1. Problem

Her pazaryerinin iş kuralları farklı. Kullanıcının verdiği örnek:

> *"Trendyol açıklama alanında SADECE düz metin kabul ederken, Pazarama HTML markup kabul ediyor. Ürün gönderilirken her pazaryerine kurallarına göre dönüştürülerek/özelleştirilerek gönderilmeli — ve bu özelleştirmeler kalıcı saklanmalı."*

Esnaf tek üründen onlarca pazaryerine satış yapacak. Her birine ayrı ayrı, elle, doğru formatta ürün hazırlamak = çile. Platformun değer vaadi: **bir kez gir, her pazaryerine o pazaryerinin kurallarına göre uyarlanmış halde gönder.**

**Kuzey Yıldızı:** Esnaf ürünü bir kez girer; istediği pazaryeri için (a) başlık/açıklama/fiyat/görsel/özellik override'ı girebilir VEYA (b) hiç dokunmaz, sistem o pazaryerinin kurallarına göre (HTML↔düz metin, karakter limiti, zorunlu özellik) **otomatik dönüştürerek** gönderir. Yanlış/eksikse gönderim öncesi **net uyarı** alır.

---

## 2. Mevcut Altyapı — Ne Var, Ne Eksik (kod-önce teyit)

> **ÖNEMLİ:** Kullanıcının "altyapısı zaten olmalı" sezgisi **büyük ölçüde DOĞRU**. Per-marketplace override veri modeli + manager + gönderim kullanımı VAR. Asıl eksik, kullanıcının verdiği **transform/kural** boyutu ve **override kapsamının darlığı** (sadece başlık/açıklama/fiyat + sadece Trendyol UI).

### ✅ VAR — sıfırdan yazılmayacak (reuse)

| Bileşen | Yer (dosya:satır) | Notlar |
|---|---|---|
| `ProductMarketplace` entity | `Entity/Products/ProductMarketplace.cs:6-40` | Ürün×pazaryeri + **TitleOverride, DescriptionOverride** + Status/ContentId/ExternalProductId/LastSyncedAt tracking |
| `ProductVariantMarketplaceOverride` entity | `Entity/Products/ProductVariantMarketplaceOverride.cs:5-18` | Variant×pazaryeri **ListPriceOverride, SalePriceOverride** |
| Migration uygulanmış | `DataAccess/.../Migrations/20260314232132_AddMarketplaceOverrides.cs` | Kolonlar + tablo + unique/composite index + cascade |
| EntityConfiguration | `.../ProductMarketplaceEntityConfiguration.cs:7-23`, `.../ProductVariantMarketplaceOverrideEntityConfiguration.cs:7-20` | Index, FK, query filter, money type |
| Manager | `Business/Abstract/IMarketplaceOverrideManager.cs:6-11` + `Business/Concrete/MarketplaceOverrideManager.cs:12-153` | `GetOverridesAsync`, `SaveOverridesAsync`, `SaveOverridesAndPublishAsync` — Validation→BusinessRules→Execution pipeline |
| DTO + Validator | `Entity/Dtos/Product/Marketplace/MarketplaceOverrideDto.cs:1-40` + `Validation/FluentValidation/SaveMarketplaceOverridesDtoValidator.cs:6-29` | Save/Detail DTO + MaxLength/GreaterThan kuralları |
| **Override gönderimde KULLANILIYOR** | 5 mapper'da `TitleOverride ?? product.Title` deseni | Trendyol `TrendyolProductMapper.cs:47-48`, Pazarama `:46-47`, Hepsiburada `:100-101`, N11 `:47-48`, Amazon `:53-54` |
| Variant fiyat override gönderimde | Trendyol `:120-121`, Pazarama `:129-130`, Hepsiburada `:111-112`, N11 `:114-115` | **Amazon HARİÇ** (bkz. ❌ bug) |
| Başlık/açıklama karakter limiti (truncate) | Trendyol başlık 100 `:193` / açıklama 30000 `:202`, Hepsiburada 500/30000, Amazon 200/2000 | Hardcoded, sessiz kırpma |
| Attribute eşleştirme altyapısı | `Entity/Matches/CategoryAttributeMarketPlaceMatch.cs`, `CategoryAttributeValueMarketPlaceMatch.cs` | Application attr/değer → pazaryeri attr/değer; gönderimde `TrendyolProductMapper.cs:65-77,138-189` kullanıyor |
| Zorunlu attribute kontrolü (preflight) | `ProductSyncManager.GetSendPreflightAsync()` ~`:376-444` | Kategori/marka/zorunlu-attr/varyant/barkod kontrolü → UI'da "Gönder" butonunu aç/kapa |
| Per-marketplace mapping validator'ları | `TrendyolMappingValidator`, `PazaramaMappingValidator`, `HepsiburadaMappingValidator` (EAN13), `N11/Pttavm/Ciceksepeti...` | Kategori/marka/zorunlu-attr + bazı format kuralları |
| Trendyol gönderim UI | `Features/Products/ProductController.cs:690-790` (`TrendyolSendGet/Post`) + `Views/TrendyolSend.cshtml` + `ViewModels/TrendyolSendVm.cs` | Başlık/açıklama/variant-fiyat override formu + preflight |

### ❌ EKSİK / ⚠️ KISMİ — asıl iş burada

| # | Eksik | Durum | Kanıt / Etki |
|---|---|---|---|
| E1 | **HTML ↔ düz metin transform** (kullanıcının ÖRNEĞİ) | ❌ YOK | Hiçbir mapper'da HTML strip/sanitize/convert yok (`StringExtension.cs`'te de yok). Açıklamadaki `<b>...</b>` Trendyol'a olduğu gibi gidiyor → hata/kötü görünüm riski. Pazarama'ya düz metin giderse HTML zenginliği kaybı. |
| E2 | **Pazaryeri kural motoru** (merkezî) | ❌ YOK | Kurallar dağınık/hardcoded: başlık limiti `[..100]`, açıklama `[..30000]` her mapper'da magic-number. "Bu pazaryeri açıklama formatı=HTML/Plain, başlık max=N, görsel max=M, zorunlu-attr=..." tek yerde tanımlı değil. |
| E3 | **Görsel (image) per-marketplace override** | ❌ YOK | `ProductMarketplace`/variant override'da image alanı yok. Tüm mapper `variant.Images`'i kullanıyor (Hepsiburada max5, Amazon max9 kırpma var ama seçim yok). "Trendyol için farklı kapak görseli" yapılamıyor. |
| E4 | **Attribute (özellik) per-marketplace override** | ❌ YOK | Eşleştirme global (kategori-attr seviyesi); ürün-bazlı "bu ürünün Trendyol rengi 'Kızıl', Pazarama 'Kırmızı'" override'ı yok. |
| E5 | **Override UI yalnızca Trendyol** | ⚠️ KISMİ | Diğer 7 pazaryeri (Pazarama/Hepsiburada/N11/Amazon/Pttavm/Çiçeksepeti/Temu) için override girme ekranı YOK. Spec `2026-03-31-trendyol-product-send-page-design.md:259`: "diğerleri sonraki fazlarda". Manager+mapper hazır, eksik olan UI. |
| E6 | **Amazon variant fiyat override bug** | 🔴 BUG | `AmazonProductMapper.cs:74` doğrudan `variant.SalePrice` okuyor, override'ı yok sayıyor. Diğer 4 pazaryeri okuyor. |
| E7 | **Sessiz truncate (fidelity)** | ⚠️ KISMİ | 150 karakter başlık → Trendyol'a 100'e kırpılıp sessizce gidiyor; kullanıcı uyarılmıyor. Override validator `TitleOverride.MaximumLength(200)` ama Trendyol gerçek limiti 100 → tutarsız. |
| E8 | **Gönderimde zorunlu-attr enforcement zayıf** | ⚠️ KISMİ | Preflight'ta kontrol var ama mapper'da eşleştirme yoksa özellik **sessizce skip** ediliyor (`TrendyolProductMapper` attribute eşleşmezse atlıyor), hata vermiyor → eksik ürün gidebilir. |

---

## 3. Önerilen Yaklaşım

İş üç bağımsız eksene ayrılıyor; her biri ayrı fazlanabilir. **Kullanıcının asıl derdi (E1+E2) önce; kapsam genişletme (E3/E4/E5) sonra; bug/fidelity (E6/E7/E8) hızlı kazanç.**

### 3.1 Pazaryeri Kural Seti + Transform Motoru (E1 + E2) — ÇEKİRDEK

**Fikir:** Her pazaryeri için tek yerde tanımlı bir **kural profili** (capability descriptor) + bu profile göre çalışan bir **transform pipeline**. Mapper'lar magic-number yerine bu profili okur.

Önerilen kural profili (kod tarafı, appsettings veya DB — DB Master kararı):

```
MarketplaceContentRules (her MarketPlaceId için):
  DescriptionFormat: PlainText | Html              // Trendyol=PlainText, Pazarama=Html
  TitleMaxLength: int                              // Trendyol=100, Hepsiburada=500...
  DescriptionMaxLength: int                        // Trendyol=30000, Amazon=2000...
  TitleAllowsHtml: bool
  MaxImages: int                                   // Hepsiburada=5, Amazon=9...
  AllowsCustomAttributeValue: bool                 // Pazarama=false (sadece eşleşen değer)
  RequiredAttributeEnforcement: Block | Warn | Skip
```

**Transform pipeline (gönderim öncesi, deterministik):**
1. **Format dönüşümü:** kaynak açıklama HTML ve hedef PlainText ise → HTML strip (tag temizle, entity decode, satır yapısını koru). Hedef Html ve kaynak düz metin ise → minimal `<p>`/`<br>` sarmalama. *(Kütüphane kararı SWE/DB: HtmlSanitizer / HtmlAgilityPack — yeni paket TL onayı.)*
2. **Limit uygulama:** truncate yerine **kelime-sınırında akıllı kısaltma** + (E7) limit aşımında kullanıcıya gönderim öncesi uyarı.
3. **Doğrulama:** zorunlu attribute eksik / custom-value desteklenmiyorsa hedef pazaryeri kuralına göre Block/Warn.

**Neden değerli:** Kullanıcının verdiği örneğin TAM karşılığı. "Bir kez gir, her yere doğru formatta git" vaadini gerçekleştirir. Magic-number dağınıklığını da merkezîleştirir (tech-debt temizliği).

### 3.2 Override Kapsamını Genişlet (E3 + E4) — VERİ MODELİ

- **Görsel override (E3):** `ProductMarketplaceImageOverride` (ProductMarketplaceId, ProductVariantId?, ImageUrl/ImageId, SortOrder) — null ise ürünün kendi görselleri. Veya basit: `ProductMarketplace.ImageSelectionJson` (seçili/sıralı görsel id listesi). **DB Master kararı** (ayrı tablo vs JSON kolon; sorgu/normalizasyon dengesi).
- **Attribute override (E4):** `ProductMarketplaceAttributeOverride` (ProductMarketplaceId, ApplicationCategoryAttributeId, OverrideValueId? / OverrideCustomValue?). Mapper bu override'ı eşleştirmeden ÖNCE uygular.

> Not: E3/E4 daha az acil. İlk müşteri (zekidsbebe) için E1+E2 (doğru açıklama formatı) + E5 (çok pazaryeri UI) muhtemelen daha yüksek değer. **Önceliklendirme TL/kullanıcı kararı.**

### 3.3 Override/Edit UI'yı Tüm Pazaryerlerine Yay (E5) — DESIGNER + SWE

Manager (`IMarketplaceOverrideManager`) ve mapper'lar pazaryeri-agnostik hazır. Eksik olan **UI**. İki seçenek:

- **A) Generic send/override partial:** `TrendyolSend.cshtml`'i pazaryeri-parametrik tek partial'a çıkar (`MarketplaceSend(marketPlaceId)`), preflight + override formu + (3.1'den) kural-bazlı alanlar (HTML editör vs düz textarea, limit göstergesi) dinamik. **Önerilen** — DRY, kural motoruyla doğal eşleşir.
- **B) Her pazaryerine ayrı sayfa:** Trendyol pattern'ini kopyala. Hızlı ama 7× tekrar + bakım yükü.

**Designer'a düşen:** kural profiline duyarlı override formu (DescriptionFormat=Html → zengin editör/HTML textarea; PlainText → düz textarea + canlı karakter sayacı; görsel seçici E3 için; "boş bırak = otomatik" mikrokopi).

### 3.4 Hızlı Kazançlar (E6 + E7 + E8) — SWE/QA, küçük

- E6: `AmazonProductMapper.cs:74` → variant override oku (diğer mapper deseniyle birebir). TDD: RED test (override set → Amazon request'inde override fiyat).
- E7: Override validator limitlerini **gerçek pazaryeri limitiyle hizala** (kural profilinden); aşımda gönderim öncesi uyarı.
- E8: Mapper'da zorunlu-attr eşleşmezse sessiz skip yerine kural profili `RequiredAttributeEnforcement`'a göre Block/Warn.

---

## 4. Kabul Kriterleri

1. **(E1)** HTML içeren bir açıklamayla ürün, DescriptionFormat=PlainText olan pazaryerine (Trendyol) gönderildiğinde, gönderilen body'de HTML tag YOK; metin/satır yapısı korunmuş. Aynı ürün Pazarama'ya (Html) gönderildiğinde HTML korunur.
2. **(E2)** Başlık/açıklama limitleri ve format kuralları tek merkezî kaynaktan okunur; mapper'larda magic-number kalmaz. Yeni pazaryeri eklerken kural tek yerde tanımlanır.
3. **(E2/E7)** Kullanıcı limiti aşan başlık/açıklama girerse gönderim öncesi **net uyarı** alır (sessiz kırpma yok).
4. **(E5)** Esnaf en az 3 pazaryeri (Trendyol + 2) için override formundan başlık/açıklama/fiyat girip kaydedebilir; kayıt kalıcıdır ve sonraki gönderimde kullanılır.
5. **(E6)** Amazon'a fiyat override'lı variant gönderiminde override fiyat gider (orijinal değil).
6. **(genel)** Override girilmemiş ürün, pazaryerinin kurallarına göre **otomatik dönüştürülerek** sorunsuz gider (regresyon yok). Mevcut Trendyol gönderimi bozulmaz.

---

## 5. Manuel Test Adımları

1. admin/123456789 ile giriş → bir ürünün açıklamasına HTML gir (`<b>Kalın</b><ul><li>Madde</li></ul>`).
2. Ürünü Trendyol'a gönder → WireMock/gerçek request body'sini incele: açıklamada HTML tag olmamalı, "Kalın" ve "Madde" düz metin olarak görünmeli.
3. Aynı ürünü Pazarama'ya gönder → request body'sinde HTML korunmuş olmalı.
4. Başlığı 130 karakter yap, Trendyol'a göndermeyi dene → gönderim öncesi "başlık 100 karakteri aşıyor" uyarısı gelmeli (sessiz kırpılmamalı).
5. Bir pazaryeri için (örn. Hepsiburada) override formundan başlık+açıklama+variant fiyatı gir, kaydet → sayfayı yenile, değerlerin korunduğunu gör; gönderimde override değerlerin gittiğini doğrula.
6. Amazon'a fiyat override'lı variant gönder → request'te override fiyatın (orijinal değil) gittiğini doğrula.
7. Override hiç girilmemiş bir ürünü her pazaryerine gönder → hepsinin kendi kuralına göre (limit/format) dönüştürülüp hatasız gittiğini doğrula.
8. (E8) Zorunlu attribute'u eşleşmemiş ürünü göndermeyi dene → kural profiline göre engellendiğini/uyarıldığını doğrula (sessizce eksik gitmemeli).

---

## 6. Rol Dağılımı

| Eksen | Sahip | İş |
|---|---|---|
| E1 Transform pipeline (HTML↔text, limit, sanitize) | **SWE** + kütüphane onayı **TL** | Deterministik transform servisi + mapper entegrasyonu; TDD-first |
| E2 Kural profili veri/konum | **DB Master** (DB'de mi config'de mi) + **SWE** | `MarketplaceContentRules` modeli; multi-tenant uyumlu (tenant override edebilir mi?) |
| E3 Görsel override veri modeli | **DB Master** | Ayrı tablo vs JSON kolon kararı + migration |
| E4 Attribute override veri modeli | **DB Master** | `ProductMarketplaceAttributeOverride` + migration |
| E5 Generic override/send UI | **Designer** (form/UX) + **SWE** (wiring) | Kural-duyarlı form; Trendyol partial'ı generic'e çıkar |
| E6 Amazon bug | **SWE** + **QA** | Tek satır fix + RED test |
| E7 Limit hizalama + uyarı | **SWE** | Validator ↔ kural profili senkron |
| E8 Zorunlu-attr enforcement | **SWE** | Sessiz skip → Block/Warn |
| Tüm akış DoD (Unit+Integration+E2E) | **QA** | RED-first, WireMock fidelity ile gerçek request body assert |

---

## 7. Açık Sorular (TL/Kullanıcı onayı gereken)

1. **Öncelik:** E1+E2 (transform/kural — kullanıcının örneği) mi, yoksa E5 (çok-pazaryeri UI) mi önce? (PA önerisi: **E1+E2 önce** — örnek bu, sonra E5.)
2. **Kural konumu:** Kural profili **DB'de** mi (tenant başına özelleştirilebilir, esnek) yoksa **kod/appsettings**'te mi (basit, pazaryeri sabit)? (PA önerisi: pazaryeri kuralı sabit/global → kod/config; ürün-bazlı override zaten DB'de. DB Master son söz.)
3. **HTML kütüphanesi:** Yeni paket (HtmlSanitizer/HtmlAgilityPack) eklenmesi TL onayına tabi (kırmızı çizgi: yeni bağımlılık).
4. **E3/E4 kapsamı:** Görsel/attribute override ilk müşteri için gerçekten gerekli mi, yoksa V2'ye ertelenebilir mi? (zekidsbebe ihtiyacına göre.)
5. **Otomatik mi opt-in mi:** Override girilmediğinde otomatik transform "sessiz ve agresif" mi olsun (kullanıcı görmeden HTML strip) yoksa önizleme/onay mı? (Fidelity açısından önizleme daha güvenli.)

---

## 8. Notlar

- Tüm ✅/❌ bu spec'te 3 paralel kod-önce keşfe dayanır (mapper'lar, manager, controller, migration, validator, attribute-match okundu). Doküman değil **kod** referans alındı.
- WireMock fidelity (CLAUDE.md strict-rule) ile bağlantı: transform sonucu gönderilen body, ilgili pazaryeri mock'unun beklediği formatla uyumlu olmalı; geçersizse mock doc-doğru hata dönmeli (T2/T4 ile kesişir).
- Bu spec **sadece araştırma + plan**; kod yok. TL + kullanıcı onayından sonra task'lara bölünüp `tasks.json`'a yazılacak (her eksen ayrı task, `manual_test_steps` dahil).
