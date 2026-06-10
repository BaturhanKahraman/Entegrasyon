# Storefront — Claude Design Prompt Seti

Bu dosya, çocuk giyim mağazası için storefront'un tüm sayfalarını Claude design (claude.ai → "design" modu) içinde tek tek üretebilmen için hazırlanmış prompt'ları içerir. Her prompt **bağımsız** olarak yapıştırılabilir; üstteki **Tasarım Sistemi** bloğu her promptun başında özet olarak bulunur ki Claude design yeni bir konuşmada da aynı stilde üretsin.

Akış:
1. İlk konuşmaya **"Tasarım Sistemi"** bloğunu yapıştır → "Anladım" cevabı al.
2. Aynı konuşmada sıradaki sayfa promptunu yapıştır → çıktıyı al → beğenmediysen "şu kısmı şöyle değiştir" diyerek iterate et.
3. Memnun kaldığın çıktıyı bana gönder, ben worktree'deki ilgili `.cshtml`'e Razor binding'leriyle yerleştireyim.

> **Marka adı placeholder:** Aşağıdaki bloklarda `Zekids Bebe` geçtiği yerleri kendi markanla değiştir (henüz net değilse "Çocuk Atölyesi" gibi geçici bir ad bırak). Renk paleti ve tipografi de değiştirilebilir; promptun başındaki "Marka" satırını düzenle, gerisi otomatik uyar.

---

## 0. Tasarım Sistemi (her konuşmanın başında bir kez)

```
Bir e-ticaret storefront tasarlayacağız. Sektör: çocuk giyim. Tüm sayfalar boyunca aşağıdaki tasarım sistemini birebir kullan, ekstra kütüphane önerme.

MARKA & TON
- Marka: Zekids Bebe (premium-affordable çocuk giyim, 0-14 yaş)
- Hedef kitle: 25-40 yaş anne/baba; mobil-ağırlıklı
- Ton: sıcak, oyuncu ama lüks değil; güvenilir, güven veren; "siz" diliyle Türkçe
- Voice örnekleri: "Minik dolaplara büyük ilham", "Bedeniniz mi şaşırttı? 30 gün ücretsiz iade"

PALET (CSS değişkenlerine bağlı; --color-primary tenant'tan gelir)
- Primary (pembe): #FF8FB1
- Secondary (bebek mavisi): #8FCBE8
- Accent (sıcak sarı): #FFD56B
- Cream (arka plan): #FAF6F0
- Charcoal (text): #2C3E50
- Muted (ikincil text): #7B8794
- Success: #4CAF89  Danger: #E27D7D  Warning: #F2B544
Tailwind class'larında bg-[#FF8FB1] gibi arbitrary değerler yerine var(--color-primary) için `bg-primary` token'ını kullan; mevcut config'de tanımlı.

TİPOGRAFİ
- Heading: "Fraunces", serif — friendly, biraz dekoratif, hafif curl. Hero'da italic variant kullanılabilir.
- Body: "Inter", sans-serif.
- Heading H1 mobil: text-3xl, masaüstü: text-5xl. H2: text-2xl/text-4xl. Body: text-base, leading-relaxed.
- font-weight: 400 body, 600 başlık. Hiç 900 kullanma — yumuşak görünüm.

GÖRSEL DİL
- Köşe yuvarlama bol: rounded-2xl varsayılan, rounded-3xl hero kartları için.
- Gölgeler yumuşak: shadow-sm/shadow-md; hiç keskin shadow-xl kullanma.
- İmaj oranı: aspect-[4/5] ürün kartları, aspect-square avatar/kategori, aspect-[16/9] hero.
- Spacing cömert: gap-6 minimum kart aralığı, py-16 section padding, container max-w-7xl mx-auto px-4 sm:px-6 lg:px-8.
- Iconography: Lucide outline (örn. lucide-shopping-bag, lucide-heart). SVG'leri inline ver; npm/CDN yok.
- Animasyon: hover'da hafif scale-105 + opacity geçişi (300ms). Sayfa geçişlerinde animasyon yok.
- Border: border-gray-200 yerine border-cream-300 (#E8DFD2) gibi sıcak ton.

BRAND DETAY (çocuk giyimine özel)
- Yaş kategorileri filtre öncüsü: 0-3 ay, 3-6 ay, 6-12 ay, 1-2 yaş, 2-4 yaş, 4-6 yaş, 6-8, 8-10, 10-12, 12-14.
- Cinsiyet: Kız / Erkek / Unisex (eşit görünür, etiket yok "kızlara özel"/"erkeklere özel" gibi).
- Mevsim filtresi: İlkbahar, Yaz, Sonbahar, Kış.
- Trust band her sayfada (footer'dan farklı, ürün sayfası altı/sepet üstü gibi): "Kargo Bedava 500₺ üstü · 30 Gün Ücretsiz İade · Güvenli Ödeme · WhatsApp Destek".
- Beden tablosu önemli; Product Detail'de modal olarak yaş ↔ boy/kilo ↔ beden mapping göster.
- Görseller: stüdyo arka planı sade krem/pembe pastel; çocuk modeli oyun halinde — statik poz değil.

TEKNİK ÇIKTI FORMATI
- Output: tek `.cshtml` fragment, Razor + Tailwind utility class.
- @model satırını koru (sana hangi tip olduğu söylenecek). Model yoksa @model satırı yok.
- `@{ Layout = ... }` yazma — _Layout zaten ayarlı.
- @section Scripts için yalnızca o sayfaya özel JS gerekiyorsa kullan.
- ViewData["Title"] = "Sayfa Başlığı" üstte ayarla.
- Mevcut JS kütüphaneleri: Tom Select (aranabilir dropdown), Flatpickr TR locale (tarih), IMask (input mask), Notyf (toast), SortableJS, GLightbox (gallery lightbox). Bunların hepsi `wwwroot/lib/` altında — direkt class isimleri kullanılabilir, npm gerekmez.
- HTMX kullanma — bu storefront vanilla JS.
- Razor If/foreach yapısı için örnek:
  ```
  @foreach (var item in Model.Items) {
      <div>@item.Name</div>
  }
  ```
- Türkçe metinler (label, button, hata mesajı) tüm sayfada Türkçe ve diakritik tam.
- Aria/a11y: button'lara aria-label, decorative svg'ye aria-hidden="true", focus-visible ring.

Bu sistemi anladığını "Sistem anlaşıldı, hangi sayfayı tasarlayalım?" diyerek onayla.
```

---

## 1. `Views/Shared/_Layout.cshtml` — Ana Şablon

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Storefront ana layout'unu üretmeni istiyorum. Aşağıdaki Razor bağlamı korunmalı:
- @inject Entegrasyon.Business.Abstract.IStorefrontTenantContext Tenant
- Tenant.Settings: StoreName, StoreSlogan, LogoUrl, FaviconUrl, PrimaryColor, SecondaryColor, AccentColor, CustomCss, AnnouncementBarActive (bool), AnnouncementBarText, AnnouncementBarColor, ContactPhone, WhatsAppNumber, ContactEmail, Address, InstagramUrl, FacebookUrl, YoutubeUrl, GoogleAnalyticsId.
- Tenant.Categories: List<CategoryTreeDto> (id, name, slug, parentId, children) — mega menu için.

