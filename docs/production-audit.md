# Storefront Production-Ready Audit

> Hazırlayan: `production-auditor` (Task #3) · Tarih: 2026-06-01
> Kapsam: `Application/Entegrasyon.Storefront`
> Build durumu: `dotnet build Entegrasyon.sln` → **0 Warning / 0 Error**. Tailwind `site.css` yeniden derlendi.

Bu rapor iki bölüm: (1) **bu audit sırasında uygulanan fix'ler**, (2) **kalan iş** (kritiklik + önerilen aksiyon + sahip). Backend genişletmesi gereken maddeler `backend-gaps` / `team-lead` içindir; küçük view/route fix'leri bu agent tarafından uygulandı.

---

## 1. Uygulanan Fix'ler ✅

| # | Dosya | Yapılan | Not |
|---|---|---|---|
| F1 | `Views/Compare/Index.cshtml` | **`Karsilastir.html` → tam Razor dönüşümü.** Placeholder (3 satır) yerine server-rendered karşılaştırma sayfası: sticky ürün kartı satırı + özellik matrisi + boş durum. Matris satırları `StorefrontProductDetailDto.Attributes` birleşiminden üretiliyor; farklı değerler koyu + `bg-primary/[0.04]` ile vurgulanıyor. Marka / Kategori / Fiyat / Stok satırları + Sepete Ekle / Tükendi aksiyonları. | Layout zaten header/footer/mega/toast sağlıyor; view yalnızca `<main>` içeriği. Model `List<StorefrontProductDetailDto>` (controller `?ids=` ile dolduruyor). |
| F2 | `wwwroot/js/compare.js` | Karşılaştırma sayfası kontrolcüsü eklendi (`[data-compare-page]`): **çıkar / temizle / sepete ekle** butonları `data-*` binding ile (inline JS yok). Çıkar → `ids` URL'ini güncelleyip yeniden yükler; temizle → `/karsilastir`. Sayfada gereksiz floating compare-bar bastırıldı. | `?ids=` tek doğruluk kaynağı. Sepete ekle storefront genelindeki davranışla tutarlı: şimdilik toast. |
| F3 | `Controllers/AccountController.cs` | **`OnActionExecuting` override** — `ViewBag.CustomerName` (Name claim) + `ViewBag.CustomerTier` TÜM hesap action'larında merkezî dolduruluyor. `Index()` içindeki tekil set kaldırıldı. | Artık hiçbir hesap sayfası sidebar'da demo "Ayşe Yılmaz" göstermiyor. Tier için backend claim yok → nötr "Üye" (bkz. R2). |
| F4 | `Views/Account/_AccountSidebar.cshtml` | Fallback değerleri demo `"Ayşe Yılmaz"` / `"Bronze Üye"` → nötr `"Üye"`. | Savunma amaçlı; normalde F3 dolduruyor. |
| F5 | `Views/Account/Security.cshtml` | **Kırık route düzeltildi:** `asp-action="DownloadData"` → `"ExportData"` (gerçek action `GET /hesabim/veri-indir`). | "Verilerimi indir" linki artık çalışıyor (`<a>` → GET). |

---

## 2. Kalan İş 🔧

### 2.1 KRİTİK — Kırık form action'ları / controller↔view kontrat uyumsuzlukları

Bunlar production'da **sessizce başarısız** olur (boş `action=""` → mevcut URL'e POST) veya **demo veri** gösterir. Çoğu backend genişletmesi gerektirir → `backend-gaps`.

