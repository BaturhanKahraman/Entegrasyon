# Missing Pages Roadmap — Tam Sayfa Tamamlama Planı

**Tarih:** 2026-04-05
**Yaklaşım:** Genişlik Önce (Breadth-First) — tüm eksik sayfaların çalışan MVP'si, sonra derinlik
**Amaç:** Demo/satış hazırlığı — feature completeness + görsel bütünlük
**Kapsam:** Admin panel ağırlıklı, Storefront'tan highlight sayfalar
**Pazaryeri Odak:** Trendyol + Hepsiburada + N11

---

## Bağlam

Blazor → MVC migration sonrası business layer **157 interface** ile tam implement edilmiş durumda, ancak MVC tarafında sadece **25 feature folder** mevcut. Bu spec, expose edilmemiş servisleri ve rakiplerde olup bizde olmayan sayfaları 6 faz halinde tamamlamayı hedefler.

### Kararlar

- **Yaklaşım:** Her sayfa minimal ama çalışan (CRUD + list). Gelişmiş özellikler (bulk, export, chart) sonraki iterasyonlarda.
- **Test:** Her sayfa en az 1 controller unit test + 1 integration test (TDD kuralı korunur, ama advanced edge case'ler sonraya kalır).
- **Demo verisi:** Her faz sonunda seed/migration ile demo'ya hazır veri.
- **Nav:** Her yeni sayfa sidebar'a eklenir — hiçbir menü boş kalmaz.

### Sayfa Tasarım Standardı

Her yeni sayfa şu pattern'i takip eder:

1. **Controller** — Feature folder'da, primary constructor DI
2. **Views** — Tabler UI + HTMX: `Index.cshtml` (liste) + `Detail.cshtml` / `Create.cshtml` / `Edit.cshtml`
3. **ViewModels** — Feature folder içinde, Mapperly ile mapping
4. **Nav Menu** — `_SidebarNav` partial'a ekleme
5. **Test** — Controller unit test + integration test (TDD)
6. **Demo Verisi** — Seed data veya migration

### Rendering Pattern

- **Liste sayfası:** Tabler `table-hover` + `card-table` + arama + sayfalama
- **Detay sayfası:** Kart tabanlı bilgi gösterimi
- **Form sayfası:** Tabler form components + FluentValidation + PRG pattern
- **HTMX:** Delete/toggle inline, form submit PRG, lazy-load partials

---

## Faz 1 — Temel Eksikler (Mevcut Servislerin MVC'ye Expose Edilmesi)

Business layer zaten var, sadece MVC controller + view + test gerekiyor.

### 1.1 Aktivite / Audit Log

**Feature Folder:** `Features/Logs/`
**Controller:** Yeni — `LogController`
**Servis:** `IApplicationLogManager.GetPaginatedLogs()` (mevcut)
**Nav:** Ayarlar → "Aktivite Logu"

| Sayfa | Route | Açıklama |
|---|---|---|
| Log Listesi | `GET /logs` | LogType + LogAction + tarih filtreli, sayfalı tablo |
| Log Detay | `GET /logs/{id}` | Tek log kaydının detayı (HTMX panel) |

### 1.2 Depo CRUD Tamamlama

**Feature Folder:** `Features/BranchOffices/` (mevcut genişletme)
**Controller:** Mevcut — `BranchOfficeController`
**Servis:** `IBranchOfficeService` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Depo Oluştur | `GET/POST /branch-offices/create` | Form + PRG |
| Depo Düzenle | `GET/POST /branch-offices/{id}/edit` | Form + PRG |

### 1.3 Stok Hareketleri

**Feature Folder:** `Features/StockMovements/`
**Controller:** Yeni — `StockMovementController`
**Servis:** `IOfficeStockManager` (mevcut — Transfer, Increase, Decrease)
**Nav:** Yönetim → "Stok Hareketleri"

| Sayfa | Route | Açıklama |
|---|---|---|
| Hareket Listesi | `GET /stock/movements` | Depo + ürün + tarih filtreli, giriş/çıkış/transfer ikonları |
| Manuel Stok Girişi | `POST /stock/movements/adjust` | HTMX dialog — ürün seç, miktar, neden |

### 1.4 Kupon / İndirim Çeki Yönetimi

**Feature Folder:** `Features/Discounts/`
**Controller:** Yeni — `DiscountController`
**Servis:** `IDiscountVoucherManager` (mevcut — Create, Check, Redeem)
**Nav:** Yönetim → "Kuponlar"

| Sayfa | Route | Açıklama |
|---|---|---|
| Kupon Listesi | `GET /discounts` | Aktif/pasif/kullanılmış filtre, kod + tutar + tarih |
| Kupon Oluştur | `GET/POST /discounts/create` | Form — kod, indirim tipi (%, TL), geçerlilik tarihi |
| Kupon Detay | `GET /discounts/{id}` | Kullanım geçmişi, durum |

### 1.5 Hediye Kartı Yönetimi

**Feature Folder:** `Features/GiftCards/`
**Controller:** Yeni — `GiftCardController`
**Servis:** `IStorefrontGiftCardManager` (mevcut)
**Nav:** Mağaza → "Hediye Kartları"

| Sayfa | Route | Açıklama |
|---|---|---|
| Hediye Kartı Listesi | `GET /gift-cards` | Bakiye, durum, son kullanma |
| Hediye Kartı Oluştur | `GET/POST /gift-cards/create` | Form — tutar, alıcı, mesaj |
| Hediye Kartı Detay | `GET /gift-cards/{id}` | İşlem geçmişi (transactions) |

### 1.6 Kargo Firması Ayarları

**Feature Folder:** `Features/Shipping/` (mevcut genişletme)
**Controller:** Mevcut — `ShippingController`
**Servis:** `ICargoCompaniesManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Kargo Firmaları | `GET /shipping/companies` | Firma listesi + aktif/pasif toggle |
| Firma Düzenle | `POST /shipping/companies/{id}/update` | HTMX inline edit |

### 1.7 Satıcı Komisyon Yönetimi

**Feature Folder:** `Features/Storefront/` (mevcut genişletme)
**Controller:** Mevcut — `StorefrontController`
**Servis:** `ISellerCommissionManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Komisyon Listesi | `GET /storefront/commissions` | Satıcı bazlı komisyon oranları |
| Komisyon Düzenle | `POST /storefront/commissions/{id}/update` | HTMX inline edit |

### 1.8 Terk Edilmiş Sepet Yönetimi

**Feature Folder:** `Features/Storefront/` (mevcut genişletme)
**Controller:** Mevcut — `StorefrontController`
**Servis:** `IStorefrontAbandonedCartManager` (mevcut)
**Nav:** Mağaza → "Terk Edilen Sepetler"

| Sayfa | Route | Açıklama |
|---|---|---|
| Terk Edilen Sepetler | `GET /storefront/abandoned-carts` | Müşteri, ürünler, tutar, süre |
| Sepet Detay | `GET /storefront/abandoned-carts/{id}` | HTMX panel — ürün detayları |

### Faz 1 Özet

- **4 yeni controller**, **4 mevcut genişletme**
- **~16 sayfa**
- **0 yeni business servis** — tümü mevcut
- **Yeni business logic yok** — sadece MVC layer

---

## Faz 2 — Operasyonel Sayfalar (Sipariş + Kargo + İade Derinliği)

Mevcut sayfaların eksik alt-sayfaları ve operasyonel akışların tamamlanması.

### 2.1 Yerel Sipariş Detay + Manuel Sipariş

**Feature Folder:** `Features/Orders/` (mevcut genişletme)
**Controller:** Mevcut — `OrderController`
**Servis:** `IOrderManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Sipariş Detay | `GET /orders/{id}` | Ürünler, müşteri, durum, ödeme özeti |
| Sipariş Timeline | partial | Durum geçmişi timeline'ı |
| Sipariş Yazdırma | `GET /orders/{id}/print` | İrsaliye / paket fişi — yazdırılabilir layout |
| Manuel Sipariş | `GET/POST /orders/create` | Telefon/e-posta sipariş oluşturma formu |

### 2.2 Marketplace İade Yönetimi

**Feature Folder:** `Features/Returns/`
**Controller:** Yeni — `ReturnController`
**Servis:** Marketplace iade servisleri (ICiceksepetiReturnService, IN11ClaimService, IHepsiburadaClaimService, Trendyol endpoints)
**Nav:** Yönetim → "İade Yönetimi"

| Sayfa | Route | Açıklama |
|---|---|---|
| İade Listesi | `GET /returns` | Tüm marketplace iadeleri birleşik — marketplace + durum filtresi |
| İade Detay | `GET /returns/{id}` | HTMX panel — ürün, müşteri, neden, fotoğraflar |
| İade Onay/Red | `POST /returns/{id}/approve`, `POST /returns/{id}/reject` | HTMX action butonları |

### 2.3 Kargo Derinleştirme

**Feature Folder:** `Features/Shipping/` (mevcut genişletme)
**Controller:** Mevcut — `ShippingController`
**Servis:** `IShipmentTrackingManager`, `ICargoTrackingAdapter` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Kargo Detay | `GET /shipping/{id}` | Tracking timeline, kargo firması bilgisi |
| Toplu Kargo | `GET/POST /shipping/bulk-create` | Seçili siparişlere toplu kargo kodu atama |
| Kargo Etiketi | `GET /shipping/{id}/label` | PDF kargo etiketi |

### 2.4 Müşteri Detay Dashboard

**Feature Folder:** `Features/Customers/` (mevcut genişletme)
**Controller:** Mevcut — `CustomerController`
**Servis:** `ICustomerManager` + `IOrderManager` (cross-query)

| Sayfa | Route | Açıklama |
|---|---|---|
| Müşteri Dashboard | `GET /customers/{id}/dashboard` | Toplam harcama, sipariş sayısı, son siparişler |
| Sipariş Geçmişi | partial | HTMX lazy-load — müşterinin tüm siparişleri |

### 2.5 Sipariş Hazırlama Ekranı

**Feature Folder:** `Features/Picking/`
**Controller:** Yeni — `PickingController`
**Servis:** Yeni — `IPickingService` (IOrderManager üzerine ince wrapper)
**Nav:** Yönetim → "Sipariş Hazırlama"

| Sayfa | Route | Açıklama |
|---|---|---|
| Toplama Listesi | `GET /picking` | Hazırlanacak siparişler — ürün bazlı gruplu liste |
| Barkod Okutma | `POST /picking/scan` | HTMX — barkod okut, "toplandı" işaretle |
| Paketleme | `POST /picking/{orderId}/pack` | Sipariş paketlendi olarak işaretle |

### 2.6 Entegrasyon Sağlık Monitörü

**Feature Folder:** `Features/IntegrationHealth/`
**Controller:** Yeni — `IntegrationHealthController`
**Servis:** Marketplace API client health check'leri (mevcut client'lardan son sync/hata bilgisi)
**Nav:** Pazaryeri → "Entegrasyon Durumu"

| Sayfa | Route | Açıklama |
|---|---|---|
| Sağlık Dashboard | `GET /integrations/health` | Marketplace bazlı bağlantı durumu, son sync, hata oranı |
| Detay | `GET /integrations/health/{marketplace}` | HTMX panel — son hatalar, API response time |

### Faz 2 Özet

- **3 yeni controller**, **3 mevcut genişletme**
- **~17 sayfa**
- **1 yeni business servis:** `IPickingService`

---

## Faz 3 — Raporlar & Analitik

Tüm yeni raporlar mevcut `ReportController`'a eklenir. Yeni controller yok.

### 3.1 Müşteri Raporu

| Sayfa | Route | Açıklama |
|---|---|---|
| Müşteri Raporu | `GET /reports/customers` | En çok alan müşteriler, yeni müşteri trendi, segment tablosu |

### 3.2 İade Raporu

| Sayfa | Route | Açıklama |
|---|---|---|
| İade Raporu | `GET /reports/returns` | İade oranı, neden dağılımı, marketplace karşılaştırma |

### 3.3 Vergi Raporu

| Sayfa | Route | Açıklama |
|---|---|---|
| Vergi Raporu | `GET /reports/tax` | KDV özeti, aylık/yıllık tablo, fatura bazlı detay |

### 3.4 Kategori Bazlı Satış Raporu

| Sayfa | Route | Açıklama |
|---|---|---|
| Kategori Satış | `GET /reports/category-sales` | Kategori ağacında satış dağılımı, trend |

### 3.5 Kargo Raporu

| Sayfa | Route | Açıklama |
|---|---|---|
| Kargo Raporu | `GET /reports/shipping` | Ort. teslimat süresi, firma karşılaştırma, maliyet analizi |

### Faz 3 Özet

- **0 yeni controller**, **1 mevcut genişletme** (ReportController)
- **~5 sayfa**
- **0 yeni business servis**

---

## Faz 4 — Fiyat & Promosyon Yönetimi

### 4.1 Fiyat Yönetimi

**Feature Folder:** `Features/Pricing/`
**Controller:** Yeni — `PricingController`
**Servis:** Yeni — `IPricingRuleManager` (entity + EF config + migration gerekli)
**Nav:** Yönetim → "Fiyat Yönetimi"

| Sayfa | Route | Açıklama |
|---|---|---|
| Fiyat Listesi | `GET /pricing` | Ürün bazlı fiyat tablosu — maliyet, satış, marketplace fiyatları |
| Toplu Güncelleme | `POST /pricing/bulk-update` | Seçili ürünlerde % veya TL artış/azalış |

### 4.2 Otomatik Fiyatlama Kuralları

**Servis:** `IPricingRuleManager` (aynı servis)

| Sayfa | Route | Açıklama |
|---|---|---|
| Kural Listesi | `GET /pricing/rules` | Aktif kurallar — "maliyet + %30", "Trendyol'a +%5" |
| Kural Oluştur | `GET/POST /pricing/rules/create` | Kaynak fiyat, hedef marketplace, marj tipi, yuvarlama |

### 4.3 Loyalty Program Yönetimi

**Feature Folder:** `Features/Loyalty/`
**Controller:** Yeni — `LoyaltyController`
**Servis:** `IStorefrontLoyaltyManager` (mevcut)
**Nav:** Mağaza → "Sadakat Programı"

| Sayfa | Route | Açıklama |
|---|---|---|
| Loyalty Dashboard | `GET /loyalty` | Üye sayısı, puan dağılımı, aktif kampanyalar |
| Puan Kuralları | `GET/POST /loyalty/rules` | Kazanım/harcama kuralları |

### 4.4 Referral Program

**Feature Folder:** `Features/Storefront/` (mevcut genişletme)
**Controller:** Mevcut — `StorefrontController`
**Servis:** `IStorefrontReferralManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Referral Dashboard | `GET /storefront/referrals` | Davet edilen/dönüşen, komisyon özeti |

### Faz 4 Özet

- **2 yeni controller**, **1 mevcut genişletme**
- **~7 sayfa**
- **1 yeni business servis:** `IPricingRuleManager` (entity + migration dahil)

---

## Faz 5 — Ayarlar & Profil Tamamlama

Tümü mevcut controller genişletmeleri.

### 5.1 Şifre Değiştirme

**Feature Folder:** `Features/Profile/` (genişletme)

| Sayfa | Route | Açıklama |
|---|---|---|
| Şifre Değiştir | `GET/POST /profile/change-password` | Mevcut şifre + yeni şifre formu |

### 5.2 Kullanıcı Aktivite Logu

**Feature Folder:** `Features/Profile/` (genişletme)

| Sayfa | Route | Açıklama |
|---|---|---|
| Aktivitem | `GET /profile/activity` | Kullanıcının kendi işlem geçmişi (IApplicationLogManager filtreli) |

### 5.3 Vergi Ayarları

**Feature Folder:** `Features/Settings/` (genişletme)

| Sayfa | Route | Açıklama |
|---|---|---|
| Vergi Ayarları | `GET/POST /settings/tax` | KDV oranları, muafiyet tanımları |

### 5.4 Kargo Ayarları

**Feature Folder:** `Features/Settings/` (genişletme)

| Sayfa | Route | Açıklama |
|---|---|---|
| Kargo Ayarları | `GET/POST /settings/shipping` | Varsayılan firma, credential'lar, desi/kg kuralları |

### 5.5 Webhook Yönetimi

**Feature Folder:** `Features/Settings/` (genişletme)
**Servis:** Yeni — `IWebhookManager`

| Sayfa | Route | Açıklama |
|---|---|---|
| Webhook Listesi | `GET /settings/webhooks` | Kayıtlı webhook'lar, son tetiklenme, durum |
| Webhook Oluştur | `POST /settings/webhooks/create` | HTMX dialog — URL, event tipi, secret |

### 5.6 API Key Yönetimi

**Feature Folder:** `Features/Settings/` (genişletme)
**Servis:** Yeni — `IApiKeyManager`

| Sayfa | Route | Açıklama |
|---|---|---|
| API Keys | `GET /settings/api-keys` | Mevcut key'ler, oluşturma tarihi, son kullanım |
| Key Oluştur | `POST /settings/api-keys/create` | HTMX — isim, permission scope, expiry |

### Faz 5 Özet

- **0 yeni controller**, **3 mevcut genişletme** (Profile, Settings)
- **~8 sayfa**
- **2 yeni business servis:** `IWebhookManager`, `IApiKeyManager` (entity + migration dahil)

---

## Faz 6 — Storefront Admin Derinliği

Tümü mevcut `StorefrontController` genişletmesi.

### 6.1 Cüzdan Yönetimi

**Servis:** `IStorefrontWalletManager` (mevcut)
**Nav:** Mağaza → "Müşteri Cüzdanları"

| Sayfa | Route | Açıklama |
|---|---|---|
| Cüzdan Listesi | `GET /storefront/wallets` | Müşteri cüzdanları, bakiyeler |
| Cüzdan Detay | `GET /storefront/wallets/{id}` | İşlem geçmişi, yükleme/harcama |
| Manuel Yükleme | `POST /storefront/wallets/{id}/credit` | HTMX — tutar, açıklama |

### 6.2 İstek Listesi Analitik

**Servis:** `IStorefrontWishlistManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Wishlist Analitik | `GET /storefront/wishlists` | En çok istenen ürünler, dönüşüm oranı |

### 6.3 Stok Bildirim Yönetimi

**Servis:** `IStorefrontStockNotificationManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Stok Bildirimleri | `GET /storefront/stock-notifications` | Bekleyen bildirimler, gönderilen, ürün bazlı |

### 6.4 Arama Analitik

**Servis:** `IStorefrontSearchHistoryManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Arama Analitik | `GET /storefront/search-analytics` | En çok aranan terimler, sonuçsuz aramalar |

### 6.5 Push Bildirim Yönetimi

**Servis:** `IStorefrontPushManager` (mevcut)

| Sayfa | Route | Açıklama |
|---|---|---|
| Push Dashboard | `GET /storefront/push-notifications` | Gönderilen bildirimler, açılma oranı |
| Push Gönder | `POST /storefront/push-notifications/send` | HTMX — hedef segment, başlık, mesaj |

### Faz 6 Özet

- **0 yeni controller**, **StorefrontController genişletmesi**
- **~8 sayfa**
- **0 yeni business servis**

---

## Toplam Özet

| Faz | Açıklama | Sayfa | Yeni Controller | Yeni Servis |
|---|---|---|---|---|
| **Faz 1** | Temel Eksikler | ~16 | 4 | 0 |
| **Faz 2** | Operasyonel Sayfalar | ~17 | 3 | 1 |
| **Faz 3** | Raporlar & Analitik | ~5 | 0 | 0 |
| **Faz 4** | Fiyat & Promosyon | ~7 | 2 | 1 |
| **Faz 5** | Ayarlar & Profil | ~8 | 0 | 2 |
| **Faz 6** | Storefront Admin | ~8 | 0 | 0 |
| **TOPLAM** | | **~61** | **9** | **4** |

### Yeni Business Servisleri (Entity + Migration Gerekli)

1. `IPickingService` (Faz 2) — sipariş toplama/paketleme durumu
2. `IPricingRuleManager` (Faz 4) — fiyat kuralları entity + motor
3. `IWebhookManager` (Faz 5) — webhook kayıt + tetikleme + loglama
4. `IApiKeyManager` (Faz 5) — API key CRUD + scope + hashing

### Bağımlılık Sırası

```
Faz 1 (bağımsız — mevcut servisler)
  └→ Faz 2 (Faz 1'deki stok/kargo altyapısına bağlı)
       └→ Faz 3 (Faz 2'deki iade/kargo verisine bağlı raporlar)
Faz 4 (bağımsız — yeni entity'ler)
Faz 5 (bağımsız — ayarlar)
Faz 6 (bağımsız — storefront genişletme)
```

Faz 1→2→3 sıralı, Faz 4/5/6 birbirine paralel çalışabilir.