İskelet:
1. <!DOCTYPE> + lang="tr" + meta + title (ViewData["Title"] ?? Tenant.Settings.StoreName), favicon (Tenant.Settings.FaviconUrl varsa), Google Fonts (Fraunces + Inter), <link site.css>, CustomCss varsa <style>{raw}</style>, GA snippet (GoogleAnalyticsId varsa).
2. <body>:
   a. **Announcement bar** (üst): Tenant.Settings.AnnouncementBarActive ve text varsa görünür; arka plan AnnouncementBarColor; sağda kapatma X. Tek satır, küçük punto.
   b. **Header** (sticky top, beyaz arka plan, alt border):
      - Sol: Logo (LogoUrl yoksa StoreName text logosu — Fraunces italic).
      - Orta (desktop): Mega menu trigger "Tüm Kategoriler" + sabit linkler (Yeni Gelenler, Kız, Erkek, Bebek, Outlet).
      - Sağ: Arama ikonu (mobilde küçülen arama bar açar), Hesabım dropdown (giriş yapılmışsa ad, değilse "Giriş"), Wishlist (kalp + sayı), Sepet (poşet + sayı + tutar).
      - Mobil: hamburger menüsü (slide-in left), arama, sepet ikonu.
   c. **Mega menu** (header'a açılan): @await Component.InvokeAsync("MegaMenu", Tenant.Categories) — Tenant context'inden kategoriler. Açıkken header'ın altında full-width pastel arka plan, üç sütunlu (kız/erkek/unisex) kategori grid'i, sağda öne çıkan koleksiyon görseli.
   d. **Main**: @RenderBody().
   e. **Trust band** (footer üstü): 4 ikon + metin (Kargo Bedava, 30 Gün İade, Güvenli Ödeme, WhatsApp Destek). bg-cream, py-12.
   f. **Newsletter**: "Yenilikler kutunuza gelsin. %10 indirim hediye." input + button. bg-primary/10, rounded-3xl, max-w-4xl ortalı.
   g. **Footer** (4 sütun): "Hakkımızda" linkleri, "Yardım & İletişim" (KVKK, İade, Kargo, SSS, İletişim), "Bizi Takip Edin" (Insta/FB/YT ikonları → Tenant.Settings'ten), "Bize Ulaşın" (telefon, email, adres, WhatsApp). Altta ince çizgi + telif satırı + ödeme yöntemleri ikonları (Visa/MC/Troy).
3. @RenderSectionAsync("Scripts", required: false) kapanış body öncesi.

Mobil-first: hamburger menü slide-in, arama overlay full-screen.
```

---

## 2. `Views/Shared/Components/MegaMenu/Default.cshtml`

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model List<Entegrasyon.Entity.Dtos.Storefront.CategoryTreeDto>

Her CategoryTreeDto: Id, Name, Slug, ParentId, Children (List<CategoryTreeDto>).

Header'da "Tüm Kategoriler" trigger'a hover/click ile açılan full-width panel.
Layout:
- 4 sütun: Kız / Erkek / Bebek / Outlet (Children'ı ParentSlug'a göre filtrele veya sabit gruplandır).
- Her sütun başı bold, altında children listesi (5-8 link).
- En sağda bir görsel card: "Yeni Koleksiyon" CTA — pastel pembe arka plan, ürün görseli placeholder, "Keşfet" buton.
- Açma animasyonu: opacity + translateY (200ms).
- Kapanış: header dışına click, ESC.
```

---

## 3. `Views/Home/Index.cshtml` — Anasayfa

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Storefront anasayfası. Model yok (ViewBag ile veya sample data ile mock'la). Sample data: 8 yeni ürün (ad, fiyat, eski fiyat, görsel, beden range, "yeni"/"%X indirim" badge), 6 yaş kategorisi, 3 koleksiyon kartı.

Sayfa akışı (sırayla):
1. **Hero** (full-width, aspect-[16/9] desktop / aspect-[4/5] mobil):
   - Sol yarı: büyük Fraunces italic başlık "Çocukluğun büyüsünü giydir" + alt satır pazarlama metni + 2 CTA ("Yeni Gelenler" primary, "Outlet" outline).
   - Sağ yarı: pembe-bebek mavisi gradient'li çerçeve içinde mutlu çocuk fotoğrafı placeholder.
   - Köşede küçük "%30'a varan indirim" sticker rotated -8deg.
2. **Yaş kategorileri** (yatay kaydırılabilir mobilde, grid masaüstü):
   - 8-10 kart: yuvarlak görsel (aspect-square, rounded-3xl pastel arka plan + ortada illustration/foto), altta "0-3 ay", "3-6 ay" ... "12-14 yaş".
   - Hover: scale + shadow.
3. **Cinsiyet split** (2-3 büyük kart yan yana):
   - "Kız" (pembe pastel), "Erkek" (mavi pastel), "Unisex" (krem). Her birinde model fotoğrafı + "Keşfet" CTA overlay.
4. **Yeni Gelenler** carousel (8 ürün):
   - Bölüm başlığı "Bu Hafta Eklediklerimiz" + sağda "Tümü →" link.
   - Ürün kartı: aspect-[4/5] görsel, üstte "Yeni" badge, sağ üstte kalp (wishlist), alt yazı: ürün adı (2 line clamp), beden range chip, fiyat (indirim varsa eski fiyat çizgili).
   - Mobilde snap-x scroll, masaüstünde grid-cols-4.
5. **Editörden / Koleksiyonlar** (3 büyük kart):
   - "Okula Dönüş", "Doğum Günü Setleri", "Pijama Geceleri" gibi 3 koleksiyon. Her biri rounded-3xl, görsel + başlık + kısa metin + CTA.
6. **Trust band** (4 ikon + metin: Bedava Kargo, 30 Gün İade, Güvenli Ödeme, WhatsApp Destek) — layout'tan gelmiyorsa burada da göster.
7. **Müşteri yorumları** (3 testimonial card):
   - Yıldızlar + kısa yorum + müşteri adı + "Sipariş #XXX'ten" etiketi.
8. **Instagram showcase**:
   - 6'lı grid, @markaad #etiket lifestyle fotoğrafları (placeholder), "Bizi Instagram'da takip edin" CTA.

Üst: ViewData["Title"] = "Anasayfa".
JS gerekirse @section Scripts'te carousel için minimal vanilla scroll-snap, kütüphane ekleme.
```

---

## 4. `Views/Catalog/Category.cshtml` — Kategori Listeleme

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Bir kategori sayfası. ViewBag.Category (slug, name, description, heroImageUrl), ViewBag.Products (Pageable<StorefrontProductCardDto>: Items, TotalItemCount, CurrentPage, TotalPageCount), ViewBag.Query (StorefrontCatalogQuery: arama, sıralama, fiyat range, beden, renk, marka, yaş, cinsiyet, mevsim).

Layout:
1. **Breadcrumb**: Anasayfa / Kız / Tişört. Küçük punto, muted.
2. **Kategori başlığı** (banner):
   - Sol: H1 kategori adı + Fraunces italic alt cümle (description), sağ: hero görsel (aspect-[4/3]).
   - bg-cream, rounded-3xl, py-12 px-8, mb-8.
3. **Filtre + grid layout**:
   - Sol sidebar (md:w-72, sticky top-24, masaüstünde görünür; mobilde alt sayfada "Filtrele" sticky button → bottom sheet açar):
     * **Yaş aralığı** (chip seçim, çoklu): 0-3 ay, 3-6 ay, ..., 12-14.
     * **Beden** (chip): XS S M L XL veya sayısal (74, 80, 86, 92...). Modeling: kid sizes.
     * **Cinsiyet** (radio): Kız / Erkek / Unisex / Hepsi.
     * **Mevsim**: İlkbahar/Yaz/Sonbahar/Kış (chip).
     * **Renk** (renkli yuvarlak): pastel pembe, mavi, krem, beyaz, gri, sarı, yeşil — accessible aria-label "Pembe".
     * **Fiyat aralığı**: iki input min-max, altında range slider (custom).
     * **Marka** (checkbox liste, üstte arama input — Tom Select gibi).
     * **Sadece stokta olanlar** toggle.
     * **İndirimliler** toggle.
     * Sidebar üstünde "Filtreleri Temizle" linki + aktif filtre chip'leri.
   - Sağ ana alan:
     * Üst bar: "{TotalItemCount} ürün" + Sıralama dropdown (Tom Select; opsiyonlar: Önerilen, Yeniden Eskiye, Fiyat Artan, Fiyat Azalan, En Çok Satan).
     * Ürün grid: grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-6. Her kart yukarıda anasayfadaki gibi.
     * Sayfalama altta (1 ... 5 önceki-sonraki).
     * **Empty state**: ürün yoksa illustration + "Aradığınız ürünü bulamadık" + "Filtreleri temizle" CTA.

Üst: ViewData["Title"] = $"{ViewBag.Category.Name} | Zekids Bebe".
```

---

## 5. `Views/Catalog/Search.cshtml` — Arama Sonuçları

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Category sayfası ile aynı layout (sidebar + grid), tek farkları:
- Hero yerine üstte küçük arama özet bandı: "{Query} için {TotalItemCount} sonuç bulundu" + arama kutusu (sonuçları daraltmak için).
- Sonuç yoksa empty state özelleşir: "{Query} bulunamadı. Şunları denemek ister misiniz?" + popüler kategoriler chip listesi + popüler arama önerileri.
- Breadcrumb: Anasayfa / Arama.

@model yok (ViewBag.Query string, ViewBag.Products Pageable).
ViewData["Title"] = $"\"{ViewBag.Query}\" için arama sonuçları".
```

---

## 6. `Views/Catalog/Categories.cshtml` — Tüm Kategoriler

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model List<Entegrasyon.Entity.Dtos.Storefront.CategoryTreeDto>

Tüm kategori ağacını gezilebilir grid olarak göster.
- Üstte H1 "Tüm Kategoriler".
- 3 ana grup card (Kız/Erkek/Bebek), her birinde alt kategoriler 2 sütunlu liste + en altta "Tümünü Gör".
- Ana grup kartları rounded-3xl, pastel renk, sol üstte ikon, başlık Fraunces.
- Hover'da kart hafif yükselir.

ViewData["Title"] = "Tüm Kategoriler".
```

---

## 7. `Views/Catalog/SellerStore.cshtml` — Satıcı Mağazası

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Multi-tenant marketplace — bir satıcının ürünlerini gösteren mağaza sayfası. ViewBag.Seller (ad, logo, banner, açıklama, puan, ürün sayısı, takipçi, kuruluş yılı), ViewBag.Products (Pageable).

Layout:
1. **Seller cover** (banner):
   - Banner görsel (aspect-[16/5]), üstüne yarı-saydan overlay.
   - Sol altta: yuvarlak logo (-mt-12 ile çıkıntılı) + satıcı adı + puan (yıldızlı) + "Takip Et" buton.
   - Sağda: "Ürün: 245" "Takipçi: 1.2k" "Kuruluş: 2018" stat row.
2. **Tab'lar**: Ürünler / Hakkında / Yorumlar.
3. **Ürünler sekmesi**: Category sayfasındaki gibi filtre + grid (sade — sadece beden, fiyat, sort).
4. Hakkında ve Yorumlar sekmesi placeholder.

ViewData["Title"] = $"{ViewBag.Seller.Name} | Zekids Bebe".
```

---

## 8. `Views/Product/Detail.cshtml` — Ürün Detay

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model Entegrasyon.Entity.Dtos.Storefront.StorefrontProductDetailDto

Model alanları (varsay): Id, Name, SeoSlug, ShortDescription, Description (HTML), Price, OldPrice, DiscountPercent, Currency, Brand, BrandSlug, SellerName, SellerSlug, CategoryBreadcrumb (List<{Name,Slug}>), Images (List<string>), Variants (List<{Id, Size, Color, ColorHex, Stock, Price}>), AvailableSizes, AvailableColors, AgeRange (string), Gender, Season, Rating (decimal), ReviewCount, Stock, ShippingInfo (string), ReturnPolicy (string), SizeGuide (yaş↔boy↔kilo↔beden mapping), Tags, Reviews (List<{Author, Rating, Comment, Date, Verified}>), RelatedProducts (List<StorefrontProductCardDto>), BoughtTogether (List<...>).

Layout (responsive, 2 sütun → mobilde stack):
1. **Breadcrumb** (üstte).
2. **Üst grid** (lg:grid-cols-2 gap-12):
   - **Sol — Galeri**:
     * Büyük ana görsel (aspect-[4/5], rounded-2xl). Tıkla GLightbox ile büyüt.
     * Altta thumbnail strip (5-6 minik, aktif olan border-primary). Mobilde yatay scroll-snap.
     * Sol üst overlay'de "%XX İndirim" badge (varsa, accent sarı), sağ üstte kalp (wishlist toggle).
   - **Sağ — Bilgi paneli** (sticky lg:top-24):
     * Marka linki (küçük, link primary).
     * H1 ürün adı (Fraunces, text-3xl).
     * Rating row: yıldızlar + "(123 değerlendirme)" link.
     * Fiyat: indirim varsa eski fiyat çizgili gri + güncel fiyat büyük primary.
     * Stok rozeti: "Stokta" yeşil veya "Son 3 ürün" turuncu.
     * **Beden seçici**: chip grid; aktif olan primary border + tick. Yanında "Beden Tablosu" link → modal açar (size guide).
     * **Renk seçici**: yuvarlak renkli swatch + tooltip; aktif primary ring.
     * **Adet stepper**: - [1] + (rounded, border).
     * 2 CTA stack: "Sepete Ekle" (primary, full-width, rounded-full, py-3, font-semibold) + "Hemen Al" (outline, primary border).
     * Quick info satırları (ikonlu): Kargoda: tahmini teslimat tarihi, "Bedava kargo eligibility", "30 gün iade", "WhatsApp ile sor" linki.
     * Paylaş ikonları (WA, Facebook, link kopyala).
3. **Tab'lar** (mt-16):
   - Ürün Detayı (HTML description, prose typography)
   - Beden Rehberi (size guide table — yaş, boy cm, kilo kg, beden kodu)
   - İade & Kargo (markdown listesi)
   - Yorumlar (Reviews list — her satır: avatar, isim, "Doğrulanmış Alıcı" badge, yıldız, tarih, yorum metni; üstte özet: ortalama puan büyük + dağılım bar chart 5★ %60 gibi; "Yorum Yaz" CTA).
4. **Benzer Ürünler** (carousel, 8 kart) — anasayfadaki ürün kartı stilinde.
5. **Birlikte Alınanlar** (3 ürün + + + 3 ürün, "Sepete Ekle (3)" CTA pakete özel fiyatla).
6. **Son Gezdikleriniz** (carousel, kullanıcı recently-viewed JS'inden gelir — sample 6 kart).

**Beden tablosu modal**: ortalı modal, max-w-2xl, kapanma X, içinde:
- Açıklama paragrafı (çocuk bedenleri yaşa göre)
- Tablo (Yaş | Boy cm | Kilo kg | Beden)
- "Nasıl ölçülür?" küçük diyagram.

**Sticky mobil CTA bar**: mobilde alt sabit (bottom-0), fiyat + "Sepete Ekle" buton (full-width).

ViewData["Title"] = $"{Model.Name} | Zekids Bebe".
@section Scripts ile GLightbox init, beden modal toggle, sticky scroll handling.
```

---

## 9. `Views/Shared/_ProductCard.cshtml` — Tekrarlanan Ürün Kartı

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model Entegrasyon.Entity.Dtos.Storefront.StorefrontProductCardDto

Alanları (varsay): Id, Name, SeoSlug, Price, OldPrice, DiscountPercent, ImageUrl, ImageUrlHover (varsa), Brand, AgeRange, AvailableSizes (List<string>), Rating, IsNew, IsBestseller, IsOutOfStock, IsInWishlist.

Çıktı sadece kart (a tag root, /urun/@Model.SeoSlug'a link):
- aspect-[4/5] görsel kapsayıcı (rounded-2xl overflow-hidden bg-cream)
  * Default img, hover'da ImageUrlHover'a fade (varsa).
  * Sol üst köşe: badge stack — "Yeni" (primary), "Çok Satan" (accent), "%XX" (success).
  * Sağ üst köşe: kalp button (aria-label "Favorilere ekle"; tıkla toggle, IsInWishlist ise dolu).
  * Stoksuzsa overlay "Stokta Yok" karartmalı.
  * Hover'da görselin altına "Hızlı Bakış" button kayar (translate-y).
- Kart gövdesi (mt-3):
  * Marka satırı (text-xs muted).
  * Ürün adı (line-clamp-2, font-medium, text-charcoal).
  * Yaş range chip (text-xs, bg-cream).
  * Fiyat row: indirim varsa eski fiyat çizgili gri, sonra güncel fiyat primary font-semibold.
  * Beden chip listesi mini (en fazla 5, sonrası +N).

A11y: kart linkinin aria-label'i "Ürün adı, fiyat".
Hover: subtle scale-105 görsele.
```

---

## 10. `Views/Cart/Index.cshtml` — Sepet

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Model yok (ViewBag.Cart: Items (Id, ProductName, Image, Slug, Variant{Size,Color}, UnitPrice, OldUnitPrice, Quantity, Stock, LineTotal), SubTotal, ShippingFee, DiscountAmount, CouponCode, GiftCardAmount, Total, FreeShippingThreshold, FreeShippingRemaining).

Layout:
1. Breadcrumb + H1 "Sepetim ({n} ürün)".
2. **Free shipping progress bar** (üstte):
   - "Ücretsiz kargoya {X}₺ kaldı 🚚" (emoji yok — sade)
   - Progress bar (rounded-full, primary fill).
3. **Grid** (lg:grid-cols-3 gap-8):
   - **Sol (col-span-2)** — Ürün listesi:
     * Boş sepet durumu: illustration + "Sepetiniz henüz boş" + "Alışverişe Başla" CTA.
     * Her satır: 4 sütun layout (görsel 100x125, ürün bilgisi 2x, adet stepper, fiyat + sil).
     * Ürün bilgisi: ad (link), variant (Beden: M, Renk: Pembe), stok uyarısı (sadece son 2 vb.), "Daha sonra için kaydet" link.
     * Adet stepper: stok max'ı geçemez.
     * Fiyat: eski fiyat çizgili (varsa) + line total.
     * Sil button: trash ikonu, aria-label, hover red.
     * Liste başı: "Tümünü kaldır" linki sağda.
   - **Sağ (col-span-1, sticky lg:top-24)** — Sipariş özeti card (rounded-2xl bg-cream p-6):
     * Başlık "Sipariş Özeti".
     * Ara toplam, kargo, indirim, hediye kart, toplam (büyük primary).
     * Kupon input + "Uygula" button (aktif kupon varsa altta chip + X).
     * Hediye kart input + "Uygula".
     * "Ödemeye Geç" CTA (primary, full, py-4, rounded-full).
     * Güvenli ödeme rozetleri (SSL, Visa, MC, Troy ikonları minik).
     * Altta küçük disclaimer "KDV dahildir. Kargo: aynı gün kargoda ise yarın elinizde."
4. **Birlikte İyi Gider** önerileri (4 ürün).

Razor önemli: indirim hesaplama, free shipping progress yüzdesi inline. Boş sepet branch'i ayrı.
JS: adet değişim → form auto-submit veya fetch ile satır güncelleme; toast Notyf ile.
ViewData["Title"] = "Sepetim".
```

---

## 11. `Views/Checkout/Index.cshtml` — Ödeme

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Tek sayfa accordion-style 3 adımlı checkout. Üstte adım göstergesi (Stepper):
1) Adres  2) Kargo  3) Ödeme — aktif adım primary, tamamlanmış check ikonu.

ViewBag: Addresses (List<{Id,Title,FullName,Phone,Line1,District,City,Zip,IsDefault}>), CartSummary (aynı sepet özeti), ShippingOptions (List<{Id,Name,Price,EstimatedDays}>), PaymentMethods (List<{Id,Name,Logo,Description}>).

Layout (lg:grid-cols-3, sol col-span-2 form, sağ col-span-1 özet sticky):

**Adım 1 — Teslimat Adresi**:
- Adres kartları grid (mevcut adresler) — her kart radio seçim, başlık (Ev/İş/Anneannem), tam adres, "Düzenle" link, IsDefault badge.
- "+ Yeni Adres Ekle" kart — açıldığında inline form: ad-soyad, telefon (IMask 0(5XX) XXX XX XX), il (Tom Select), ilçe, mahalle, açık adres (textarea), posta kodu (IMask), "Kaydet ve Devam" CTA.
- "Faturayı farklı adrese gönder" toggle → ek adres seçimi.

**Adım 2 — Kargo Yöntemi**:
- Kart listesi (radio): Standart Kargo (1-3 iş günü) 29.90₺, Express (yarın elinizde) 49.90₺, Aynı Gün (İstanbul içi) 79.90₺.
- Hediye paketi (toggle): "+15₺" — kart UI altında alternatif renk seçimi (pembe/mavi/krem) + hediye notu textarea.

**Adım 3 — Ödeme**:
- Sekme: Kart, Havale/EFT, Kapıda Ödeme.
- **Kart**: kart numarası (IMask 16 hane), ad-soyad, son kullanma (MM/YY mask), CVV (3 hane mask), 3D Secure ile öde toggle, taksit seçenekleri (3,6,9,12 ay buton row, kart bin'ine göre dinamik dolacak).
- **Havale**: hesap bilgileri kartı + açıklama text.
- **Kapıda**: ek ücret notu + "+10₺".
- "Mesafeli Satış Sözleşmesi ve Ön Bilgilendirme'yi okudum onaylıyorum" checkbox.
- "Siparişi Tamamla" CTA (primary, full, py-4).

**Sağ özet** (sticky lg:top-24):
- Ürün thumbnail listesi (mini, 50x60 görsel + ad + adet + fiyat).
- Ara toplam, kargo, indirim, hediye paketi, toplam.
- Güvenli ödeme rozetleri.

Mobil: özet en üste çekilebilir collapse.
ViewData["Title"] = "Ödeme".
```

---

## 12. `Views/Checkout/Basarili.cshtml` — Başarılı Sipariş

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.OrderId, ViewBag.OrderNumber, ViewBag.EstimatedDelivery, ViewBag.TrackingUrl, ViewBag.Email.

Tek ortalı card, max-w-2xl, py-16:
- Üstte yumuşak success ikonu (büyük yuvarlak yeşil pastel + check). Hafif "konfeti" SVG arka plan — abartısız.
- H1 Fraunces: "Siparişiniz alındı, teşekkürler!".
- Alt paragraf: "Sipariş #ORDER_NUMBER · Onay e-postası {Email} adresine gönderildi."
- Bilgi kartı (rounded-2xl bg-cream p-6):
  * Tahmini teslimat: EstimatedDelivery (örn. "5-7 Haziran Perşembe").
  * Sipariş takibi: TrackingUrl bağlantısı + "Siparişi Takip Et" button.
- 2 CTA: "Hesabıma Git" (primary), "Alışverişe Devam" (outline).
- Altta "İstediğiniz ürünü bulamadınız mı? WhatsApp ile sorun" küçük not.
- Önerilen ürünler bölümü (Anasayfa carousel'ı gibi 8 kart).

ViewData["Title"] = "Siparişiniz Alındı".
```

---

## 13. `Views/Checkout/başarısız.cshtml` — Başarısız Ödeme

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.ErrorCode, ViewBag.ErrorMessage.

Empathic, suçlayıcı olmayan ton.
Ortalı card max-w-2xl:
- Üstte yumuşak warning ikonu (sarı pastel + ünlem).
- H1: "Ödemenizi tamamlayamadık".
- Açıklama paragrafı: hata mesajı insan diliyle (kart limiti yetersiz, 3D doğrulanmadı, banka reddetti vs.).
- "Ne yapabiliriz?" listesi (ikonlu):
  * Kart bilgilerini kontrol edin
  * Farklı bir kart veya banka deneyin
  * Bankanızı arayarak limit/online işlem kontrolü yapın
  * Havale/EFT veya kapıda ödeme seçin
- 2 CTA: "Tekrar Dene" (primary, ödeme adımına döner), "Sepete Dön" (outline).
- Altta küçük "Yardım gerekirse WhatsApp/Telefon ile bize ulaşın" satırı.

ViewData["Title"] = "Ödeme Tamamlanamadı".
```

---

## 14. `Views/Auth/Login.cshtml` — Giriş

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Split layout (min-h-[80vh], lg:grid-cols-2):
- **Sol** (lg only): pastel pembe arka plan, mutlu çocuk illustration veya yumuşak fotoğraf, alt köşede Fraunces italic slogan "Minik dolaplara büyük ilham." + 3 trust icon row (kargo bedava, iade, güvenli).
- **Sağ**: form ortalı card max-w-md:
  * Logo (üstte küçük).
  * H1 "Tekrar Hoş Geldiniz" + alt "Hesabınıza giriş yapın".
  * Form: email input, şifre input (göz toggle ile göster/gizle), "Beni hatırla" checkbox (sol) + "Şifremi unuttum" link (sağ).
  * "Giriş Yap" button (primary, full, py-3, rounded-full).
  * Divider "veya".
  * SSO row: Google ve Apple butonları (outline, ikon + metin).
  * Altta "Hesabınız yok mu? Hemen Kayıt Olun" link.

ViewBag.ReturnUrl varsa form action'a parametre olarak ekle.
ViewData["Title"] = "Giriş Yap".
```

---

## 15. `Views/Auth/Register.cshtml` — Kayıt

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Login ile aynı split layout. Sağ formda:
- H1 "Aramıza Hoş Geldiniz" + alt "Hesap oluşturarak hızlı sipariş ve kişisel önerilere erişin".
- Form alanları:
  * Ad / Soyad (yan yana, sm:grid-cols-2)
  * E-posta
  * Telefon (IMask, opsiyonel)
  * Şifre (göz toggle, snippet strength indicator alt çizgi)
  * Şifre tekrar
  * Doğum tarihi (opsiyonel, Flatpickr TR locale) — "Doğum gününüzde sürpriz bizden"
  * "Pazarlama e-postaları almak istiyorum" checkbox (opt-in)
  * "Kullanım Koşulları ve KVKK Aydınlatma Metni'ni kabul ediyorum" checkbox (zorunlu)
- "Hesap Oluştur" button (primary, full).
- Divider + SSO.
- Altta "Zaten üye misiniz? Giriş yapın".

ViewData["Title"] = "Kayıt Ol".
```

---

## 16. `Views/Auth/ForgotPassword.cshtml`

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Ortalı card, max-w-md, py-16:
- Yumuşak ikon (anahtar veya soru işareti).
- H1 "Şifremi Unuttum".
- Açıklama: "E-posta adresinizi girin, size sıfırlama bağlantısı gönderelim."
- Input: e-posta.
- Button: "Bağlantı Gönder" (primary full).
- Altta "Giriş ekranına dön" link.

ViewData["Title"] = "Şifremi Unuttum".
```

---

## 17. `Views/Auth/ResetPassword.cshtml`

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Ortalı card max-w-md:
- H1 "Yeni Şifre Belirle".
- Açıklama: "Hesabınız için yeni bir şifre belirleyin."
- Form: Yeni şifre (göz toggle, strength bar), Şifre tekrar.
- Güvenli şifre ipucu listesi (en az 8 karakter, 1 büyük harf, vs.) — gerçek zamanlı check.
- "Şifreyi Güncelle" button.
- Altta "Giriş ekranına dön" link.

ViewBag.Token gizli input olarak korunur.
ViewData["Title"] = "Yeni Şifre Belirle".
```

---

## 18. `Views/Auth/ConfirmEmail.cshtml`

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model string (sonuç durumu: "success" / "expired" / "invalid")

Ortalı card max-w-md:
- @model'e göre 3 durum:
  * success: yeşil check ikonu, "E-posta adresiniz onaylandı 🎉" (emoji yok), "Hoş geldiniz! Artık hesabınızı kullanabilirsiniz." + "Hesabıma Git" CTA.
  * expired: turuncu warning, "Bu doğrulama bağlantısının süresi doldu" + "Yeni bağlantı iste" CTA.
  * invalid: kırmızı X, "Bağlantı geçersiz" + "Giriş ekranına dön".

ViewData["Title"] = "E-posta Doğrulama".
```

---

## 19. `Views/Auth/TwoFactor.cshtml`

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Ortalı card max-w-md:
- H1 "İki Aşamalı Doğrulama".
- Açıklama: "Uygulamanızdan 6 haneli kodu girin." veya "SMS ile gelen kodu girin."
- 6 ayrı kutucuk OTP input (her birine 1 hane, otomatik focus geçişi).
- Geri sayım: "Kodu yeniden gönder (00:42)" disable, süre bitince aktif.
- "Doğrula" button (primary).
- "Yöntem değiştir" link (SMS ↔ Authenticator).

ViewData["Title"] = "İki Aşamalı Doğrulama".
@section Scripts'te OTP auto-focus + paste handler.
```

---

## 20. `Views/Account/_AccountSidebar.cshtml` — Hesap Sidebar Partial

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model string  (aktif olan menü item key: "dashboard", "orders", "addresses", "profile", "security", "wallet", "loyalty", "returns", "referral", "buyagain", "wishlist").

Sticky sol sidebar (lg:w-72, lg:top-24):
- Üstte kullanıcı kart: avatar (yuvarlak, baş harfler fallback), ad-soyad, "Bronze/Silver/Gold üye" rozeti, "Profili Düzenle" mini link.
- Menü grupları:
  * "Hesabım": Özet (dashboard), Siparişlerim, İadeler, Yine Al, Favorilerim
  * "Cüzdan & Puan": Cüzdanım, Sadakat Puanları, Arkadaşını Davet Et
  * "Ayarlar": Profil Bilgileri, Adreslerim, Güvenlik, Şifre Değiştir, 2FA
  * En altta: Çıkış Yap (form post, kırmızı outline)
- Her item: ikon (lucide outline) + label; aktif olan bg-primary/10 + text-primary + sol border-l-2 primary.
- Mobilde: sayfanın üstünde collapse menü (sol drawer veya bottom sheet).

ViewData["Title"] hesap sayfalarında varsayılan değil — her sayfa kendisi belirler.
```

---

## 21. `Views/Account/Index.cshtml` — Hesap Özeti (Dashboard)

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.User (FirstName, LastName, Email, MemberSince, Tier), ViewBag.ActiveOrders (count + son 3 sipariş), ViewBag.Wallet (balance), ViewBag.LoyaltyPoints (points + nextTierAt), ViewBag.WishlistCount, ViewBag.Coupons (List<{Code,Discount,Expires}>).

Layout: 2 sütun (sol _AccountSidebar partial, sağ içerik).

İçerik:
1. Selamlama: "Merhaba {FirstName}" (Fraunces text-3xl) + "Üyelik: {Tier}" rozet.
2. Hızlı kart grid (md:grid-cols-4 gap-4):
   - Aktif Sipariş: count + "Tümünü gör →"
   - Cüzdan Bakiyesi: ₺ tutar + "Detay →"
   - Sadakat Puanı: puan + progress bar bir sonraki seviyeye
   - Favoriler: count + "Tümünü gör →"
3. **Aktif Kuponlar**: yatay liste — her kupon kart: "-100₺" büyük + kod + "31 Aralık'a kadar" + "Kopyala" button.
4. **Son Siparişler** (3 satır): sipariş no, tarih, durum chip, ürün thumbnail row, toplam, "Detay" CTA.
5. **Önerilen** (4 ürün kartı): "Bunlar ilginizi çekebilir".
6. **Yardım**: "Sorun mu yaşıyorsunuz? WhatsApp / İletişim" CTA satırı.

@Html.Partial("_AccountSidebar", "dashboard")
ViewData["Title"] = "Hesabım".
```

---

## 22. `Views/Account/Orders.cshtml` — Siparişlerim

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Orders (List<{Id, OrderNumber, Date, Status, StatusLabel, Total, ItemCount, ItemThumbs (List<string>)}>), ViewBag.Filters (status filter chips).

Layout: sidebar + ana.
İçerik:
1. H1 "Siparişlerim" + sağda arama input (sipariş no).
2. Filter chip bar: Tümü / Beklemede / Hazırlanıyor / Kargoda / Teslim Edildi / İptal / İade.
3. Sipariş listesi card stack (her kart rounded-2xl border):
   - Üst row: "Sipariş #ORD-2026-1234" · 23 Mayıs 2026 · {Status chip — Hazırlanıyor mavi, Kargoda accent, Teslim Edildi success, İptal danger}
   - Orta: 3-4 thumbnail strip + "+N daha".
   - Alt row: "{ItemCount} ürün · {Total}₺" + "Detay" button (primary outline) + Kargoda ise "Takip Et" link.
   - Teslim edildiyse "Tekrar Sipariş Et" + "İade Talep Et" mini link'leri.
4. Pagination veya "Daha Fazla Göster" infinite scroll.
5. Boş durum: "Henüz sipariş vermediniz" illustration + "Alışverişe Başla" CTA.

@Html.Partial("_AccountSidebar", "orders")
ViewData["Title"] = "Siparişlerim".
```

---

## 23. `Views/Account/OrderDetail.cshtml` — Sipariş Detayı

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Order (OrderNumber, Date, Status, Items (List<{Name, Image, Slug, Variant, Quantity, UnitPrice, LineTotal}>), Address, ShippingMethod, PaymentMethod, SubTotal, ShippingFee, Discount, Total, Timeline (List<{Step, Date, IsCompleted}>), TrackingUrl, TrackingNumber, CanCancel, CanReturn, InvoiceUrl).

Layout: sidebar + ana.
İçerik:
1. Üst: "Sipariş #ORDER" + tarih + sağda primary action button'lar: "Faturayı İndir", "İade Talep Et" (CanReturn), "Siparişi İptal Et" (CanCancel).
2. **Status timeline** (yatay desktop, dikey mobil): 5 adım (Alındı → Onaylandı → Hazırlanıyor → Kargoda → Teslim Edildi). Tamamlanan: primary dolu daire + check, aktif: pulse anim, sonraki: gri. Her step altında tarih.
3. **Kargo takip** (eğer kargoda): kargo firması logo + takip no + "Takip Et" CTA.
4. **Ürünler** card: her satır görsel + ad + variant + adet + fiyat + "Yorum Yaz" / "Tekrar Al" mini link'ler.
5. 2-col alt: **Teslimat Adresi** kart + **Ödeme Bilgisi** kart (kart son 4 hane veya havale).
6. **Özet** sağ kolon: ara toplam, kargo, indirim, toplam.

@Html.Partial("_AccountSidebar", "orders")
ViewData["Title"] = $"Sipariş #{Order.OrderNumber}".
```

---

## 24. `Views/Account/Addresses.cshtml` — Adreslerim

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Addresses (List<{Id, Title, FullName, Phone, Line1, District, City, Zip, IsDefault}>).

Layout: sidebar + ana.
İçerik:
1. H1 "Adreslerim" + sağda "+ Yeni Adres" CTA.
2. Kart grid (md:grid-cols-2 gap-4):
   - Her kart rounded-2xl border p-6:
     * Üstte başlık (Ev / İş / Anneanne) + IsDefault ise "Varsayılan" badge primary
     * Ad soyad bold
     * Telefon (formatlı)
     * Adres satırları
     * Alt sağ: "Düzenle" + "Sil" + (default değilse "Varsayılan Yap") mini link'ler
3. Yeni adres modal'ı veya inline drawer: form (Checkout adres formu ile aynı).
4. Boş durum: "Henüz adres eklemediniz" + "Adres Ekle" CTA.

@Html.Partial("_AccountSidebar", "addresses")
ViewData["Title"] = "Adreslerim".
```

---

## 25. `Views/Account/Profile.cshtml` — Profil

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.User (Ad, Soyad, Email, Phone, BirthDate, Gender, AvatarUrl, NewsletterOptIn).

Layout: sidebar + ana.
İçerik:
1. H1 "Profil Bilgileri".
2. **Avatar bölümü**: yuvarlak avatar (büyük), "Fotoğraf Değiştir" + "Kaldır" link.
3. Form (2 col grid):
   - Ad, Soyad
   - E-posta (readonly veya değiştir CTA → ayrı ekran)
   - Telefon (IMask)
   - Doğum tarihi (Flatpickr)
   - Cinsiyet (radio: Kadın/Erkek/Belirtmek istemiyorum)
4. **Çocuklarım** bölümü (çocuk giyim için özel — opsiyonel "ailedeki çocuklar"):
   - Liste: her çocuk için ad, doğum tarihi, beden takibi.
   - "+ Çocuk Ekle" CTA → modal: ad, doğum tarihi, cinsiyet.
   - Açıklama: "Çocuklarınızı ekleyin, beden öneri ve yaş-uygun ürünleri kişiselleştirelim."
5. Newsletter toggle.
6. "Değişiklikleri Kaydet" CTA + "İptal".

@Html.Partial("_AccountSidebar", "profile")
ViewData["Title"] = "Profil Bilgileri".
```

---

## 26. `Views/Account/Security.cshtml` — Güvenlik

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.User (LastPasswordChange, TwoFactorEnabled, ActiveSessions (List<{Device, Location, LastSeen, IsCurrent}>), LoginAlerts).

Layout: sidebar + ana.
İçerik:
1. H1 "Güvenlik".
2. Kart 1 — Şifre: "Son değişiklik: 3 ay önce" + "Şifreyi Değiştir" CTA → ChangePassword sayfasına.
3. Kart 2 — 2FA: durum (Açık/Kapalı), açıksa "SMS ile" veya "Authenticator", "Yapılandır" / "Kapat" CTA → TwoFactorSetup sayfasına.
4. Kart 3 — Giriş bildirimleri: toggle "Yeni cihazdan giriş yapıldığında bana e-posta gönder".
5. Kart 4 — Aktif Oturumlar: liste — cihaz, konum, son aktivite, "Bu cihaz" badge varsa. Diğerleri için "Çıkışı Zorla" mini CTA.
6. Kart 5 — Veri & Hesap: "Verilerimi indir" + "Hesabımı sil" (kırmızı outline, modal onayı).

@Html.Partial("_AccountSidebar", "security")
ViewData["Title"] = "Güvenlik".
```

---

## 27. `Views/Account/ChangePassword.cshtml` — Şifre Değiştir

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Layout: sidebar + ana, form ortalı max-w-lg.
İçerik:
1. H1 "Şifre Değiştir".
2. Form: Mevcut şifre, Yeni şifre (göz toggle + strength bar), Yeni şifre tekrar.
3. Şifre kuralları gerçek zamanlı check listesi.
4. "Şifreyi Güncelle" CTA + "İptal".

@Html.Partial("_AccountSidebar", "security")
ViewData["Title"] = "Şifre Değiştir".
```

---

## 28. `Views/Account/TwoFactorSetup.cshtml` — 2FA Kurulum

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.QrCodeUrl, ViewBag.SecretKey, ViewBag.RecoveryCodes (List<string>).

Layout: sidebar + ana.
İçerik adımları (stepper):
1. **Yöntem Seç**: kart row — Authenticator (önerilen, "telefon ikonu"), SMS (telefon numarası kullan).
2. **Kurulum** (Authenticator seçildiyse):
   - QR kod büyük ortalı kart.
   - Altta secret key kopyalanabilir mono font + "Kopyala" button.
   - Açıklama: "Google Authenticator, Authy, 1Password gibi uygulamanızla QR'ı tarayın."
3. **Doğrula**: 6 haneli OTP input (kayıt ekranındaki gibi).
4. **Yedek Kodlar**: 10 kodluk grid, "İndir" + "Kopyala" CTA, "Bu kodları güvenli bir yerde saklayın" uyarı.
5. "2FA'yı Aktifleştir" CTA.

@Html.Partial("_AccountSidebar", "security")
ViewData["Title"] = "İki Aşamalı Doğrulama Kurulumu".
```

---

## 29. `Views/Account/Returns.cshtml` — İadeler

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Returns (List<{Id, OrderNumber, Date, Status, ItemThumbs, RefundAmount, TrackingNumber}>).

Layout: sidebar + ana.
İçerik:
1. H1 "İadelerim" + sağda "+ Yeni İade" CTA → CreateReturn.
2. Filtre chip: Tümü / Talep Edildi / Onaylandı / Kargoda / Tamamlandı / Reddedildi.
3. Liste kartları:
   - Sol thumbnail strip
   - Orta: Sipariş #, talep tarihi, durum chip
   - Sağ: tutar + "Detay" CTA
4. Boş: "Henüz iade talebiniz yok" + "İade nasıl çalışır?" mini açıklama kartı.

@Html.Partial("_AccountSidebar", "returns")
ViewData["Title"] = "İadelerim".
```

---

## 30. `Views/Account/CreateReturn.cshtml` — İade Talebi Oluştur

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Order (Items (List<{Id, Name, Image, Variant, Quantity, UnitPrice}>) + OrderNumber).

Layout: sidebar + ana.
İçerik:
1. H1 "İade Talebi Oluştur" + alt "Sipariş #{OrderNumber}".
2. Adım 1 — **Ürün Seç**: checkbox liste — her satır görsel, ad, variant, adet seçici (max sipariş adedi), satır toplamı.
3. Adım 2 — **Sebep**: radio liste (Beden uymadı, Beklediğim gibi değil, Hasarlı geldi, Yanlış ürün, Diğer). "Diğer" seçilirse textarea.
4. Adım 3 — **Çözüm**: radio (İade et / Değişim talep et — varsa).
5. Adım 4 — **Fotoğraf yükle** (drag-drop, opsiyonel, hasar/yanlış ürün için): preview thumbnail grid + sil.
6. Adım 5 — **İade adresi**: mevcut adres seç veya yeni ekle.
7. Özet card sağ tarafta sticky: seçilen ürünler + iade tutarı tahmini + kargo notu ("Kargo ücreti tarafımızca karşılanır").
8. "İade Talebi Oluştur" CTA.

@Html.Partial("_AccountSidebar", "returns")
ViewData["Title"] = "İade Talebi Oluştur".
```

---

## 31. `Views/Account/Wallet.cshtml` — Cüzdan

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Wallet (Balance, Transactions (List<{Date, Type ("topup","spend","refund","cashback"), Amount, Description, RelatedOrderNumber})).

Layout: sidebar + ana.
İçerik:
1. **Bakiye kartı** büyük (rounded-3xl bg-gradient-to-br primary/secondary, text-white, py-12):
   - "Cüzdan Bakiyesi" küçük etiket
   - Tutar Fraunces text-5xl
   - "Bakiye Yükle" CTA (white button) + "Çek" outline white.
2. Hızlı kart row (3 col): Toplam yüklenen, Toplam harcanan, Cashback kazancı.
3. **Hareketler** tablosu:
   - Filtre chip: Tümü / Yükleme / Harcama / İade / Cashback.
   - Liste (timeline UI): her satır tarih, açıklama, sağda tutar (+/- işaretli, yeşil/kırmızı).
   - Tıkla satır genişle: ilişkili sipariş linki.
4. Boş: "Henüz işleminiz yok" + "Bakiye Yükle" CTA.

@Html.Partial("_AccountSidebar", "wallet")
ViewData["Title"] = "Cüzdanım".
```

---

## 32. `Views/Account/LoyaltyPoints.cshtml` — Sadakat Puanları

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Loyalty (Points, Tier (Bronze/Silver/Gold), NextTier, NextTierAtPoints, EarningRules (List<{Action, Points}>), History (List<{Date, Action, Points}>)).

Layout: sidebar + ana.
İçerik:
1. **Puan kartı** büyük (gradient): "1.250 Puan" + Tier rozeti + bir sonraki tier'a progress bar ("750 puana daha 'Gold' olun").
2. **Nasıl Kazanılır?** kart grid (3-4 kart):
   - "Alışveriş Yap": 1₺ = 1 puan
   - "Yorum Yaz": +50 puan / yorum
   - "Arkadaşını Davet Et": +200 puan / üye olunan davet
   - "Doğum Günün": +500 puan (yıllık)
3. **Nasıl Kullanılır?** mini kart: "100 puan = 10₺ indirim" + ödeme adımında uygulanır açıklaması.
4. **Puan Geçmişi** liste: tarih, açıklama, +/− puan.

@Html.Partial("_AccountSidebar", "loyalty")
ViewData["Title"] = "Sadakat Puanlarım".
```

---

## 33. `Views/Account/Referral.cshtml` — Arkadaş Davet Et

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Referral (Code, InviteLink, InvitedCount, RewardEarned, InvitedFriends (List<{Email, JoinedAt, Status})).

Layout: sidebar + ana.
İçerik:
1. **Hero kart** (rounded-3xl bg-primary/10 p-10):
   - H1 "Arkadaşını Çağır, İkiniz Kazanın".
   - Açıklama: "Davet kodunuzla kayıt olan arkadaşınız ilk siparişine 100₺ indirim, siz 200 puan kazanırsınız."
   - **Davet kodu kutu**: mono font büyük + "Kopyala" button (Notyf toast "Kopyalandı!").
   - **Davet linki** kutu altta: input readonly + "Kopyala".
   - Paylaş row (4 button): WhatsApp, Email, X/Twitter, Link kopyala.
2. **İstatistik row** (3 kart): Davet ettiklerim, Kayıt olanlar, Toplam kazanım.
3. **Davet Listem** tablo: email, davet tarihi, durum (Bekliyor / Kayıt Oldu / İlk Sipariş Verdi).

@Html.Partial("_AccountSidebar", "referral")
ViewData["Title"] = "Arkadaşını Davet Et".
```

---

## 34. `Views/Account/BuyAgain.cshtml` — Yine Al

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Products (List<StorefrontProductCardDto> — daha önce alınan ürünler).

Layout: sidebar + ana.
İçerik:
1. H1 "Yine Al" + alt "Daha önce sevdiğiniz ürünleri tekrar sipariş edin".
2. Filter chip: Tümü / Bu sezon / Bedeni değişmedi / Stoğa düştü.
3. Ürün grid (Product card, anasayfadaki gibi) + her kartın altına "Yeniden Sepete Ekle" mini CTA.
4. Boş: "İlk siparişinizden sonra burada eski favorileriniz görünecek."

@Html.Partial("_AccountSidebar", "buyagain")
ViewData["Title"] = "Yine Al".
```

---

## 35. `Views/Wishlist/Index.cshtml` — Favoriler

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Products (List<StorefrontProductCardDto>).

Layout: sidebar + ana (veya tam genişlik, layout tercih senin).
İçerik:
1. H1 "Favorilerim ({n} ürün)".
2. Üst sağ: "Listeyi Paylaş" (link kopyala / WA) + "Tümünü Sepete Ekle" CTA (stokta olanları).
3. Ürün grid (Product card) + her kartta kalp dolu varsayılan (kaldırılabilir).
4. Boş: "Favoriler listeniz boş" illustration + "Alışverişe başla" CTA.

@Html.Partial("_AccountSidebar", "wishlist")  // sidebar'ı kullanıyorsa
ViewData["Title"] = "Favorilerim".
```

---

## 36. `Views/Compare/Index.cshtml` — Karşılaştırma

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model List<Entegrasyon.Entity.Dtos.Storefront.StorefrontProductDetailDto>

Yan yana ürün karşılaştırma tablosu (max 4 ürün).
Layout:
1. H1 "Ürünleri Karşılaştır".
2. Sticky üst kart row: 4 sütun, her sütunda ürün — görsel, ad, fiyat, "Sepete Ekle" mini CTA, sağ üst X (listeden çıkar).
3. Karşılaştırma matrix tablosu (her satır bir özellik, kalın satır başlığı sol):
   - Marka
   - Yaş aralığı
   - Beden
   - Renk seçenekleri
   - Kumaş içeriği
   - Yıkama bilgisi
   - Üretim yeri
   - Sezon
   - Rating
4. Boş durum: "Karşılaştıracak ürün eklemediniz" + "Kataloga göz at".
5. < 2 ürün varsa boş kart: "+ Ürün Ekle".

ViewData["Title"] = "Karşılaştır".
```

---

## 37. `Views/Seller/Register.cshtml` — Satıcı Kayıt

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Multi-tenant marketplace — satıcı olmak isteyenler için başvuru formu.

Layout: ortalı max-w-3xl, py-12:
1. Üstte sade hero: "Satıcı Olun, Türkiye'ye ulaşın" Fraunces + alt açıklama + 3 fayda chip ("Kolay Liste", "Düşük Komisyon", "Pazarlama Desteği").
2. Form (gruplar accordion veya tek sayfa):
   - **Firma Bilgileri**: Firma adı, vergi no/TC, vergi dairesi, MERSİS, Şahıs/Limited radio.
   - **İletişim**: Yetkili ad-soyad, e-posta, telefon (IMask), KEP.
   - **Mağaza**: Mağaza adı (slug otomatik), logo upload (drag-drop), kısa açıklama (textarea, 280 karakter).
   - **Banka**: IBAN (mask), hesap sahibi.
   - **Kategoriler**: Tom Select multi (Kız giyim, Erkek giyim, Bebek, Aksesuar, Ayakkabı).
   - **Belgeler**: Vergi levhası upload, imza sirküleri upload, ticaret sicil upload.
   - "Satıcı Sözleşmesi'ni okudum" checkbox.
3. "Başvuruyu Gönder" CTA.

ViewData["Title"] = "Satıcı Başvurusu".
```

---

## 38. `Views/Seller/Pending.cshtml` — Onay Bekliyor

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Seller (Name, AppliedAt, EstimatedReviewDays).

Layout: ortalı card max-w-2xl, py-16:
- Üstte yumuşak saat ikonu (mavi pastel).
- H1 "Başvurunuz Alındı".
- "{Seller.Name} mağazanız için başvurunuzu inceliyoruz. Genellikle 2-3 iş günü içinde sonuçlanır."
- Stepper timeline (4 adım): Başvuru Alındı (check) → İnceleniyor (aktif pulse) → Onay → Mağaza Aktif.
- "Bilgilerimi Güncelle" + "İletişim" CTA.
- Önerilen kart: "Bu süreçte bizi tanıyın — Satıcı Rehberi"

ViewData["Title"] = "Başvurunuz İnceleniyor".
```

---

## 39. `Views/Seller/Panel.cshtml` — Satıcı Paneli (Dashboard)

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Metrics (TodayRevenue, TodayOrders, ActiveProducts, PendingOrders, AvgRating, Conversion), ViewBag.Chart (last 7 days revenue), ViewBag.RecentOrders (5), ViewBag.TopProducts (5).

Layout: satıcı sidebar (Panel, Ürünlerim + Ekle, Siparişlerim, Bakiye, Profil, Çıkış) + ana içerik.

İçerik:
1. H1 "Mağaza Paneli" + tarih range picker (Flatpickr).
2. **Metric grid** (md:grid-cols-3 lg:grid-cols-6 gap-4): kartlar — başlık, büyük sayı, küçük trend chip (+8% yeşil okla).
3. **Gelir grafiği** (7 günlük çizgi grafik — CSS-only veya inline SVG, basit polyline).
4. **Aksiyon gerektirenler** kart (turuncu border): "5 yeni siparişiniz var" + "Görüntüle" CTA.
5. **Son siparişler** tablo (sipariş no, alıcı, ürün adedi, tutar, durum chip, eylem).
6. **En çok satan ürünler** liste (görsel + ad + adet + gelir).

ViewData["Title"] = "Mağaza Paneli".
```

---

## 40. `Views/Seller/Profile.cshtml` — Satıcı Profili

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Seller info düzenleme — Account/Profile gibi ama satıcı odaklı: mağaza adı, logo, banner, açıklama, kategoriler, kargo politikası, iade politikası, iletişim, sosyal medya.
Layout: satıcı sidebar + ana, 2-col form.

ViewData["Title"] = "Mağaza Profili".
```

---

## 41. `Views/Seller/Orders.cshtml` — Satıcı Siparişleri

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Account/Orders gibi liste ama satıcı için — durum filtreleri Bekleyen / Onaylandı / Hazırlanıyor / Kargoda / Tamamlandı / İptal / İade. Her satırda "Hazırlandı" / "Kargoya Ver" / "İptal" inline aksiyon button'ları.

Layout: satıcı sidebar + ana.
ViewData["Title"] = "Siparişler".
```

---

## 42. `Views/Seller/OrderDetail.cshtml` — Satıcı Sipariş Detayı

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model Entegrasyon.Entity.Orders.Order

Order entity'sinden satıcının görmesi gereken alanları kullan: OrderNumber, OrderDate, OrderStatus, Items (ProductName, Quantity, UnitPrice, Variant), Customer (FullName, MaskedPhone), ShippingAddress, ShippingMethod, ShippingCost, SubTotal, Total, Notes.

Layout: satıcı sidebar + ana.
İçerik:
1. Üstte sipariş başlığı + durum chip + aksiyon button row: "Hazırlandı Olarak İşaretle", "Kargo Bilgisi Gir" (modal), "Faturayı Görüntüle", "İade Talebi Aç".
2. Sol col: Ürünler tablo (Account/OrderDetail'daki gibi).
3. Sağ col: Müşteri & Adres kart, Ödeme kart, Notlar kart.
4. Kargo modal: kargo firma seç (Tom Select), takip no input, "Kaydet ve Müşteriye Bildir".

ViewData["Title"] = $"Sipariş #{Model.OrderNumber}".
```

---

## 43. `Views/Seller/Balance.cshtml` — Satıcı Bakiye

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Balance (Available, Pending, TotalEarned, NextPayoutDate), ViewBag.Transactions (List<{Date, OrderNumber, GrossAmount, Commission, NetAmount, Status}>).

Layout: satıcı sidebar + ana.
İçerik:
1. Bakiye kartı (Account/Wallet'a benzer ama satıcıya özel). 3 sub-stat: Kullanılabilir, Beklemede, Toplam Kazanç.
2. "Çekim Talep Et" CTA (büyük primary).
3. Komisyon özet kartı (%X komisyon oranı).
4. Hareketler tablosu: tarih, sipariş no, brüt, komisyon, net, durum.

ViewData["Title"] = "Bakiye & Ödemeler".
```

---

## 44. `Views/SellerProduct/Index.cshtml` — Satıcı Ürünleri

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model List<Entegrasyon.Entity.SellerProduct>

Layout: satıcı sidebar + ana.
İçerik:
1. Üst bar: H1 "Ürünlerim" + sağda "+ Yeni Ürün" CTA + arama input + filtre (Aktif / Pasif / Stok bitti / Onay bekliyor).
2. Tablo:
   - Sütunlar: Görsel | Ürün adı | SKU | Fiyat | Stok | Durum chip | Eylem (düzenle/sil dropdown).
   - Satır hover bg-cream/50.
   - Toplu seçim checkbox + "Seçilenleri pasif yap / sil" sticky bar.
3. Boş: "Henüz ürün eklemediniz" + "İlk Ürünü Ekle" CTA.

ViewData["Title"] = "Ürünlerim".
```

---

## 45. `Views/SellerProduct/Add.cshtml` — Yeni Ürün Ekle

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@model List<Entegrasyon.Entity.Products.Product>  (mevcut catalog product'ları mı dropshipping mantığı mı bilemedim; placeholder kullan)

Layout: satıcı sidebar + ana. Wizard 4 adım üst stepper.

**Adım 1 — Temel Bilgiler**:
- Ürün adı
- Marka (Tom Select)
- Kategori (Tom Select, ağaç — Kız → Üst Giyim → Tişört)
- Yaş aralığı (chip multiselect)
- Cinsiyet (radio)
- Mevsim (chip multi)
- Kısa açıklama (textarea)
- Uzun açıklama (basit rich text — bold, italic, list)

**Adım 2 — Görseller & Medya**:
- Drag-drop multi upload (ilk yüklenen kapak).
- SortableJS ile sıralama.
- Her görsele alt text input.

**Adım 3 — Varyantlar & Fiyat**:
- Bedenler chip seç (74,80,86... veya XS,S,M...)
- Renkler input (renk adı + hex picker)
- Bedenler × renkler matrix tablo otomatik oluştur (her satır: SKU input, fiyat, eski fiyat, stok, barkod).
- Toplu fiyat/stok düzenle button.

**Adım 4 — SEO & Yayın**:
- SEO slug
- Meta title, meta description (karakter sayıcı).
- Etiketler (Tom Select, virgüllü).
- "Hemen yayınla" / "Taslak kaydet" radio.
- "Kaydet" CTA.

Sağ kolonda sticky preview kart (girilen bilgilerden mini ürün kartı önizleme).
ViewData["Title"] = "Yeni Ürün Ekle".
```

---

## 46. `Views/GiftCard/Index.cshtml` — Hediye Çeki Satın Alma

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Designs (List<{Id, Name, ImageUrl}>) — tema kartları (Doğum Günü, Bebek Hoş Geldin, Bayram, Açılış vs.).

Layout: ortalı max-w-4xl, py-12:
1. **Hero kart** (pastel pembe arka plan):
   - H1 Fraunces: "Çocuk Atölyesi Hediye Çeki".
   - Açıklama: "Sevdiklerinize özel hediye çekiyle alışveriş özgürlüğü hediye edin."
   - Sağ tarafta dönen hediye çeki kart görseli (CSS-only kart, gradient).
2. **Tasarım Seç** carousel: 6-8 tema kartı (Doğum Günü pasta, Bebek bulutu, Bayram, Sevgiyle vs.) — seçilen primary ring.
3. **Tutar**: önceden tanımlı chip (100₺, 250₺, 500₺, 1.000₺) + "Diğer" input.
4. **Alıcı bilgileri**:
   - Alıcı adı
   - Alıcı e-postası
   - Teslimat tarihi (Flatpickr — bugün veya ileri tarih)
   - "Mesajınız" (textarea, 200 karakter sayaç).
5. **Önizleme**: girilenler real-time olarak kart görünümünde gösterilir.
6. "Hediye Çekini Sepete Ekle" CTA.

ViewData["Title"] = "Hediye Çeki".
```

---

## 47. `Views/GiftCard/Created.cshtml` — Hediye Çeki Oluşturuldu

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.GiftCard (Code, Amount, Design, RecipientName, RecipientEmail, DeliveryDate, Message).

Layout: ortalı card max-w-2xl, py-16:
- Üstte yumuşak success ikonu.
- H1 "Hediye Çekiniz Hazır".
- "{RecipientName} adına {DeliveryDate} tarihinde gönderilecek."
- **Çek önizlemesi** kart (büyük, brand themed).
- Kod kutu: mono font + "Kopyala" + "PDF İndir" button'lar.
- 2 CTA: "Başka Çek Al" + "Anasayfa".

ViewData["Title"] = "Hediye Çekiniz Oluşturuldu".
```

---

## 48. `Views/GiftCard/Balance.cshtml` — Hediye Çeki Bakiye Sorgula

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Layout: ortalı card max-w-md:
- H1 "Hediye Çeki Bakiyem".
- Input: çek kodu (mask edilmemiş, ama harfleri otomatik upper-case).
- "Sorgula" CTA.
- Sonuç (POST sonrası ViewBag.Result varsa): bakiye + son kullanma tarihi büyük kart.
- Yardım metni: "Çek kodunuzu satın aldığınız e-postada bulabilirsiniz."

ViewData["Title"] = "Hediye Çeki Bakiyem".
```

---

## 49. `Views/Contact/Index.cshtml` — İletişim

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Tenant (ContactPhone, WhatsAppNumber, ContactEmail, Address, City, MapEmbedUrl).

Layout: 2 col grid (md:grid-cols-2 gap-12):
- **Sol — Form**: H1 "Bize Ulaşın" + alt açıklama; form (Ad, E-posta, Telefon (IMask), Konu (Tom Select: Sipariş Sorusu / İade / Ürün Önerisi / Kurumsal / Diğer), Mesaj textarea (sayaç), "Gönder" CTA, KVKK onay checkbox).
- **Sağ — Bilgi**: kart liste:
  * Telefon: ikonlu kart, ContactPhone + "Hafta içi 09:00-18:00".
  * WhatsApp: yeşil pastel kart, "Aynı saatlerde anlık".
  * E-posta: ikonlu kart.
  * Adres: ikonlu kart + Google Maps embed altta (MapEmbedUrl varsa).

ViewData["Title"] = "İletişim".
```

---

## 50. `Views/Tracking/Index.cshtml` — Kargo Takip Form

```
Yukarıdaki Tasarım Sistemi'ni kullan.

Layout: ortalı card max-w-lg:
- H1 "Kargonuzu Takip Edin".
- Alt açıklama: "Sipariş numaranız veya kargo takip kodunuzla anlık durumu öğrenin."
- Form: input ("Sipariş veya takip no"), "Sorgula" CTA.
- Altta küçük "Sipariş numarası satın alma e-postanızda yer alır" not.

ViewData["Title"] = "Kargo Takip".
```

---

## 51. `Views/Tracking/Result.cshtml` — Kargo Takip Sonucu

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Order (OrderNumber, Status, Carrier, TrackingNumber, EstimatedDelivery), ViewBag.Events (List<{Date, Time, Location, Description}>).

Layout: ortalı max-w-3xl:
1. **Üst durum kartı**: Carrier logo + takip no + "Tahmini Teslimat: 5 Haziran Çarşamba" + büyük durum chip ("Kargoda" accent / "Teslim Edildi" success / "İade Edildi" muted).
2. **Timeline** dikey (büyük): Sipariş alındı → Onaylandı → Kargoya verildi → Dağıtım merkezinde → Kuryede → Teslim. Aktif step animated pulse, geçmiş tarih + saat.
3. **Detaylı hareketler** liste (her satır: timestamp, lokasyon, açıklama).
4. Alt: "Kargocuyu Ara" telefon link.
5. Bulunamadıysa: empty state "Kayıt bulunamadı, kodu kontrol edin".

ViewData["Title"] = "Kargo Takibi".
```

---

## 52. `Views/Page/Show.cshtml` — CMS Statik Sayfa

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.Page (Title, Slug, ContentHtml, UpdatedAt) — KVKK, Hakkımızda, İade, SSS gibi statik sayfalar.

Layout: ortalı max-w-4xl py-12:
- Breadcrumb.
- H1 Fraunces: Page.Title.
- "Son güncelleme: {date}" küçük muted.
- Prose content: `<article class="prose prose-lg prose-cream max-w-none">` Razor: @Html.Raw(ViewBag.Page.ContentHtml). Tailwind typography ile başlık/p/list/quote/code stilleri.
- Altta: "Yardım gerekirse iletişim" link.
- SSS sayfaları için soru-cevap accordion görünümü (bir sonraki promptta).

ViewData["Title"] = ViewBag.Page.Title.
```

---

## 53. `Views/Error/Index.cshtml` — Hata Sayfası

```
Yukarıdaki Tasarım Sistemi'ni kullan.

ViewBag.StatusCode (404, 500, 503), ViewBag.Message.

Layout: ortalı card py-20:
- StatusCode'a göre içerik:
  * **404**: Friendly çocuk illustration (kayıp ayı / balon uçuran çocuk) + H1 "Buralarda kimse yok" + alt "Aradığınız sayfa taşınmış olabilir." + 2 CTA: "Anasayfa", "Kataloğa Git".
  * **500/503**: Açıklayıcı illustration + H1 "Bir şeyler ters gitti" + "Hata kayıt altına alındı, en kısa sürede çözeceğiz." + "Tekrar Dene" + "Anasayfa".
- Altta arama kutusu: "Belki şunu mu arıyordunuz?"

ViewData["Title"] = $"Hata {ViewBag.StatusCode}".
```

---

## 54. `Views/Shared/_Breadcrumb.cshtml` (Partial)

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@{
    var breadcrumbs = ViewBag.Breadcrumbs as List<Entegrasyon.Entity.Dtos.Storefront.BreadcrumbItemDto>;
    if (breadcrumbs == null || breadcrumbs.Count == 0) return;
}

<nav> element, schema.org BreadcrumbList JSON-LD opsiyonel.
Görünüm: yatay liste, separator "›" (krem renkli), aktif item bold text-charcoal, link'ler hover'da text-primary.
Mobilde overflow-x-auto, whitespace-nowrap.
```

---

## 55. `Views/Shared/_FilterSidebar.cshtml` (Partial)

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@{
    var query = ViewBag.Query as Entegrasyon.Entity.Dtos.Storefront.StorefrontCatalogQuery;
}

Catalog/Category promptundaki sidebar bloğunun aynısını üret — bağımsız partial olarak (form action="@Url.Action("Category", new {...})" GET).
Her filtre değişikliği form auto-submit veya pushState ile URL güncelleyebilir. JS minimal.
```

---

## 56. `Views/Shared/_Pagination.cshtml` (Partial)

```
Yukarıdaki Tasarım Sistemi'ni kullan.

@{
    var products = ViewBag.Products as Entegrasyon.Entity.Pageable<Entegrasyon.Entity.Dtos.Storefront.StorefrontProductCardDto>;
    if (products == null || products.TotalPageCount <= 1) return;
    var currentUrl = ViewBag.PaginationBaseUrl as string;
}

Sayfalama bileşeni: orta hizalı row, Önceki (disabled ise opacity-50), sayfa numaraları (5 görünür, fazlaysa "..." ile sıkıştır), Sonraki.
Aktif sayfa primary background + text-white. Diğerleri border + hover bg-cream.
A11y: nav role, aria-label, aria-current="page".

Mobilde sadece "‹ Önceki  3/12  Sonraki ›" sade görünüm.
```

---

## Bonus — Çıktıyı geliştirici-dostu hale getirme önerisi

Her sayfayı Claude design'dan aldıktan sonra benim worktree'ye yapıştırırken şunları kontrol edeceğim:
- `@model` direktifi başta korunmuş mu
- `@Html.Raw` ya da Razor injection'ları XSS açısından güvenli mi
- Tom Select/Flatpickr/GLightbox script include'ları @section Scripts'te
- ViewBag prop'larını Controller'da set ediyor muyuz, gerekirse Controller method'unu güncelleyeceğim
- A11y (aria, focus-visible, alt text) eksikse ekleyeceğim
- Tailwind `bg-primary` `text-primary` gibi token'lar mevcut config'de — yoksa tailwind.config.js'ye color extension ekleyeceğim

İlk sayfayı denemek için **0. Tasarım Sistemi** + **3. Anasayfa** promptlarını sırayla Claude design'a yapıştır, çıkan kodu bana gönder.

---

# 🚀 Hızlı Promptlar — Anasayfa Sonrası (Süreklilik Direktifli)

> Anasayfa Claude design'da oluşturulduktan sonra **aynı konuşmada** sıradaki sayfaları üretirken kullan. Eğer **yeni konuşma** açıyorsan, her promptun başına önce aşağıdaki **Süreklilik Direktifi**ni yapıştır.

---

## A. Süreklilik Direktifi (yeni konuşmaya geçtiğinde bir kez)

```
SÜREKLILİK DİREKTİFİ — Zekids Bebe storefront tasarım sistemi (önceki anasayfa konuşmasından devam):

Marka: Zekids Bebe. Logo lock-up: "Zekids" charcoal + italic <span class="text-primary">"Bebe"</span>, Fraunces italic.

Tailwind config (aynısı tüm sayfalarda):
  primary: #FF8FB1, secondary: #8FCBE8, accent: #FFD56B
  cream { DEFAULT: #FAF6F0, 300: #E8DFD2 }
  charcoal: #2C3E50, muted: #7B8794
  success: #4CAF89, danger: #E27D7D, warning: #F2B544
  fontFamily: heading=Fraunces, body=Inter

Global stiller (her sayfada aynı):
- font-heading: Fraunces
- body: Inter, color #2C3E50, bg #fff, antialiased
- .ph (placeholder) ve img.photo pattern'i koru (gerçek fotoğraf yüklendiğinde stripeları gizle)
- .sticker (yumuşak gölge), .toast-in animasyonu, .no-scrollbar

ORTAK CHROME (kopyala-yapıştır, redesign etme):
- Üst duyuru barı (charcoal bg, kapatılabilir, "500₺ üzeri kargo bedava · 30 gün ücretsiz iade")
- Sticky header (bg-white/95 backdrop-blur, border-b border-cream-300):
  * Sol: mobil hamburger + logo lock-up
  * Orta (lg only): "Tüm Kategoriler" dropdown trigger + nav linkler (Yeni Gelenler, Kız, Erkek, Bebek, Outlet — Outlet primary renkli)
  * Sağ: arama, hesap, wishlist (kalp + counter), sepet (poşet + counter + tutar)
- Mega menü (header altında full-width pastel panel, 4 sütun: Kız/Erkek/Bebek/Outlet + sağda öne çıkan koleksiyon kartı, ESC ve dış-tık ile kapanır)
- Mobile drawer (sol slide-in), arama overlay (full-screen)
- Footer alanı: trust band (4 ikon: kargo bedava, 30 gün iade, güvenli ödeme, WhatsApp) → newsletter (pembe pastel kart, "%10 indirim hediye") → 4 sütun footer (Hakkımızda, Yardım, Sosyal, İletişim) → alt çizgi + telif + ödeme ikonları

Tüm chrome elementlerini anasayfadakiyle birebir koru. Sadece <main> içindeki sayfa-spesifik içeriği değiştir.

Türkçe, "siz" dili, çocuk giyim sektörü, 0-14 yaş, sıcak-oyuncu ton.
"Anladım, hangi sayfayı tasarlayalım?" diyerek onayla.
```

---

## B. Ürün Detay Sayfası (Önerilen sıra: 1)

```
Şimdi Ürün Detay sayfasını tasarla. Aynı chrome'u koru (header/footer/announcement/mega menu).

Ürün: "Çiçekli Yazlık Elbise" (placeholder veri kullan).
Veri seti (sample data ile mock'la):
- 5 görsel (Unsplash çocuk elbisesi flat-lay ve model fotoğrafları)
- Marka: "Mavi Kids"
- Fiyat: 349₺, eski fiyat: 499₺, indirim %30
- Beden: 2-3 yaş (98), 3-4 yaş (104), 4-5 yaş (110), 5-6 yaş (116), 6-7 yaş (122) — 4-5 yaş stoksuz, diğerleri var
- Renk: 3 seçenek — Pudra Pembe (#FFD0DC), Bebek Mavisi (#B8E0F0), Sarı (#FFE39E)
- Stok: 12 adet (104 bedeninde)
- Rating: 4.8 / 156 değerlendirme
- Yaş aralığı chip: 2-7 yaş
- Sezon: İlkbahar/Yaz

Layout (lg:grid-cols-2 gap-12, mobilde stack):

SOL — Galeri:
- Büyük ana görsel aspect-[4/5] rounded-2xl. Tıkla GLightbox stilinde lightbox (vanilla JS ile basit modal ok).
- 5 thumbnail strip altta (mobilde yatay scroll-snap, masaüstünde grid-cols-5). Aktif olan ring-2 ring-primary.
- Sol üst overlay: "%30 İndirim" sarı sticker rotated -8deg (anasayfadaki sticker stilinde).
- Sağ üst: kalp button (wishlist toggle, dolu/boş state, tıklayınca Notyf-benzeri toast).

SAĞ — Bilgi paneli (lg:sticky lg:top-24 lg:self-start):
- Marka linki: küçük muted, hover primary.
- H1 ürün adı: font-heading text-3xl text-charcoal.
- Rating row: 5 yıldız (4.8 dolu, 0.2 yarım) + "(156 değerlendirme)" link.
- Fiyat row: eski fiyat çizgili gri text-lg + güncel fiyat text-4xl font-semibold text-primary.
- Stok rozeti: yeşil pastel chip "Stokta · 12 adet" veya turuncu "Son 2 ürün".
- Yaş aralığı chip: bg-cream rounded-full px-3 py-1 text-sm "2-7 yaş".

- BEDEN SEÇİCİ:
  * Üst row: "Beden seçin" + sağda "Beden Tablosu" link → modal açar
  * Chip grid: her beden için button, "98 (2-3 yaş)" gibi. Aktif: bg-primary text-white. Stoksuz: line-through opacity-40 cursor-not-allowed.

- RENK SEÇİCİ:
  * "Renk: Pudra Pembe" satır
  * Yuvarlak renk swatch row (w-10 h-10 rounded-full border-2). Aktif: ring-2 ring-offset-2 ring-primary.
  * Hover'da tooltip ile renk adı.

- ADET STEPPER: - [1] + butonlar, rounded-full border, min 1 max stok.

- 2 CTA stack:
  * "Sepete Ekle" — primary button, full-width, rounded-full, py-4, font-semibold, shadow primary glow
  * "Hemen Al" — outline primary, py-4

- Quick info kartları (her biri ikonlu satır, border-t border-cream-300 üstte):
  * Kargo ikonu — "Yarın elinizde" (sample tahmini teslim tarihi)
  * Paket ikonu — "500₺ üzeri kargo bedava"
  * İade ikonu — "30 gün ücretsiz iade"
  * Chat ikonu — "WhatsApp ile sor" link

- Paylaş ikonları: WA, Facebook, link kopyala (button).

TAB'LAR (üstte text-sm font-semibold, aktif tab border-b-2 border-primary):
- Ürün Detayı — uzun açıklama paragrafları + özellikler tablosu (Kumaş: %100 pamuk, Yıkama: 30°, Menşei: Türkiye, Beden modeli için: model 110 cm boyunda 4 yaş çocuk).
- Beden Rehberi — modal'daki tablo aynısı.
- İade & Kargo — markdown listesi.
- Yorumlar (156) — özet kart (büyük 4.8 + yıldız dağılım bar chart) + 5 örnek yorum (avatar, isim, doğrulanmış alıcı badge, yıldız, tarih, yorum metni, "Faydalı (12)" mini link) + "Yorum Yaz" CTA.

BEDEN TABLOSU MODAL (sayfa içi, gizli, açıldığında bg-black/40 overlay):
- Card max-w-2xl rounded-3xl bg-white p-8
- Üst: H2 "Beden Tablosu" + X kapat
- Açıklama: "Çocuk bedenleri yaşa ve boya göre değişir. En doğru beden için çocuğunuzun boy ve kilosunu ölçün."
- Tablo (4 sütun): Beden | Yaş | Boy (cm) | Kilo (kg)
  * 86 | 12-18 ay | 80-86 | 11-12
  * 92 | 18-24 ay | 86-92 | 12-13
  * 98 | 2-3 yaş | 92-98 | 13-15
  * 104 | 3-4 yaş | 98-104 | 15-17
  * 110 | 4-5 yaş | 104-110 | 17-19
  * 116 | 5-6 yaş | 110-116 | 19-22
  * 122 | 6-7 yaş | 116-122 | 22-25
- Altta küçük SVG diyagram: "Nasıl ölçülür?" (boy, göğüs, bel okları).

BENZER ÜRÜNLER (mt-16):
- "Benzer Ürünler" başlığı + 8 ürün carousel'ı (anasayfadaki ürün kartı tasarımıyla aynı stil).

BİRLİKTE ALINANLAR (mt-16):
- "Birlikte sık alınıyor" başlığı
- 3 ürün yan yana + + + 3 ürün toplamı + "Sepete Ekle (3 ürün - 750₺)" CTA pakete özel %10 indirimle.

SON GEZDİKLERİNİZ (mt-16):
- 6 ürün carousel'ı.

STICKY MOBİL CTA BAR (mobilde alt sabit, lg:hidden):
- bg-white border-t border-cream-300 p-3
- Sol: küçük thumbnail + fiyat
- Sağ: "Sepete Ekle" buton

ETKİLEŞİMLER:
- Beden seç → state update + "Stokta {n} adet" mesajı güncellenir
- Renk swatch tıkla → aktif değişir, ana görsel ona göre değişir (varsa)
- Tab tıkla → içerik değişir
- Kalp → toast "Favorilere eklendi"
- Sepete Ekle → toast "Sepete eklendi (1 adet)"
- Beden Tablosu link → modal aç (ESC + dış-tık + X kapat)
- Lightbox: ana görsele tıkla → full-screen overlay, ok tuşlarıyla gez, ESC kapat
```

---

## C. Kategori Listeleme Sayfası (Önerilen sıra: 2)

```
Şimdi Kategori Listeleme sayfasını tasarla (örnek: Kız > Elbise). Aynı chrome'u koru.

Sample data:
- Kategori adı: "Kız Elbise"
- Açıklama: "Tatlı baskılar, yumuşak kumaşlar — minik prensesiniz için özenle seçtik."
- Hero görseli: küçük kız çiçekli elbise modeli
- 24 ürün (Pageable, sayfa 1/4, toplam 96)
- Aktif filtreler örnek: "2-4 yaş", "Pudra Pembe" chip'leri üstte

Layout breadcrumb + hero card + (lg:grid-cols-[18rem_1fr] gap-8):

BREADCRUMB (üst): Anasayfa › Kız › Elbise — küçük muted.

KATEGORİ HERO CARD (bg-cream rounded-3xl mb-8):
- 2 col grid: sol metin (H1 "Kız Elbise" font-heading text-4xl + Fraunces italic alt "Tatlı baskılar...") + sağ hero görsel (rounded-2xl aspect-[4/3]).
- py-10 px-8.

SOL SIDEBAR (lg:w-72 lg:sticky lg:top-24, mobilde alt bottom-sheet trigger):
- Mobilde: sayfa altında sticky "Filtrele + Sırala" button bar, tıklayınca bottom-drawer.
- Card: bg-white border border-cream-300 rounded-2xl p-6 space-y-6
- Üstte: "Filtreler" + sağda "Temizle" link (aktif filtre varsa).
- Aktif filtre chip'leri: küçük rounded-full bg-primary/10 text-primary "2-4 yaş ×" stil.

Filtre grupları (her biri details/summary collapsible, varsayılan açık):
1. Yaş Aralığı (chip multiselect):
   - 0-3 ay, 3-6 ay, 6-12 ay, 1-2 yaş, 2-4 yaş, 4-6 yaş, 6-8 yaş, 8-10 yaş, 10-12 yaş, 12-14 yaş
   - chip stil: rounded-full border px-3 py-1.5 text-sm; aktif bg-primary text-white border-primary

2. Beden (chip multiselect):
   - 74, 80, 86, 92, 98, 104, 110, 116, 122, 128, 134

3. Cinsiyet (radio):
   - Hepsi / Kız / Erkek / Unisex

4. Mevsim (chip multi): İlkbahar, Yaz, Sonbahar, Kış

5. Renk (yuvarlak swatch grid w-8 h-8 rounded-full):
   - Pudra Pembe, Bebek Mavisi, Krem, Beyaz, Açık Gri, Sarı, Mint Yeşili, Lavanta
   - aria-label ile renk adı
   - aktif: ring-2 ring-primary ring-offset-2

6. Fiyat Aralığı:
   - 2 input min-max yan yana
   - Altında range slider (basit double thumb, custom CSS)

7. Marka (checkbox liste):
   - Üstte mini arama input
   - 8-10 marka checkbox + ürün sayısı: ☐ Mavi Kids (24) ☐ Lc Waikiki (18) ...
   - max-h-48 overflow-y-auto

8. Durum (toggle switches):
   - ☑ Sadece stokta olanlar
   - ☐ İndirimliler
   - ☐ Yeni gelenler

SAĞ ANA ALAN:
- Üst bar (flex justify-between items-center mb-6):
  * Sol: "96 ürün" + altında küçük "12-36 arası gösteriliyor" muted text
  * Sağ: Sıralama dropdown — özel görünüm, native değil. Trigger: "Sırala: Önerilen ▾". Açılınca menü.
    Opsiyonlar: Önerilen, Yeniden Eskiye, Fiyat (artan), Fiyat (azalan), En Çok Satan, En Yüksek Puanlı, En Çok İndirim
  * Sağda küçük: grid view toggle (2/3/4 kolon mobil-tablet-desktop)

- Ürün grid: grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-6
- Her ürün kartı: Anasayfadaki ürün kartı stilinde (görsel + badge + kalp + marka + ad + yaş chip + fiyat + beden chip'leri).

- Sayfalama altta (mt-12):
  * Yatay row centered
  * "‹ Önceki" — disabled ise opacity-50 cursor-not-allowed
  * Sayfa numaraları: [1] [2] [3] ... [12] — aktif bg-primary text-white rounded-full w-10 h-10
  * "Sonraki ›"

- BOŞ DURUM (eğer sonuç yok): card bg-cream rounded-3xl py-16 text-center
  * SVG illustration (basit, üzgün ayıcık veya boş kutu)
  * "Aradığınız ürünü bulamadık" font-heading text-2xl
  * "Filtreleri biraz daha geniş tutmayı deneyin"
  * "Filtreleri Temizle" CTA

ETKİLEŞİMLER:
- Filtre değişimi → URL query güncellense ürün grid'i fade-update olur (mock animation OK)
- Aktif filtre chip × tıkla → o filtreyi temizler
- Sıralama dropdown: outside-click + ESC ile kapanır
- Mobil filtre drawer: bottom-sheet slide-up, header'da "Filtrele" + sağda kapat, alt sticky "Uygula (45 ürün)" CTA

JS olarak filtre toggle + sıralama dropdown + mobile drawer yeterli, gerçek filtreleme yapmasına gerek yok.
```

---

## D. Sepet Sayfası (Önerilen sıra: 3)

```
Şimdi Sepet sayfasını tasarla. Aynı chrome'u koru.

Sample data (3 ürün sepette):
1. Çiçekli Yazlık Elbise — Beden: 104 (3-4 yaş), Renk: Pudra Pembe — 1 adet — 349₺ (eski 499₺)
2. Eşofman Takımı — Beden: 110 (4-5 yaş), Renk: Bebek Mavisi — 2 adet — 199₺ × 2 = 398₺
3. Pamuklu Tişört (3'lü Paket) — Beden: 98 (2-3 yaş) — 1 adet — 249₺
- Ara toplam: 996₺
- Kargo: 0₺ (500₺ üstü)
- Sadakat indirimi: -50₺ (uygulanmış)
- Toplam: 946₺
- Free shipping threshold: 500₺ — şu an 996₺ — "Kargonuz bedava" success ikonlu mesaj

Üst: Breadcrumb + H1 "Sepetim (3 ürün)".

PROGRESS BAR (üstte tüm sepet kartından önce):
- Card bg-success/10 rounded-2xl p-4 mb-6
- "Kargonuz bedava!" success ikonlu + progress bar full primary
- VEYA "Bedava kargoya 154₺ kaldı" + bar yarı dolu

GRİD (lg:grid-cols-3 gap-8):

SOL (lg:col-span-2):
Card bg-white border border-cream-300 rounded-2xl
- Her ürün satırı (grid grid-cols-[100px_1fr_auto] gap-4 p-6, border-b border-cream-300 son hariç):
  * Görsel 100px aspect-[4/5] rounded-xl
  * Orta:
    - Ürün adı (font-medium link)
    - "Beden: 104 · Renk: Pudra Pembe" muted text-sm
    - Stok uyarısı sadece son 2 ürün vb. ise turuncu
    - Mini link'ler: "Daha sonra için kaydet" · "Favorilere taşı"
  * Sağ stack:
    - Üst: Sil ikonu (trash, hover red)
    - Orta: Adet stepper - [1] +
    - Alt: Eski fiyat çizgili + line total primary bold
- Footer satırı: "Tümünü kaldır" link sol + alışverişe devam linki sağ

SAĞ (lg:col-span-1) — Özet card sticky lg:top-24:
- bg-cream rounded-2xl p-6
- "Sipariş Özeti" font-heading text-xl
- Detay rows (justify-between text-sm):
  * Ara toplam: 996₺
  * Kargo: <span class="text-success">Bedava</span>
  * Sadakat indirimi: -50₺ (success renk)
- Border-t border-cream-300 my-4
- Toplam row: font-bold text-xl, sağda text-primary text-2xl

- Kupon kutusu:
  * "Kupon kodunuz var mı?" details/summary
  * Açıldığında: input + "Uygula" button row
  * Aktif kupon varsa: chip "BAHAR50 -50₺ ×"

- Hediye kartı kutusu (aynı pattern)

- "Ödemeye Geç" primary button, full, py-4, rounded-full, font-semibold + sağ ok ikonu

- Güvenli ödeme rozetleri row: SSL + Visa + MC + Troy SVG'leri minik

- Küçük disclaimer: "KDV dahildir. Aynı gün kargoda ise yarın elinizde."

ÖNERİLENLER (alt):
- "Birlikte iyi gider" başlığı
- 4 ürün carousel'ı

BOŞ SEPET DURUMU (alternatif görünüm — buton ile preview göster):
- Card py-16 text-center
- SVG illustration (boş alışveriş poşeti, ayıcık)
- "Sepetiniz henüz boş" font-heading text-2xl
- "Birbirinden tatlı ürünler sizi bekliyor"
- "Alışverişe Başla" CTA + altında "Favorilerimi gör" link

ETKİLEŞİMLER:
- Adet stepper - / + → state update, line total + sipariş özeti recalculate
- Sil → satır slide-out animation + toast "Üründen kaldırıldı + Geri Al"
- Kupon "Uygula" → mock validation (BAHAR50 başarılı, diğeri "Geçersiz")
- Toplam değişimi sayı counter animasyonu (300ms ease)
```

---

## Önerilen üretim sırası

1. **B (Ürün Detay)** — galeri + varyant + beden tablosu pattern'ini sabitler
2. **C (Kategori Listeleme)** — filtre sidebar pattern'i, yaş chip'leri
3. **D (Sepet)** — daha hafif

Bunları üretip bana attığında, ilk üç sayfanın Razor'a çevrilmesi tek seferde olur.

---

## E. Checkout — 3 Adımlı Ödeme (Önerilen sıra: 4)

```
Şimdi Checkout (ödeme) sayfasını tasarla. Aynı chrome'u koru.

Tek sayfa, üstte 3 adım stepper (Adres → Kargo → Ödeme), her adım accordion-style açılıp kapanır. Aktif adım primary, tamamlanmış check.

Sample data:
- 2 kayıtlı adres:
  * Ev (varsayılan): Ayşe Yılmaz, 0(532) 123 45 67, Atatürk Mah. Çiçek Sok. No:12 D:4, Kadıköy/İstanbul 34710
  * İş: Ayşe Yılmaz, 0(532) 123 45 67, Bağdat Cad. No:55 K:3, Maltepe/İstanbul 34840
- Sepet 3 ürün: Çiçekli Elbise (104) Pudra Pembe — 349₺, Eşofman Takımı (110) Mavi — 199×2=398₺, Pamuklu Tişört (98) — 249₺. Ara toplam 996₺.
- Kargo seçenekleri:
  * Standart: 1-3 iş günü, Bedava (500₺ üstü)
  * Express: yarın elinizde, 49.90₺
  * Aynı Gün (İstanbul içi): 79.90₺
- Hediye paketi: +15₺ toggle, 3 renk seçimi (pembe/mavi/krem) + not textarea.
- Toplam (Standart + hediye paketi yok): 996₺.

LAYOUT (lg:grid-cols-3 gap-8):

SOL (lg:col-span-2):
1. Stepper bar üst (px-2, items-center justify-between):
   - 3 step her biri için: yuvarlak number (aktifte primary dolu, tamamlandığında success + check, pasifte border + muted), label.
   - Aralarda ince çizgi (tamamlananlar success çizgi).

2. **Adım 1 — Teslimat Adresi** (accordion içerik):
   - Kayıtlı adres kartları grid (sm:grid-cols-2 gap-4):
     * Her kart radio seçim, başlık (Ev/İş) + IsDefault ise primary badge, tam adres, alt sağ "Düzenle" link.
     * Seçili kart border-primary ring-2.
   - "+ Yeni Adres Ekle" kart (dashed border) — tıkla açıldığında inline form:
     * Adres etiketi (Ev/İş/Anneanne free text)
     * Ad-Soyad, Telefon (IMask 0(5XX) XXX XX XX)
     * İl (Tom Select), İlçe (il bağımlı), Mahalle, Açık adres (textarea)
     * Posta kodu (IMask 5 hane)
     * "Varsayılan adres yap" checkbox
     * "Kaydet" CTA
   - Fatura tipi toggle: "Bireysel" / "Kurumsal" (kurumsal: vergi no + vergi dairesi alanları açılır)
   - "Faturayı farklı adrese gönder" toggle → ek adres seçimi.
   - "Devam Et" primary CTA → adım 2'ye geç.

3. **Adım 2 — Kargo Yöntemi**:
   - 3 kart radio (cards bg-white border rounded-2xl p-5):
     * Her kart: sol radio + ikon, orta başlık + alt açıklama (teslim süresi), sağ fiyat.
     * Aktif: border-primary ring + bg-primary/5.
   - "Hediye Paketi" toggle card (rounded-2xl bg-accent/10):
     * Toggle açıkken aşağı açılır: 3 renk swatch + "Hediye notunuz" textarea (200 karakter sayaç).
   - "Devam Et" CTA.

4. **Adım 3 — Ödeme**:
   - Sekme bar: Kart / Havale-EFT / Kapıda Ödeme (aktif border-b-2 primary).
   - **Kart sekmesi**:
     * Kart numarası input (IMask 4-4-4-4 16 hane, kart bin ikonunu sağda göster — Visa/MC/Troy)
     * Kart üzerindeki ad
     * Son kullanma MM/YY (IMask) + CVV 3 hane (IMask, password-style toggle)
     * 3D Secure ile öde checkbox (varsayılan açık)
     * Taksit seçenekleri: button row (Tek Çekim, 3 Ay, 6 Ay, 9 Ay, 12 Ay) — aktif primary
     * Kartı kaydet (bir sonraki alışveriş için) checkbox
   - **Havale sekmesi**: hesap bilgileri kartı (banka, hesap no, IBAN — kopyalanabilir), açıklama text.
   - **Kapıda sekmesi**: ek ücret notu "+10₺ hizmet bedeli", "Sadece kart ile, nakit kabul edilmez" not.
   - KVKK/Mesafeli Satış checkbox'ları:
     * "Ön Bilgilendirme Formu'nu okudum onaylıyorum" link
     * "Mesafeli Satış Sözleşmesi'ni okudum onaylıyorum" link
   - "Siparişi Tamamla" CTA primary (full-width py-4 rounded-full font-semibold + kilit ikonu).

SAĞ (lg:col-span-1) — Sticky özet (lg:top-24):
- bg-cream rounded-2xl p-6
- "Sipariş Özeti" font-heading
- Ürün thumbnail liste (mini): 50x60 görsel + ad + variant + "x{adet}" + line price (max-h-64 overflow-y-auto)
- Border-t border-cream-300
- Detay: Ara toplam, Kargo (Bedava success), Hediye paketi (varsa), İndirim (varsa).
- Toplam row büyük primary.
- Kupon collapse: input + uygula
- Güvenli ödeme ikonları SSL+Visa+MC+Troy

MOBIL: sticky bottom bar "996₺ · Siparişi Tamamla" button.

ETKİLEŞİMLER:
- Stepper tıkla → tamamlanmış adıma geri dön
- "Devam Et" → bir sonraki adımı aç + öncekini kapat (accordion)
- Yeni adres formu inline aç/kapa
- Kart numarası girilince taksit kartı dinamik göster (mock)
- Hediye paketi toggle → renk + not alanları açılır
- KVKK linkleri tıkla → modal aç (lorem ipsum metin)
- "Siparişi Tamamla" → /odeme/basarili'ya yönlendir (mock)
```

---

## F. Checkout Başarılı (Önerilen sıra: 5)

```
Şimdi Checkout/Basarili sayfasını tasarla. Aynı chrome'u koru.

Sample data:
- Sipariş no: ZB-2026-1234
- Tahmini teslimat: 4-6 Haziran Çarşamba
- Toplam tutar: 996₺
- Müşteri e-postası: ayse.yilmaz@example.com
- Takip URL: /siparis-takip/ZB-2026-1234

Ortalı tek card, max-w-3xl, py-16:
1. Üstte yumuşak success ikonu büyük (yuvarlak success/20 bg + check ikonu primary boyutta). Hafif konfeti SVG arka plan (pembe-mavi-sarı yumuşak parçacıklar, abartısız).
2. H1 font-heading text-4xl: "Siparişiniz alındı, teşekkürler!"
3. Alt paragraf muted:
   "Sipariş <span class='font-semibold text-charcoal'>#ZB-2026-1234</span> onay e-postası {Email} adresine gönderildi."
4. **Bilgi kartı** rounded-2xl bg-cream p-6 my-8:
   - 2 col (md:grid-cols-2 gap-6):
     * Tahmini teslimat: ikon + "4-6 Haziran Çarşamba" bold
     * Toplam: ikon + "996₺" bold primary
5. **Takip bandı** card bg-primary/10 rounded-2xl p-5 flex:
   - Sol: kargo ikonu + "Siparişiniz hazırlanıyor" + alt küçük muted "Tahmini kargolanma: 1-2 iş günü"
   - Sağ: "Siparişi Takip Et" outline primary button
6. 2 CTA stack (mt-8 sm:flex-row sm:justify-center gap-4):
   - "Hesabıma Git" primary full
   - "Alışverişe Devam Et" outline
7. Altta WhatsApp hatırlatma satırı küçük:
   "İstediğiniz ürünü bulamadınız mı? <a class='text-success font-medium'>WhatsApp ile sorun</a>"
8. **Önerilen ürünler** carousel mt-16 (anasayfa product card stilinde, 6-8 ürün):
   - "Bunlar da ilginizi çekebilir" başlığı
```

---

## G. Checkout Başarısız (Önerilen sıra: 5b)

```
Şimdi Checkout/başarısız sayfasını tasarla. Aynı chrome'u koru.

Sample data:
- Hata kodu: PAYMENT_3D_FAILED
- Hata mesajı: "Bankanız 3D Secure doğrulamasını reddetti"
- İletişim: 0850 000 00 00, /İletişim, WhatsApp

Ortalı card max-w-2xl py-16:
1. Yumuşak warning ikonu (sarı pastel yuvarlak + ünlem işareti, hiç korkutucu değil).
2. H1 "Ödemenizi tamamlayamadık"
3. Alt açıklama:
   "Ödemeniz şu sebeple alınamadı: <span class='font-medium text-charcoal'>Bankanız 3D Secure doğrulamasını reddetti.</span> Endişelenmeyin, hesabınızdan herhangi bir kesinti olmadı."
4. **Ne yapabiliriz?** card rounded-2xl bg-cream p-6:
   - 4 ikonlu satır:
     * Kart bilgilerini kontrol edin
     * Farklı bir kart veya banka deneyin
     * Bankanızı arayarak limit/online işlem onayını kontrol edin
     * Havale, EFT veya kapıda ödeme seçin
5. 2 CTA: "Tekrar Dene" primary (ödeme adımına döner) + "Sepete Dön" outline.
6. Alt iletişim satırı:
   "Yardım gerekirse: 0850 000 00 00 · <a class='text-success'>WhatsApp</a> · <a>İletişim formu</a>"
```

---

## H. Auth/Login (Önerilen sıra: 6)

```
Şimdi Login sayfasını tasarla. Aynı chrome'u koru ama header ve footer'ı sadeleştir (login'de daha minimal hissetsin — header normal, footer kompakt).

Sample data:
- "ReturnUrl" varsa giriş sonrası ona dön.
- Google + Apple SSO mevcut.

Split layout (min-h-[70vh] lg:grid-cols-2):

SOL (lg only): bg-primary/15 illustration alanı
- Aspect-square mid-card içinde aile/çocuk fotoğrafı (Unsplash mutlu anne-bebek).
- Alt köşede Fraunces italic slogan: "Minik dolaplara büyük ilham."
- En altta 3 trust chip row (kargo bedava, 30 gün iade, güvenli):
  * Beyaz pill rounded-full + ikon + metin

SAĞ: form alanı flex justify-center items-center px-8 py-12
- Card max-w-md mx-auto w-full:
  * Logo mini üst (Zekids<span>Bebe</span> italic)
  * H1 font-heading text-3xl mt-6: "Tekrar Hoş Geldiniz"
  * Alt muted: "Hesabınıza giriş yapın"
  * Form (mt-8 space-y-4):
    - E-posta input (label "E-posta", input pill rounded-full px-5 py-3 border-cream-300 focus:border-primary focus:ring-2 focus:ring-primary/20)
    - Şifre input (göz ikonu sağda, tıkla göster/gizle toggle)
    - Row flex justify-between text-sm:
      * Sol: checkbox "Beni hatırla"
      * Sağ: "Şifremi unuttum" link primary
    - "Giriş Yap" primary button full rounded-full py-3.5 font-semibold
  * Divider "veya" (centered with line)
  * SSO row (grid-cols-2 gap-3):
    - Google button (white bg, gray border, G ikonu + "Google ile giriş")
    - Apple button (black bg, white text, Apple ikonu + "Apple ile giriş")
  * Alt link mt-8 text-center text-sm:
    "Hesabınız yok mu? <a class='font-semibold text-primary'>Hemen kayıt olun</a>"

Mobil: split kalkar, sadece sağ form. Üstte küçük illustration banner aspect-[16/9].
```

---

## I. Auth/Register (Önerilen sıra: 7)

```
Şimdi Register sayfasını tasarla. Aynı chrome + Login ile aynı split layout.

SAĞ form:
- H1 "Aramıza Hoş Geldiniz"
- Alt: "Hesap oluşturarak hızlı sipariş ve kişisel önerilere erişin"
- Form alanları (mt-6 space-y-4):
  * Ad / Soyad (sm:grid-cols-2 gap-3)
  * E-posta
  * Telefon (IMask 0(5XX) XXX XX XX) — opsiyonel etiket
  * Şifre (göz toggle + strength bar altta — zayıf/orta/güçlü renkli)
  * Şifre tekrar (eşleşme kontrolü gerçek zamanlı)
  * Doğum tarihi (Flatpickr TR locale, "Doğum gününüzde sürpriz" mini note)
  * "Pazarlama e-postaları almak istiyorum" checkbox (varsayılan kapalı, KVKK uyumu)
  * "Kullanım Koşulları ve KVKK Aydınlatma Metni'ni kabul ediyorum" zorunlu (linkler modal açar)
- "Hesap Oluştur" primary full
- Divider + SSO row (Login ile aynı)
- Alt: "Zaten üye misiniz? Giriş yapın"

SOL illustration: farklı bir Unsplash görseli (çocuk + ebeveyn portresi), slogan "Aile gibi bir markaya hoş geldiniz."
```

---

## J. Auth/ForgotPassword + ResetPassword + ConfirmEmail + TwoFactor (Önerilen sıra: 8 — 4 sayfa, ardarda)

```
Şimdi 4 minimal Auth sayfasını birden tasarla. Hepsi ortalı card max-w-md, chrome aynı.

1) Auth/ForgotPassword
- Card py-12 px-8:
- Üstte anahtar ikonu yumuşak (bg-primary/10 rounded-full p-3)
- H1 "Şifremi Unuttum"
- Açıklama: "E-posta adresinizi girin, size sıfırlama bağlantısı gönderelim."
- E-posta input
- "Bağlantı Gönder" primary full
- Alt: "Giriş ekranına dön" link

2) Auth/ResetPassword
- Token query string'den gelir (gizli input)
- H1 "Yeni Şifre Belirle"
- Açıklama: "Hesabınız için yeni bir şifre belirleyin."
- Yeni şifre (göz + strength bar)
- Şifre tekrar
- Kuralları gerçek zamanlı kontrol listesi:
  ✓ En az 8 karakter ✓ Bir büyük harf ✓ Bir sayı (kontrol süresince yeşil tik canlandırılır)
- "Şifreyi Güncelle" CTA

3) Auth/ConfirmEmail (3 state)
- @model string ile durum ("success" / "expired" / "invalid")
- success: yeşil check yuvarlak, "E-posta adresiniz doğrulandı", "Hoş geldiniz! Artık hesabınızı kullanabilirsiniz." + "Hesabıma Git" primary
- expired: turuncu warning, "Bu doğrulama bağlantısının süresi doldu", "Yeni bağlantı iste" CTA
- invalid: kırmızı X, "Bağlantı geçersiz", "Giriş ekranına dön"

4) Auth/TwoFactor
- H1 "İki Aşamalı Doğrulama"
- Açıklama: "Uygulamanızdan 6 haneli kodu girin." (veya SMS modu)
- 6 ayrı kutu OTP input (auto-focus geçişi + paste handler kabul eder)
- Geri sayım: "Kodu yeniden gönder (00:42)" disable, süre bitince aktif
- "Doğrula" CTA
- "Yöntem değiştir" link (SMS ↔ Authenticator toggle)
```

---

## K. Account Hub (_AccountSidebar + Index) (Önerilen sıra: 9)

```
Şimdi Hesap Sidebar partial + Account/Index (dashboard) sayfalarını tasarla. Chrome aynı.

ÖNCE: _AccountSidebar partial (her hesap sayfasında sol sticky)
- @model string activeKey (dashboard, orders, addresses, profile, security, wallet, loyalty, returns, referral, buyagain, wishlist)
- Sticky lg:top-24 lg:w-72
- Card bg-white border border-cream-300 rounded-2xl p-6:
  * Üst: user kart (avatar yuvarlak baş harfler fallback "AY" — bg-primary/15 text-primary, ad-soyad font-medium, "Bronze Üye" rozet primary/10 text-primary text-xs px-2 py-0.5 rounded-full)
  * "Profili Düzenle" mini link
- Menü grupları (her grup font-semibold text-xs uppercase text-muted üst etiket + 4-5 link):
  * "Hesabım" — Özet, Siparişlerim, İadeler, Yine Al, Favorilerim
  * "Cüzdan & Puan" — Cüzdanım, Sadakat Puanları, Davet Et
  * "Ayarlar" — Profil, Adresler, Güvenlik, Şifre, 2FA
  * Çıkış: form post + kırmızı outline button alt
- Her menu item: rounded-xl px-3 py-2.5 + ikon (lucide outline) + label
- Aktif item: bg-primary/10 text-primary font-medium border-l-2 border-primary

Mobil: sayfa üstünde collapsible "Hesabım ▾" trigger (bottom sheet açar).

---

SONRA: Account/Index (dashboard)
Sample data:
- User: Ayşe Yılmaz, Bronze üye, üyelik 2 yıl
- Aktif sipariş: 2, kargoda 1
- Cüzdan: 250₺
- Puan: 1.250 (Silver'a 750 kaldı)
- Wishlist: 8
- Aktif kuponlar: 3
- Son 3 sipariş

Layout: 2 col (md:grid-cols-[18rem_1fr] gap-8)
SOL: _AccountSidebar partial activeKey="dashboard"
SAĞ:
1. Selamlama: "Merhaba Ayşe" font-heading text-3xl + alt muted "Üyelik: Bronze · 2 yıldır bizimlesiniz"

2. **Hızlı kart grid** (md:grid-cols-4 gap-4):
   - Her kart rounded-2xl p-5 (farklı pastel):
     * Aktif Sipariş: bg-primary/10, ikon, "2" büyük, "Tümünü gör →" link
     * Cüzdan: bg-secondary/15, ikon, "250₺" büyük, "Detay →"
     * Puan: bg-accent/20, ikon, "1.250" büyük + progress bar + "Silver'a 750 kaldı"
     * Favoriler: bg-success/10, ikon, "8" büyük, "Tümünü gör →"

3. **Aktif Kuponlar** kart bg-cream rounded-2xl p-6:
   - Başlık satırı: "Aktif Kuponlarınız" + "Tümü →" link
   - Yatay kuponlar (overflow-x-auto): her kupon kart:
     * Sol kupon ikonu + sağ "BAHAR50" mono + alt "-50₺" big primary + tarih "31 Aralık'a kadar" + "Kopyala" mini button
     * Kart kenarı dashed border-primary, hover scale

4. **Son Siparişler** kart border border-cream-300 rounded-2xl divide-y:
   - Header row: "Son Siparişler" + "Tümünü gör →"
   - 3 satır: thumbnail strip (3 mini görsel) + sipariş no + tarih + durum chip + toplam + "Detay" button

5. **Bunlar ilginizi çekebilir** (4 ürün carousel — anasayfa kart stili)

6. **Yardım kart** bg-primary/5 rounded-2xl p-5 flex:
   - Sol: ikon + "Sorun mu yaşıyorsunuz?"
   - Sağ: 2 CTA mini — WhatsApp + İletişim

Tüm hesap sayfalarında ortak pattern: H1 + content grid; sidebar her zaman sol.
```

---

## L. Account/Orders + OrderDetail (Önerilen sıra: 10)

```
Şimdi 2 sipariş sayfasını birden tasarla.

1) Account/Orders
Sample data: 12 sipariş, sayfa 1/2, filtre durum chip'leri.
- Sol sidebar partial activeKey="orders"
- Sağ:
  * H1 "Siparişlerim" + sağda sipariş no arama input (rounded-full pill)
  * Filter chip bar (overflow-x-auto): Tümü (12), Beklemede (1), Hazırlanıyor (2), Kargoda (1), Teslim Edildi (7), İptal (1)
  * Sipariş kartı stack space-y-4:
    Her kart rounded-2xl border border-cream-300 p-6:
    - Üst flex justify-between items-center:
      * Sol: "Sipariş #ZB-2026-1234" font-mono + sm muted "23 Mayıs 2026"
      * Sağ: durum chip (Hazırlanıyor bg-secondary/15 text-secondary, Kargoda bg-accent/25 text-warning, Teslim Edildi bg-success/15 text-success, İptal bg-danger/10 text-danger)
    - Orta: 4-5 thumbnail strip aspect-square w-16 rounded-xl (görsel + "+3" overflow chip)
    - Alt flex justify-between items-center:
      * Sol: "5 ürün · 1.247₺" font-medium
      * Sağ: aksiyon button'lar (Detay outline primary, Kargoda ise "Takip Et" link, Teslim Edildi ise "Yine Al" + "İade Aç")
  * Pagination (kategori sayfasındaki gibi)
  * Boş durum: ayıcık SVG + "Henüz sipariş vermediniz" + "Alışverişe Başla" CTA

2) Account/OrderDetail
Sample data: Sipariş ZB-2026-1234, 5 ürün, Kargoda durumda, kargo Aras 1234567890123 takip no, tahmini teslimat 4 Haziran.
- Sidebar activeKey="orders"
- İçerik:
  * Breadcrumb: Hesabım › Siparişler › #ZB-2026-1234
  * Üst row: H1 + sağda button row: "Faturayı İndir" + "İade Aç" (eligible ise)
  * **Status timeline** card bg-cream rounded-3xl p-8:
    - 5 step yatay (mobil dikey): Alındı (24 May) → Onaylandı (24 May) → Hazırlanıyor (25 May) → Kargoda (26 May, AKTİF pulse animasyon) → Teslim Edildi (—)
    - Tamamlanan: bg-success daire + check, line bg-success
    - Aktif: bg-primary daire + pulse halka
    - Sonraki: bg-cream-300 border daire
  * **Kargo takibi** kart (durum Kargoda ise) bg-white border:
    - Aras Kargo logo + "1234567890123" mono + "Tahmini Teslim: 4 Haziran Çarşamba"
    - "Kargonu Takip Et" CTA primary
  * **Ürünler** card border rounded-2xl divide-y:
    - 5 satır: görsel 80x100 + bilgi (ad, variant, adet × birim) + sağda line total + "Yine Al" mini link, teslim edildi ise "Yorum Yaz"
  * 2 col alt md:grid-cols-2 gap-6:
    - **Teslimat Adresi** kart: tam adres + "Düzenlenemez" muted not
    - **Ödeme Bilgisi** kart: "Kredi Kartı ****1234" + ikon, taksit bilgisi
  * **Özet** card sticky-able sm:max-w-md ml-auto:
    - Ara toplam, Kargo, İndirim, Toplam
```

---

## M. Account/Addresses + Profile + Security + ChangePassword + TwoFactorSetup (Önerilen sıra: 11)

```
Şimdi 5 ayar sayfasını birden tasarla.

1) Account/Addresses
Sample data: 3 adres (Ev varsayılan, İş, Anneanne).
- Sidebar activeKey="addresses"
- H1 "Adreslerim" + sağda "+ Yeni Adres" primary button
- Card grid (md:grid-cols-2 gap-4):
  Her adres kart rounded-2xl border border-cream-300 p-6 (relative):
  - Üst: başlık (Ev) bold + IsDefault primary badge
  - Adres metni (3-4 satır)
  - Telefon mono
  - Alt sağ flex gap-2: "Düzenle" + "Sil" + (default değilse "Varsayılan Yap") mini link'ler text-sm
  - Hover: shadow-md
- Yeni adres modal/drawer: form Checkout'taki ile aynı

2) Account/Profile
Sample data: ayşe.yilmaz@example.com, doğum 1990-05-12, kadın, avatar yok, 2 çocuk (Selin 4 yaş, Ege 1 yaş).
- Sidebar activeKey="profile"
- H1 "Profil Bilgileri"
- **Avatar bölümü** card rounded-2xl p-6:
  - Sol: yuvarlak avatar 80x80 (baş harfler "AY", bg-primary/15)
  - Sağ: "Fotoğraf Yükle" outline + "Kaldır" muted link
- Form 2-col (sm:grid-cols-2 gap-4):
  - Ad, Soyad
  - E-posta (readonly muted + "Değiştir →" link)
  - Telefon (IMask)
  - Doğum tarihi (Flatpickr)
  - Cinsiyet radio: Kadın / Erkek / Belirtmek istemiyorum
- **Çocuklarım bölümü** kart bg-cream rounded-2xl p-6 (kids brand-specific!):
  - Başlık + açıklama: "Çocuklarınızı ekleyin, beden öneri ve yaş-uygun ürünleri kişiselleştirelim."
  - Çocuk kartları yatay: avatar (initial) + ad + yaş chip + "Düzenle" + "Sil" mini
  - "+ Çocuk Ekle" outline (mini ikon + metin) → modal: ad, doğum tarihi, cinsiyet
- Newsletter toggle
- "Değişiklikleri Kaydet" primary + "İptal" outline

3) Account/Security
Sample data: son şifre değişikliği 3 ay önce, 2FA Authenticator aktif, login alerts açık, 3 aktif oturum.
- Sidebar activeKey="security"
- H1 "Güvenlik"
- Card stack space-y-4 (her biri rounded-2xl border p-6):
  * Şifre kartı: "Son değişiklik: 3 ay önce" + "Şifreyi Değiştir" outline CTA
  * 2FA kartı: durum chip ("Aktif" success) + yöntem (Authenticator) + "Yapılandır" + "Kapat" danger outline
  * Login bildirimleri: toggle + açıklama
  * Aktif oturumlar: liste her satır cihaz ikonu + "MacBook Pro · İstanbul · 2 dk önce" + "Bu cihaz" badge primary varsa, diğerleri "Çıkış zorla" mini link
  * Veri & Hesap: "Verilerimi indir" outline + "Hesabımı sil" danger outline (modal onayı)

4) Account/ChangePassword
Layout: sidebar + ortalı form max-w-lg:
- H1 "Şifre Değiştir"
- Form: Mevcut şifre, Yeni şifre (göz + strength bar), Tekrar
- Kurallar listesi real-time check
- "Şifreyi Güncelle" + "İptal"

5) Account/TwoFactorSetup
Layout: sidebar + içerik. Wizard 4 adım stepper.
- Adım 1 — **Yöntem Seç**:
  * 2 büyük kart radio:
    - Authenticator app (önerilen badge primary): "Telefon uygulamasıyla, internetsiz çalışır" + telefon ikonu
    - SMS: "Telefon numaranıza SMS ile" + SMS ikonu