| # | Dosya / satır | Sorun | Kritiklik | Önerilen aksiyon | Sahip |
|---|---|---|---|---|---|
| R3 | `Views/Account/Profile.cshtml:53` | Form `asp-action="UpdateProfile"` → **böyle bir action YOK** (POST action adı `Profile`). Ayrıca form alanları `Name/Surname/Phone/BirthDate/Gender/Avatar/NewsletterOptIn` ≠ `StorefrontProfileDto(Name, Surname, PhoneNumber)` → `Phone` (DTO `PhoneNumber`), `BirthDate`, `Gender`, `Avatar`, `NewsletterOptIn` **bind olmaz / kaydedilmez**. | 🔴 Kritik | DTO'yu genişlet (BirthDate, Gender, NewsletterOptIn, Avatar) + form alan adını `PhoneNumber` yap + `asp-action="Profile"`. Avatar için upload. | backend-gaps |
| R4 | `Views/Account/Profile.cshtml` ViewBag | GET `Profile()` `ViewBag.Profile` (DTO) + `ViewBag.Email` set ediyor; view ise `ViewBag.Name/Surname/Phone/Email` okuyor → **ad/soyad/telefon her zaman demo** ("Ayşe"/"Yılmaz"/"0(532)..."). | 🔴 Kritik | Controller `ViewBag.Name/Surname/Phone/BirthDate/Gender/...` set etsin VEYA view `ViewBag.Profile` DTO'sunu okusun. | backend-gaps |
| R5 | `Views/Account/Profile.cshtml:91` | `asp-action="ChangeEmail"` → **action yok**. | 🟠 Yüksek | `ChangeEmail` GET/POST action + e-posta değişikliği akışı (doğrulama maili). | backend-gaps |
| R6 | `Views/Account/Addresses.cshtml` | Tüm sayfa **statik demo veri** (hardcoded 2 adres, "Ayşe Yılmaz"). Form `asp-action="SaveAddress"` → **action yok**. Adres manager'ı bağlı değil. | 🔴 Kritik | `Addresses()` GET gerçek adresleri çeksin + `SaveAddress`/`DeleteAddress`/`SetDefault` action'ları + adres manager. | backend-gaps |
| R7 | `Views/Account/Security.cshtml:142` | Hesap silme formu `asp-action="DeleteAccount"` → **action yok**. | 🟠 Yüksek | `DeleteAccount` POST + soft-delete/anonimleştirme (KVKK). | backend-gaps |
| R8 | `Views/Account/Security.cshtml` ViewBag | `LastPasswordChange`, `TwoFactorMethod`, `LoginAlerts` demo fallback; controller `ViewBag.LoginHistory` set ediyor ama view kullanmıyor (oturumlar `guvenlik.js` demo verisi). | 🟡 Orta | Controller gerçek son şifre değişikliği + 2FA yöntemi geçsin; aktif oturumlar `LoginHistory`'den render edilsin. | backend-gaps |

> ✔️ Antiforgery: tüm POST form'ları `asp-controller`/`asp-action` Tag Helper kullanıyor (token otomatik). Tek manuel form `Home/Index.cshtml` newsletter → `@Html.AntiForgeryToken()` mevcut. **Eksik token yok.**

### 2.2 ORTA — Placeholder view'lar (`@* tasarım bekleniyor *@`)

24 view hâlâ placeholder (≤2 satır). Bunlar controller action'larından `View()` ile dönülüyor → production'da **boş sayfa**. Backend hazır olanlar yalnızca tasarım/HTML dönüşümü bekliyor; backend'i olmayanlar hem view hem controller işi.

**Backend HAZIR — sadece HTML→Razor dönüşümü gerekiyor** (öncelik sırası: en çok kullanılan):

