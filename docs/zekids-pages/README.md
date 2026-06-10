# Zekids Storefront — Tasarım Bekleyen Sayfalar

Bu klasördeki her `.md` dosyası **henüz Claude design'da üretilmemiş** bir sayfanın brief'idir. Sayfa tasarlandıktan ve Razor'a çevrildikten sonra dosya silinir; klasörde kalanlar = hâlâ üretilmesi gerekenler.

## Süreklilik Direktifi

Yeni konuşma açtığında [`../storefront-design-prompts.md`](../storefront-design-prompts.md) içindeki **0. Tasarım Sistemi** (palet, Fraunces+Inter, Zekids Bebe tonu, çocuk giyim) ve **A. Süreklilik Direktifi** bloklarını ilk yapıştırılan şey olarak ver. Her MD'deki prompt bloğu bu iki blok zaten yapıştırıldığı varsayımıyla yazılmıştır — Süreklilik Direktifi MD'lere kopyalanmadı.

## Durum tablosu

| Sayfa | Hedef view | Öncelik | MD |
|---|---|---|---|
| Tüm Kategoriler | Views/Catalog/Categories.cshtml | high | [link](catalog-categories.md) |
| Arama Sonuçları | Views/Catalog/Search.cshtml | high | [link](catalog-search.md) |
| Satıcı Mağazası | Views/Catalog/SellerStore.cshtml | medium | [link](catalog-seller-store.md) |
| Karşılaştır | Views/Compare/Index.cshtml | medium | [link](compare-index.md) |
| Şifremi Unuttum | Views/Auth/ForgotPassword.cshtml | high | [link](auth-forgot-password.md) |
| Şifre Sıfırla | Views/Auth/ResetPassword.cshtml | high | [link](auth-reset-password.md) |
| E-posta Onayı | Views/Auth/ConfirmEmail.cshtml | high | [link](auth-confirm-email.md) |
| İki Aşamalı (Giriş) | Views/Auth/TwoFactor.cshtml | high | [link](auth-two-factor.md) |
| Cüzdanım | Views/Account/Wallet.cshtml | medium | [link](account-wallet.md) |
| Sadakat Puanları | Views/Account/LoyaltyPoints.cshtml | medium | [link](account-loyalty.md) |
| Arkadaş Davet Et | Views/Account/Referral.cshtml | medium | [link](account-referral.md) |
| Yine Al | Views/Account/BuyAgain.cshtml | medium | [link](account-buy-again.md) |
| Hediye Çeki Satın Al | Views/GiftCard/Index.cshtml | medium | [link](gift-card-index.md) |
| Hediye Çeki Oluşturuldu | Views/GiftCard/Created.cshtml | medium | [link](gift-card-created.md) |
| Hediye Çeki Bakiye | Views/GiftCard/Balance.cshtml | medium | [link](gift-card-balance.md) |
| İletişim | Views/Contact/Index.cshtml | high | [link](contact-index.md) |
| Kargo Takip Form | Views/Tracking/Index.cshtml | high | [link](tracking-index.md) |
| Kargo Takip Sonuç | Views/Tracking/Result.cshtml | high | [link](tracking-result.md) |
| CMS Sayfa | Views/Page/Show.cshtml | high | [link](page-show.md) |
| Hata (404/500) | Views/Error/Index.cshtml | high | [link](error-index.md) |
| Satıcı Kayıt | Views/Seller/Register.cshtml | low | [link](seller-register.md) |
| Satıcı Onay Bekliyor | Views/Seller/Pending.cshtml | low | [link](seller-pending.md) |
| Satıcı Paneli | Views/Seller/Panel.cshtml | low | [link](seller-panel.md) |
| Satıcı Profili | Views/Seller/Profile.cshtml | low | [link](seller-profile.md) |
| Satıcı Siparişleri | Views/Seller/Orders.cshtml | low | [link](seller-orders.md) |
| Satıcı Sipariş Detay | Views/Seller/OrderDetail.cshtml | low | [link](seller-order-detail.md) |
| Satıcı Bakiye | Views/Seller/Balance.cshtml | low | [link](seller-balance.md) |
| Satıcı Ürünleri | Views/SellerProduct/Index.cshtml | low | [link](seller-product-index.md) |
| Satıcı Ürün Ekle | Views/SellerProduct/Add.cshtml | low | [link](seller-product-add.md) |

**Kalan:** 29 sayfa.

## İş akışı (her sayfa için)

1. İlgili MD'deki prompt bloğunu (Süreklilik Direktifi + 0. Tasarım Sistemi yapıştırıldıktan sonra) Claude design'a ver.
2. Çıktı HTML'i `İndirilenler/zekids/<Ad>.html` olarak indir.
3. Uygun `agent-...` ajanına Razor + JS çevirisi için ver.
4. View üretildikten sonra bu MD'yi sil.

## Tamamlanan sayfalar (referans)

| ✓ Sayfa | View |
|---|---|
| Anasayfa | Views/Home/Index.cshtml |
| Ürün Detay | Views/Product/Detail.cshtml |
| Kategori | Views/Catalog/Category.cshtml |
| Sepet | Views/Cart/Index.cshtml |
| Ödeme | Views/Checkout/Index.cshtml |
| Sipariş Başarılı | Views/Checkout/Basarili.cshtml |
| Sipariş Başarısız | Views/Checkout/başarısız.cshtml |
| Giriş | Views/Auth/Login.cshtml |
| Kayıt | Views/Auth/Register.cshtml |
| Hesabım | Views/Account/Index.cshtml |
| Siparişlerim | Views/Account/Orders.cshtml |
| Sipariş Detay | Views/Account/OrderDetail.cshtml |
| Adreslerim | Views/Account/Addresses.cshtml |
| Profil | Views/Account/Profile.cshtml |
| Güvenlik | Views/Account/Security.cshtml |
| Şifre Değiştir | Views/Account/ChangePassword.cshtml |
| İki Aşamalı Kurulum | Views/Account/TwoFactorSetup.cshtml |
| Favorilerim | Views/Wishlist/Index.cshtml |
| İadelerim | Views/Account/Returns.cshtml |
| İade Oluştur | Views/Account/CreateReturn.cshtml |
</content>
</invoke>