- Adım 2 — **Kurulum** (Authenticator):
  * QR kod büyük ortalı kart bg-white border p-8
  * Altta secret key mono kopya button
  * Talimat: "Google Authenticator, Authy, 1Password gibi uygulamanızla QR'ı tarayın."
- Adım 3 — **Doğrula**: 6 OTP input (TwoFactor sayfasındaki gibi)
- Adım 4 — **Yedek Kodlar**:
  * 10 kodluk grid (2-col, mono font, bg-cream rounded-lg p-3 her biri)
  * "İndir (.txt)" + "Kopyala" CTA
  * Warning kart bg-warning/10 rounded-xl p-4: "Bu kodları güvenli bir yerde saklayın. Telefonunuza erişiminiz olmadığında ihtiyaç duyacaksınız."
- "2FA'yı Aktifleştir" primary CTA
```

---

## N. Account/Returns + CreateReturn (Önerilen sıra: 12)

```
Şimdi iade sayfalarını tasarla.

1) Account/Returns
Sample data: 3 iade — 1 talep edildi, 1 kargoda, 1 tamamlandı.
- Sidebar activeKey="returns"
- H1 "İadelerim" + sağda "+ Yeni İade" outline
- Filter chip: Tümü / Talep / Onaylandı / Kargoda / Tamamlandı / Reddedildi
- Liste kartları (orders pattern):
  * Sol thumbnail strip + orta sipariş no + talep tarihi + durum chip + sağ iade tutarı + "Detay" button