| View | Controller action | Kaynak HTML (İndirilenler/zekids) |
|---|---|---|
| `Catalog/Search.cshtml` | `CatalogController.Search` ✓ veri var | (Kategori.html'e benzer; özel arama tasarımı yoksa Kategori şablonu) |
| `Catalog/SellerStore.cshtml` | `CatalogController` seller store ✓ | — |
| `Contact/Index.cshtml` | `ContactController` ✓ | — |
| `Account/Referral.cshtml` | `AccountController.Referral` ✓ (ViewBag.ReferralCode/Referrals/Domain dolu) | `DavetEt.html` ✓ |
| `Auth/TwoFactor.cshtml` | `AuthController.TwoFactor` ✓ | `IkiAsamali.html` ✓ |
| `GiftCard/Index/Balance/Created` | `GiftCardController` ✓ | — |
| `Tracking/Index/Result` | `TrackingController` ✓ | — |
| `Page/Show.cshtml` | `PageController` (CMS) ✓ | — |
| `Error/Index.cshtml` | `ErrorController` | (basit hata sayfası) |

**Backend de gerekebilir (satıcı paneli)** — `Seller/*` (Balance, OrderDetail, Orders, Panel, Pending, Profile, Register), `SellerProduct/Add`, `SellerProduct/Index`: 9 view. Satıcı tarafı bütünüyle placeholder; controller'ların veri sağlayıp sağlamadığı ayrıca doğrulanmalı.

**Shared partial'lar** — `_Breadcrumb.cshtml`, `_FilterSidebar.cshtml`, `_Pagination.cshtml`: placeholder ama **kullanımda değiller** (sayfalar breadcrumb/filtre/sayfalamayı inline yapıyor). Ya doldurulup ortak kullanıma alınmalı ya silinmeli. 🟢 Düşük.

> Aksiyon: `page-converter` agent'ı bu listeyi sıradan alabilir. Backend-hazır olanlar hızlı; satıcı paneli ayrı epik.

### 2.3 DÜŞÜK — Hardcoded / demo değerler

| Konu | Yer | Durum / Aksiyon | Kritiklik |
|---|---|---|---|
| Layout iletişim bilgileri | `_Layout.cshtml` | ✅ Zaten `Tenant.Settings` (ContactPhone/Email/City/WhatsApp/sosyal) ile bağlı. Sorun yok. | — |
| Demo telefon `0(532) 123 45 67` | `Checkout/Index.cshtml`, `Account/OrderDetail.cshtml`, `Profile.cshtml` fallback | Controller gerçek müşteri/adres telefonu geçtiğinde çözülür (R3/R4/R6 ile birlikte). | 🟡 Orta |
| Statik adresler | `Addresses.cshtml` | R6 kapsamında. | 🔴 (R6) |
| `0850 000 00 00` fallback | `Checkout/Basarisiz.cshtml:6` | `settings?.ContactPhone ?? "0850 000 00 00"` — kabul edilebilir fallback. | 🟢 Düşük |
| **`onerror` → loremflickr demo görselleri** | `Account/CreateReturn.cshtml:81`, `Wishlist/Index.cshtml:85` | `img onerror` ile dış demo servisi (`loremflickr.com`) fallback. img fallback kabul edilebilir AMA production'da dış demo servisine bağımlılık istenmez → yerel placeholder görsele (`/img/...`) çevrilmeli. | 🟡 Orta |

> ✔️ Inline `onclick`/event handler taraması: **0** (tüm chrome `data-*` + `site.js`). `onerror` yalnızca yukarıdaki 2 img.
> ✔️ Leftover `.html` link (tasarım dosyası kalıntısı): **0**.

### 2.4 Tier (üyelik kademesi) — koordinasyon notu

`backend-gaps` `StorefrontSettings`'e `LoyaltyTierSilverMin` / `LoyaltyTierGoldMin` eşiklerini ekledi (Task #2), ama login'de bir tier **claim'i** yok. `AccountController.OnActionExecuting` `"MembershipTier"` claim'ini okuyor, yoksa nötr `"Üye"` gösteriyor.

**Öneri (backend-gaps):** Login'de loyalty puanını eşiklerle karşılaştırıp `MembershipTier` claim'i ekle (`Bronze/Silver/Gold`), VEYA ortak bir `IStorefrontLoyaltyManager.GetTierAsync` helper'ı çıkar. O zaman sidebar tier otomatik doğru görünür — view tarafında ek değişiklik gerekmez. 🟡 Orta.

---

## 3. Page-by-page durum özeti

| Sayfa grubu | Durum |
|---|---|
| Home, Cart, Checkout (Index/Başarılı/Başarısız), Product/Detail, Catalog/Category & Categories, Auth (Login/Register/ForgotPassword/ResetPassword/ConfirmEmail), Wishlist | ✅ Hazır (tasarım dönüşümü tamam) |
| **Compare/Index** | ✅ **Bu audit'te tamamlandı** |
| Account: Index, Orders, OrderDetail, Returns, CreateReturn, ChangePassword, LoyaltyPoints, Wallet, BuyAgain, TwoFactorSetup | ✅ Tasarım hazır (sidebar adı F3 ile gerçek) |
| Account: **Profile** | 🔴 Tasarım var ama veri/post kırık (R3/R4/R5) |
| Account: **Addresses** | 🔴 Statik demo, post kırık (R6) |
| Account: **Security** | 🟠 Tasarım var, DeleteAccount + bazı veriler eksik (R7/R8) |
| Account: Referral, Auth: TwoFactor | 🟡 Backend hazır, view placeholder |
| Catalog: Search, SellerStore · Contact · GiftCard · Tracking · Page/Show · Error | 🟡 Backend (çoğu) hazır, view placeholder |
| Seller/* + SellerProduct/* (11 view) | 🟠 Tümü placeholder; backend doğrulaması gerekli |
| Shared `_Breadcrumb`/`_FilterSidebar`/`_Pagination` | 🟢 Placeholder ama kullanılmıyor — doldur veya sil |