- Boş: "Henüz iade talebiniz yok" + "İade nasıl çalışır?" mini açıklama kartı yanında

2) Account/CreateReturn
Sample data: Sipariş ZB-2026-1234, 5 ürün arasından iade için seç.
- Sidebar
- Üst: "İade Talebi Oluştur" H1 + alt muted "Sipariş #ZB-2026-1234"
- Layout: 2 col (md:grid-cols-3 gap-8)
  Sol col-span-2 form, Sağ col-span-1 sticky özet.

  Form 5 adım stack:
  1. **Ürün Seç** card bg-white border rounded-2xl divide-y:
     - Her ürün satırı checkbox + 80x100 görsel + ad + variant + adet seçici (max sipariş adedi) + line price (iade tutarı recalculate)
  2. **İade Sebebi** card: radio liste
     - "Bedeni uymadı" / "Beklediğim gibi değil" / "Hasarlı geldi" / "Yanlış ürün" / "Diğer"
     - "Diğer" seçilirse textarea açılır
  3. **Çözüm** radio:
     - "İade et (ücret iadesi)" / "Aynı ürün değişim (varsa)"
  4. **Fotoğraf yükle** (hasar/yanlış ürün için opsiyonel):
     - Drag-drop alanı dashed border rounded-2xl py-12
     - Preview grid thumbnail + X kaldır
  5. **İade Adresi**:
     - Mevcut adres dropdown (Tom Select) + "Yeni Adres Ekle" link
  
  Sağ özet sticky lg:top-24:
  - Card bg-cream rounded-2xl p-6
  - "İade Özeti" başlık
  - Seçilen ürünler mini liste (görsel + ad + adet)
  - Tahmini iade tutarı (primary text-2xl)
  - Kargo notu: "Kargo ücreti tarafımızca karşılanır" success ikon
  - "İade Talebi Oluştur" primary full CTA
```

---

## O. Account/Wallet + LoyaltyPoints + Referral + BuyAgain (Önerilen sıra: 13)

```
Şimdi 4 hesap sayfasını birden tasarla.

1) Account/Wallet
Sample data: 250₺ bakiye, 12 hareket.
- Sidebar
- **Bakiye kartı** büyük rounded-3xl py-12 bg-gradient-to-br from-primary via-primary/80 to-secondary text-white relative overflow-hidden:
  - Üst sağ: dekoratif yıldızlar veya bulut SVG opacity-30
  - "Cüzdan Bakiyesi" küçük etiket
  - "250,00₺" Fraunces text-5xl mt-2
  - 2 CTA row mt-6: "Bakiye Yükle" white pill button + "Çek" outline white pill
- 3 mini stat card (md:grid-cols-3 gap-4):
  - Yüklenen: "850₺"
  - Harcanan: "600₺"
  - Cashback: "+45₺" success
- **Hareketler** card:
  - Filter chip: Tümü / Yükleme / Harcama / İade / Cashback
  - Tablo benzeri liste, her satır:
    * Sol ikon (yükleme yeşil ok yukarı, harcama mor sepet, iade mavi geri ok, cashback sarı yıldız)
    * Orta: tarih + açıklama (linked: "Sipariş #ZB-2026-1234")
    * Sağ: tutar (+/− işaretli renkli)
  - Tıkla satır genişle: detaylar

2) Account/LoyaltyPoints
Sample data: 1.250 puan, Bronze tier, Silver'a 750 kaldı.
- Sidebar
- **Puan kartı** gradient kart (Wallet ile aynı stil):
  - "Sadakat Puanlarınız" küçük
  - "1.250" Fraunces text-5xl
  - "Bronze Üye" tier rozet
  - Progress bar (Bronze → Silver): %62 dolu
  - Alt: "Silver olmak için 750 puan daha"
- **Nasıl Kazanılır?** 4 kart grid (md:grid-cols-4 gap-4) — her kart farklı pastel:
  - "Alışveriş Yap": 1₺ = 1 puan (sepet ikonu)
  - "Yorum Yaz": +50 puan / yorum (yıldız ikonu)
  - "Arkadaş Çağır": +200 puan / üye olunan davet (kişi+ ikonu)
  - "Doğum Günün": +500 puan / yıllık (hediye ikonu)
- **Nasıl Kullanılır?** mini info kart bg-primary/10:
  - "100 puan = 10₺ indirim. Ödeme adımında otomatik uygulanır."
- **Puan Geçmişi** liste: tarih + açıklama + +/− puan

3) Account/Referral
Sample data: Davet kodu "AYSE2026", davet linki, 5 davet ettim, 3'ü üye oldu, 2 ilk sipariş verdi, 600 puan kazandım.
- Sidebar
- **Hero card** rounded-3xl bg-primary/10 p-10:
  - H1 "Arkadaşını Çağır, İkiniz Kazanın"
  - Açıklama: "Davet kodunuzla kayıt olan arkadaşınız ilk siparişine 100₺ indirim, siz 200 puan kazanırsınız."
  - **Davet kodu kutu** card bg-white rounded-2xl p-5:
    - "Davet Kodunuz" üst etiket
    - "AYSE2026" mono Fraunces text-3xl + "Kopyala" button (Notyf toast "Kopyalandı!")
  - **Davet linki** card alt:
    - input readonly + "Kopyala" + "Paylaş" dropdown
  - Paylaş row (4 ikon button):
    - WhatsApp (yeşil), E-posta, X/Twitter, Link kopyala
- **İstatistik row** (md:grid-cols-3 gap-4): Davet ettim (5) / Kayıt olan (3) / İlk sipariş (2) ve kazanım (600 puan)
- **Davet Listem** tablo: e-posta + tarih + durum chip + kazanılan

4) Account/BuyAgain
Sample data: 12 daha önce alınmış ürün.
- Sidebar
- H1 "Yine Al" + alt "Daha önce sevdiğiniz ürünleri tekrar sipariş edin"
- Filter chip: Tümü / Bu sezon / Bedeni değişmedi / Stoğa düştü
- Ürün grid (anasayfa product card stili) + her kartta "Yeniden Sepete Ekle" mini CTA altta
- Boş: "İlk siparişinizden sonra burada eski favorileriniz görünecek."
```

---

## P. Wishlist + Compare (Önerilen sıra: 14)

```
İki sayfa.

1) Wishlist/Index
Sample data: 8 favori ürün.
- Sidebar (account altı veya tam genişlik — Hesabım menüsünde olduğu için sidebar göster)
- H1 "Favorilerim (8 ürün)"
- Üst sağ: 2 mini CTA — "Listeyi Paylaş" (link kopyala/WA dropdown) + "Tümünü Sepete Ekle" (stokta olanları) outline primary
- Ürün grid (anasayfa kart stili). Her kartta kalp DOLU default, tıkla kaldır.
- Boş: SVG illustration + "Favoriler listeniz boş" + "Alışverişe Başla" CTA

2) Compare/Index (@model List<StorefrontProductDetailDto> — max 4 ürün)
Sample: 3 ürün karşılaştırma.
- H1 "Ürünleri Karşılaştır"
- **Sticky üst kart row** (md:grid-cols-4 gap-4 sticky lg:top-20 bg-white py-4 z-30):
  - Her sütunda kart: görsel aspect-square rounded-2xl + ad font-medium 2 satır + fiyat primary + "Sepete Ekle" outline mini CTA + sağ üst X (listeden çıkar)
  - Boş slot: dashed border + "+ Ürün Ekle" → kategori arama açar
- **Karşılaştırma matrix** tablo:
  - Sol kolon kalın satır başlığı (Marka, Yaş aralığı, Beden, Renk, Kumaş, Yıkama, Üretim, Sezon, Rating)
  - Sağ kolonlar her ürün için değer
  - Farklı değerler bold highlight
  - Mobilde yatay scroll
- Boş durum: 2'den az ürün varsa kart + "Karşılaştıracak ürün eklemediniz" + "Kataloğa göz at" CTA
```

---

## R. Seller (Marketplace) Tüm Sayfalar (Önerilen sıra: 15 — toplu)

```
Çok satıcılı (marketplace) tarafının tüm sayfalarını tek seferde tasarla. Chrome aynı.

Bu sayfalar küçük bir ekibin küçük admin panelidir — minimal, fonksiyonel, kid-brand'a uygun.

1) Seller/Register — Satıcı başvurusu (form-heavy)
Sample data: Boş form.
- Ortalı max-w-3xl py-12
- Hero sade: H1 "Satıcı Olun, Türkiye'ye Ulaşın" Fraunces + alt açıklama
- 3 fayda chip row (bg-primary/10 text-primary pill): "Kolay Liste · Düşük Komisyon · Pazarlama Desteği"
- Form gruplar (accordion veya kart):
  * **Firma Bilgileri**: Firma adı, vergi no/TC, vergi dairesi, MERSİS, Şahıs/Limited radio
  * **İletişim**: Yetkili ad-soyad, e-posta, telefon (IMask), KEP
  * **Mağaza**: Mağaza adı (slug otomatik altta gösterilir), logo upload (drag-drop preview), kısa açıklama textarea (280 karakter)
  * **Banka**: IBAN (IMask), hesap sahibi
  * **Kategoriler**: Tom Select multi (Kız giyim, Erkek giyim, Bebek, Aksesuar, Ayakkabı, Ayakkabı)
  * **Belgeler**: Vergi levhası upload, imza sirküleri upload, ticaret sicil upload
  * "Satıcı Sözleşmesi'ni okudum" checkbox link → modal
- "Başvuruyu Gönder" primary full

2) Seller/Pending — Onay bekliyor
Sample data: ZekidsBebe'ye 25 Mayıs'ta başvuru, 2-3 iş günü inceleme.
- Ortalı card max-w-2xl py-16
- Üstte yumuşak saat ikonu bg-secondary/15
- H1 "Başvurunuz Alındı"
- "Mağaza başvurunuzu inceliyoruz. Genellikle 2-3 iş günü içinde sonuçlanır."
- 4 adım stepper yatay: Başvuru Alındı (check) → İnceleniyor (aktif pulse) → Onay → Mağaza Aktif
- 2 CTA: "Bilgilerimi Güncelle" outline + "İletişim" outline
- Alt: "Bu süreçte bizi tanıyın — Satıcı Rehberi" link card

3) Seller/Panel — Dashboard (en kompleks)
Sample data:
- Bugün: 8 sipariş, 2.450₺ gelir, 245 aktif ürün, 5 bekleyen sipariş, 4.7 puan, %3.2 dönüşüm.
- Grafik: son 7 gün gelir (mock data).
- Son 5 sipariş + en çok satan 5 ürün.

Layout: SAĞ TARAFTA SEYRENTİ — Storefront chrome'a ek olarak SATICI SIDEBAR (sol):
- Satıcı sidebar (lg:w-64 sticky lg:top-24, ana storefront sidebar'dan FARKLI):
  * Üst: mağaza logo + ad + "Mağaza Profilini Gör" link
  * Menü: Panel (aktif) / Ürünlerim / + Ürün Ekle / Siparişlerim / Bakiye / Profil / Çıkış
- İçerik (sağ):
  * H1 "Mağaza Paneli" + sağda tarih range picker (Flatpickr "Son 7 gün" varsayılan)
  * **Metric grid** (md:grid-cols-3 lg:grid-cols-6 gap-4):
    Her kart rounded-2xl p-5 farklı pastel:
    - "Bugün Gelir" 2.450₺ + trend chip +12% yeşil
    - "Bugün Sipariş" 8 + +2 dün
    - "Aktif Ürün" 245
    - "Bekleyen Sipariş" 5 (turuncu warning)
    - "Mağaza Puanı" 4.7 + yıldız
    - "Dönüşüm" %3.2 + trend
  * **Gelir Grafiği** kart bg-white rounded-2xl p-6:
    - Başlık "Son 7 Gün Gelir"
    - SVG polyline çizgi grafik (X: günler, Y: ₺) — primary color, gradient fill altında
    - X axis: 7 gün etiketi, Y axis: 0/500/1000/2000/3000
  * **Aksiyon kart** bg-warning/10 border border-warning rounded-2xl p-5:
    - Sol: warning ikon + "5 yeni siparişiniz bekliyor"
    - Sağ: "Görüntüle" primary CTA
  * **Son Siparişler** card border divide-y:
    - 5 satır: # + alıcı (masked Ayşe Y.) + ürün adedi + tutar + durum chip + "Detay" mini
  * **En çok satan ürünler** card border divide-y:
    - 5 satır: thumbnail + ad + adet + gelir

4) Seller/Profile
Sample data: mağaza adı, logo, banner, açıklama, kategori, kargo/iade politikası.
- Satıcı sidebar + sağ form
- H1 "Mağaza Profili"
- 2-col form:
  * Banner upload (drag-drop, aspect-[16/5])
  * Logo upload (yuvarlak preview)
  * Mağaza adı, slug
  * Kısa açıklama (280 char counter)
  * Uzun açıklama (rich text basic)
  * Kategoriler (Tom Select multi)
  * Kargo politikası textarea
  * İade politikası textarea
  * İletişim (telefon, email, WhatsApp)
  * Sosyal medya (Instagram, Facebook URL'leri)
- "Kaydet" primary

5) Seller/Orders
- Satıcı sidebar
- Account/Orders'a benzer ama satıcı için + satır inline button'lar: "Hazırlandı" / "Kargoya Ver" / "İptal Et"
- Filter chip: Bekleyen / Onaylandı / Hazırlanıyor / Kargoda / Tamamlandı / İptal / İade

6) Seller/OrderDetail (@model Entegrasyon.Entity.Orders.Order)
- Satıcı sidebar
- Account/OrderDetail'a benzer ama satıcı için + üstte aksiyon button row:
  "Hazırlandı Olarak İşaretle" / "Kargo Bilgisi Gir" (modal: kargo firma Tom Select + takip no) / "Faturayı Görüntüle" / "İade Talebi Aç"
- Müşteri bilgisi masked: "Ayşe Y." + masked telefon "+90 5XX XXX XX 67"

7) Seller/Balance
- Satıcı sidebar
- Bakiye kartı (Wallet stiline benzer, primary gradient):
  - "Kullanılabilir" 1.450₺ Fraunces
  - 2 sub-stat: "Beklemede" 850₺ + "Toplam Kazanç" 12.450₺
  - "Çekim Talep Et" primary CTA
- Komisyon kartı: "Komisyon Oranı: %12" + açıklama
- Hareketler tablo: tarih, sipariş no, brüt, komisyon (-), net, durum chip

8) SellerProduct/Index (@model List<SellerProduct>)
- Satıcı sidebar
- H1 "Ürünlerim" + "+ Yeni Ürün" primary
- Filter: Aktif / Pasif / Stok Bitti / Onay Bekliyor
- Tablo:
  - Sütunlar: Checkbox | Görsel 50x50 | Ürün adı (slug alt) | SKU | Fiyat | Stok | Durum chip | Eylem dropdown (Düzenle, Pasif Yap, Sil)
  - Hover bg-cream/50
- Toplu seçim aksiyon bar (sticky alt): "X seçildi · Toplu Pasif Yap / Sil / Fiyat Güncelle"
- Boş: "Henüz ürün eklemediniz" + "İlk Ürünü Ekle" CTA

9) SellerProduct/Add (@model List<Product> — wizard)
- Satıcı sidebar
- Stepper üstte 4 adım: Temel / Görseller / Varyantlar / SEO & Yayın
- Layout: sol form (2/3) + sağ sticky preview kart (1/3) — preview anasayfa ürün kartı stilinde
- **Adım 1 — Temel**:
  * Ürün adı (slug otomatik altta)
  * Marka Tom Select
  * Kategori Tom Select ağaç (Kız → Üst Giyim → Tişört)
  * Yaş aralığı chip multi (10 chip)
  * Cinsiyet radio
  * Mevsim chip multi (4 chip)
  * Kısa açıklama textarea
  * Uzun açıklama (basit rich text: bold/italic/list)
- **Adım 2 — Görseller**:
  * Drag-drop multi upload (kapak: ilk yüklenen veya seç)
  * SortableJS sıralama
  * Her görsele alt text input
- **Adım 3 — Varyantlar**:
  * Beden chip multi seç
  * Renk input row: ad + hex picker + ekle button
  * Otomatik matrix tablo (beden × renk): her hücre genişler: SKU input, fiyat, eski fiyat, stok, barkod
  * Toplu fiyat/stok düzenle button (en üstte)
- **Adım 4 — SEO & Yayın**:
  * SEO slug
  * Meta title (60 char sayaç)
  * Meta description (160 char sayaç)
  * Etiketler Tom Select multi
  * "Hemen Yayınla" / "Taslak Kaydet" radio
- "Kaydet" primary full
```

---

## S. GiftCard Üçlüsü + Contact + Tracking İkilisi + Page + Error (Önerilen sıra: 16 — toplu)

```
Kalan utility sayfalarını tek seferde tasarla.

1) GiftCard/Index — Hediye çeki satın al
Sample data: 6 tema (Doğum Günü, Bebek Hoş Geldin, Bayram, Açılış, Sevgiyle, Yeni Yıl).
- Ortalı max-w-4xl py-12
- **Hero kart** bg-primary/10 rounded-3xl p-10 relative:
  - Sol: H1 Fraunces "Zekids Bebe Hediye Çeki" + açıklama "Sevdiklerinize özel hediye çekiyle alışveriş özgürlüğü hediye edin."
  - Sağ: 3D rotated hediye çeki kart görseli (CSS gradient + Fraunces logo, hafif rotate-[-3deg])
- **Tasarım Seç** carousel: 6 tema kart yan yana scroll-snap, her biri rounded-2xl aspect-[3/4] tema illustration + tema adı, seçili ring-2 ring-primary
- **Tutar Seç**: chip row (rounded-full border px-5 py-2):
  - 100₺ / 250₺ / 500₺ / 1.000₺ / 2.500₺ / "Diğer" → input açılır
- **Alıcı bilgileri** form:
  - Alıcı ad-soyad
  - Alıcı e-posta
  - Teslimat tarihi (Flatpickr — bugün veya ileri)
  - "Mesajınız" textarea (200 char sayaç)
- **Önizleme** kart altta: gerçek zamanlı çek görüntüsü güncelleniyor (alıcı adı, tutar, mesaj görünür)
- "Hediye Çekini Sepete Ekle" primary full CTA

2) GiftCard/Created — Çek oluştu
- Ortalı card max-w-2xl py-16
- Yumuşak success ikonu
- H1 "Hediye Çekiniz Hazır"
- "{RecipientName} adına {DeliveryDate} tarihinde gönderilecek."
- **Büyük çek önizlemesi** card (brand themed gradient + tema görseli + alıcı + tutar + kod mono)
- Kod kutu: mono font + "Kopyala" + "PDF İndir" buttonlar
- 2 CTA: "Başka Çek Al" + "Anasayfa"

3) GiftCard/Balance — Bakiye sorgula
- Ortalı card max-w-md py-12
- H1 "Hediye Çeki Bakiyem"
- Input: çek kodu (auto-uppercase mask)
- "Sorgula" CTA
- Sonuç state'i (POST sonrası):
  - Card bg-success/10 rounded-2xl p-6:
    - "Mevcut Bakiye" üst etiket
    - "250₺" Fraunces text-4xl primary
    - "Son kullanma: 31 Aralık 2027"
- Yardım not: "Çek kodunuzu satın aldığınız e-postada bulabilirsiniz."

4) Contact/Index — İletişim
Sample data: Tenant'tan ContactPhone, WhatsApp, Email, Address, City, MapEmbedUrl.
- Breadcrumb + H1 "Bize Ulaşın"
- 2 col grid (md:grid-cols-2 gap-12):
  - **Sol form** card bg-white border rounded-2xl p-8:
    * Ad-soyad, e-posta, telefon (IMask) sm:grid-cols-2
    * Konu Tom Select: "Sipariş Sorusu / İade / Ürün Önerisi / Kurumsal / Diğer"
    * Mesaj textarea (1000 char sayaç)
    * KVKK onay checkbox + link
    * "Gönder" primary full CTA
  - **Sağ bilgi** stack space-y-4:
    * Telefon kart: ikon + "0850 000 00 00" + "Hafta içi 09:00-18:00" muted
    * WhatsApp kart bg-success/10: ikon + "Aynı saatlerde anlık" + "WhatsApp'ta yaz" link
    * E-posta kart: ikon + email
    * Adres kart: ikon + adres + Google Maps embed altta (aspect-[16/9] rounded-2xl iframe)

5) Tracking/Index — Kargo takip form
- Ortalı card max-w-lg py-12
- H1 "Kargonuzu Takip Edin"
- Açıklama: "Sipariş numaranız veya kargo takip kodunuzla anlık durumu öğrenin."
- Input pill: "Sipariş veya takip no"
- "Sorgula" primary CTA
- Alt not muted: "Sipariş numarası satın alma e-postanızda yer alır."

6) Tracking/Result — Sonuç
Sample data: ZB-2026-1234, Kargoda, Aras 1234567890123, tahmini 4 Haziran, 5 hareket kaydı.
- Ortalı max-w-3xl
- **Üst durum kartı** bg-cream rounded-3xl p-8:
  - Carrier logo (Aras) + takip no mono + "Tahmini: 4 Haziran Çarşamba"
  - Büyük durum chip "Kargoda" bg-accent/25 text-warning
- **Timeline** dikey büyük card bg-white border:
  - 6 step: Sipariş Alındı (24 May 14:32) → Onaylandı (24 May 15:01) → Hazırlanıyor (25 May 09:15) → Kargoya Verildi (26 May 11:30) → Dağıtım Merkezi (27 May 06:45, AKTİF pulse) → Teslim (—)
  - Her step yuvarlak ikon + bg renkli (tamamlanmışlar success, aktif primary pulse, sonraki muted)
  - Step yanında: lokasyon + saat
- **Detaylı hareketler** card border divide-y:
  - 5 satır: timestamp + lokasyon + açıklama
- 2 CTA: "Kargocuyu Ara" outline + "Sipariş Detayına Git"
- Bulunamadıysa: empty state "Kayıt bulunamadı, kodu kontrol edin"

7) Page/Show — CMS statik
Sample data: KVKK / Hakkımızda / İade Politikası gibi.
- Ortalı max-w-4xl py-12
- Breadcrumb
- H1 Fraunces: Page.Title
- Son güncelleme muted küçük
- **Prose içerik** article class="prose prose-lg max-w-none":
  - @Html.Raw(Page.ContentHtml) — Tailwind typography ile p, h2, h3, ul, ol, blockquote, code stilleri
  - Renk overrides: prose-headings:text-charcoal prose-headings:font-heading prose-a:text-primary prose-strong:text-charcoal
- Footer alt link kart: "Yardım gerekirse iletişim" → /İletişim

SSS özel görünüm (Page slug "sss" ise):
- Soru-cevap accordion liste
- Her details/summary açılınca cevap fade-in

8) Error/Index — Hata sayfası
@ViewBag.StatusCode (404, 500, 503)
- Ortalı card py-20 max-w-2xl
- StatusCode'a göre:
  * **404**: 
    - SVG illustration: kaybolmuş bir ayıcık balonun peşinden uçuyor (Fraunces çocuk vibe, abartısız)
    - H1 "Buralarda kimse yok"
    - Açıklama: "Aradığınız sayfa taşınmış olabilir."
    - 2 CTA: "Anasayfa" primary + "Kataloğa Git" outline
  * **500/503**:
    - SVG illustration: ufak makine ayıcık tamir ediyor
    - H1 "Bir şeyler ters gitti"
    - "Hata kaydedildi, en kısa sürede çözeceğiz."
    - 2 CTA: "Tekrar Dene" primary + "Anasayfa" outline
- Altta arama kutusu kart: "Belki şunu mu arıyordunuz?" + input
```

---

## Önerilen tüm üretim sırası (özet)

| # | Sayfa | Prompt | Bağımlı |
|---|---|---|---|
| 1 | Anasayfa | (orijinal, üretildi ✓) | — |
| 2 | Ürün Detay | B | — |
| 3 | Kategori Listeleme | C | — |
| 4 | Sepet | D | — |
| 5 | Checkout | E | — |
| 6 | Checkout Başarılı | F | E |
| 7 | Checkout Başarısız | G | E |
| 8 | Login | H | — |
| 9 | Register | I | H |
| 10 | ForgotPassword + ResetPassword + ConfirmEmail + TwoFactor (4 sayfa) | J | H |
| 11 | Account Hub (Sidebar + Dashboard) | K | — |
| 12 | Account Orders + OrderDetail | L | K |
| 13 | Account Settings (5 sayfa) | M | K |
| 14 | Account Returns + CreateReturn | N | K |
| 15 | Account Wallet + Loyalty + Referral + BuyAgain | O | K |
| 16 | Wishlist + Compare | P | — |
| 17 | Seller (9 sayfa, toplu) | R | — |
| 18 | GiftCard + Contact + Tracking + Page + Error (8 sayfa) | S | — |

**Verim ipucu:** Promptlar B-S sırasını birlikte takip edersen aynı konuşmada chrome cache'i Claude design'da kalır → süreklilik direktifini her seferinde tekrarlamana gerek yok.

**Çıktıları bana atarken:** Tek seferde 3-5 dosya gönderebilirsin; toplu Razor extraction çok daha hızlı oluyor.
