# E-Ticaret Platformu (Storefront as a Service) — Tam Spec

## Context

Mevcut Entegrasyon sistemi müşterilere pazaryeri entegrasyonu sağlıyor. Şimdi aynı müşterilere **kendi alan adlarında tam teşekküllü e-ticaret sitesi** hizmeti de verilecek. Modern e-ticaret sitelerinden hiçbir farkı olmayacak. SEO, GEO ve AEO'ya tam optimize. Müşteri sadece alan adını sağlar, kurulum/bakım/güncelleme tamamen bizde.

**Teknoloji:** ASP.NET Core MVC + Razor Pages + Tailwind CSS
**Neden MVC:** Hafif, hızlı, SEO-doğal HTML output. Component lifecycle overhead yok. E-ticaret için kanıtlanmış pattern.

**Kritik Kurallar:**
- `{slug}.entegrasyon.com` = müşterinin **yönetim paneli** (dashboard). E-ticaret sitesi DEĞİL.
- Her müşterinin **kendi alan adı zorunlu** (magaza-ali.com). Subdomain ile mağaza AÇILMAZ.
- **Üyelik sistemi zorunlu** — son kullanıcılar kayıt/giriş yapar, geçmiş siparişlerini görür.
- **Storefront üyeleri = Entegrasyon Customer entity** — dashboard'dan da görünür/yönetilir.
- **SEO hedefi: Trendyol ve Hepsiburada'dan daha iyi** — teknik SEO, structured data, Core Web Vitals, content SEO hepsinde üstün olmalı.

---

## 1. Üst Düzey Mimari

```
                     ┌─────────────────────────────┐
                     │      Nginx Reverse Proxy     │
                     │   SSL Termination + Routing  │
                     └──────┬──────────┬────────────┘
                            │          │
              ┌─────────────┘          └──────────────┐
              ▼                                        ▼
┌──────────────────────┐                ┌──────────────────────┐
│  Storefront App      │                │  Dashboard App       │
│  (ASP.NET MVC)       │                │  (mevcut Blazor)     │
│  magaza-x.com        │                │  + Sanal POS Ayar    │
│  magaza-y.com        │                │  + Mağaza Kurulum    │
└──────────┬───────────┘                └──────────┬───────────┘
           │ Direct DI                              │ Direct DI
           ▼                                        ▼
┌──────────────────────────────────────────────────────────────┐
│                  Business Layer (Shared)                      │
│  + StorefrontManager, CartManager, CheckoutManager           │
│  + PaymentGatewayService, WishlistManager                    │
└──────────────────────────┬───────────────────────────────────┘
                           │
              ┌────────────▼────────────┐
              │  IntegrationDbContext    │
              │  (Tenant-aware, PG)     │
              └─────────────────────────┘
```

### Deployment: Shared Instance + Nginx Routing

```
Nginx
├── magaza-ali.com             → X-Tenant-Id: 1  → Storefront App (e-ticaret sitesi)
├── www.veli-giyim.com.tr      → X-Tenant-Id: 2  → Storefront App (e-ticaret sitesi)
├── ahmet-tekstil.entegrasyon.com → (Dashboard)   → Dashboard App (yönetim paneli)
├── veli-giyim.entegrasyon.com    → (Dashboard)   → Dashboard App (yönetim paneli)
├── panel.entegrasyon.com         → (Admin)       → Admin Panel (bizim yönetim)
└── api.entegrasyon.com           →               → Public API (ileride)
```

**Önemli ayrım:**
- `magaza-ali.com` → **E-ticaret sitesi** (son kullanıcılar görür, alışveriş yapar)
- `ahmet.entegrasyon.com` → **Dashboard** (müşterimiz ürün/sipariş/stok yönetir)
- Her müşterinin **kendi domain'i zorunlu** — subdomain ile mağaza açılmaz

---

## 2. Müşteri Kurulum Portalı (Dashboard İçinde)

Dashboard'a giriş yapan müşteri "Mağazanı Kur" butonuyla wizard'ı başlatır.

### Adım 1: Tema Seçimi
Görsel önizlemeli tema kartları. Tıkla → seç.

| Tema | Hedef | Özellik |
|------|-------|---------|
| **Varsayılan** | Genel | Temiz grid, responsive, Tailwind |
| **Modern** | Moda/giyim | Büyük görseller, parallax |
| **Klasik** | Elektronik | Sidebar filtreler, spec tabloları |
| **Minimal** | Butik | Beyaz alan, tipografi odaklı |

### Adım 2: Marka Kimliği
| Alan | Zorunlu | Açıklama |
|------|---------|----------|
| Logo | ✅ | Header'da görünür (300x100px, PNG/SVG) |
| Favicon | ❌ | Tarayıcı sekme ikonu (32x32) |
| Mağaza Adı | ✅ | "Ahmet Tekstil" |
| Slogan | ❌ | Logo altı / header |
| Ana Renk | ✅ | Butonlar, linkler (color picker) |
| İkinci Renk | ❌ | Header/footer arka plan |
| Vurgu Rengi | ❌ | İndirim badge, kampanya |

### Adım 3: Firma & İletişim
| Alan | Zorunlu | Görünürlük |
|------|---------|------------|
| Firma Unvanı | ✅ | Footer, fatura, yasal sayfalar |
| Vergi Dairesi | ✅ | Footer, fatura |
| Vergi No | ✅ | Footer, fatura |
| MERSİS No | ❌ | Footer (yasal) |
| KEP Adresi | ❌ | İletişim sayfası |
| Telefon | ✅ | Header, footer, structured data |
| WhatsApp | ❌ | Floating buton (sağ alt) |
| E-posta | ✅ | İletişim, footer |
| Adres + İl/İlçe | ✅ | İletişim, LocalBusiness schema |
| Instagram | ❌ | Footer sosyal ikonlar |
| Facebook | ❌ | Footer + OG meta |
| Twitter/X | ❌ | Footer |
| YouTube | ❌ | Footer |
| TikTok | ❌ | Footer |

### Adım 4: Yasal Metinler & Politikalar
**Varsayılan şablonlar** firma bilgileriyle otomatik doldurulur. Müşteri WYSIWYG editörle düzenler.

| Metin | Zorunlu | Yasal Dayanak |
|-------|---------|---------------|
| Kullanıcı Sözleşmesi | ✅ | 6502 Tüketici Kanunu |
| Gizlilik Politikası | ✅ | KVKK Md.10 |
| KVKK Aydınlatma Metni | ✅ | 6698 KVKK |
| Çerez Politikası | ✅ | KVKK + ePrivacy |
| Mesafeli Satış Sözleşmesi | ✅ | 6502/Md.48 |
| Ön Bilgilendirme Formu | ✅ | Mesafeli Sözleşmeler Yönetmeliği |
| İade & Değişim Politikası | ✅ | 14 gün cayma hakkı |
| Teslimat Koşulları | ✅ | Mesafeli Sözleşmeler Yönetmeliği |
| Hakkımızda | ❌ | Güven + SEO |

### Adım 5: Kargo & Teslimat
- Varsayılan kargo firması (dropdown: Yurtiçi, Sürat, Aras)
- Kargo ücreti (₺)
- Ücretsiz kargo limiti (₺, 0=kapalı)
- Tahmini teslimat süresi (gün)
- Kapıda ödeme aktif/pasif + ek ücret

### Adım 6: Sanal POS / Ödeme Ayarları
- Ödeme sağlayıcı seçimi (iyzico / PayTR)
- API Key + Secret Key girişi
- Test / Canlı mod seçimi
- Taksit aktif/pasif + max taksit sayısı
- Minimum sipariş tutarı

### Adım 7: Alan Adı (Zorunlu)
- Müşterinin **kendi alan adı zorunlu** (ör: magaza-ali.com)
- Dashboard erişimi: `{slug}.entegrasyon.com` (otomatik oluşturulur)
- DNS talimatları müşteriye verilir (A record + CNAME)
- SSL otomatik (Let's Encrypt + Certbot)
- Domain doğrulama: DNS propagation kontrolü

### Adım 8: Ekstralar
- Google Analytics ID, GTM ID, Facebook Pixel ID
- Duyuru çubuğu (metin + renk)
- WhatsApp butonu aktif/pasif
- Çerez onayı aktif/pasif

### "Mağazamı Oluştur" → Otomatik Kurulum
1. `CREATE DATABASE tenant_{id}` (PostgreSQL)
2. EF Core migration uygula
3. Seed data (kategoriler, ayarlar, yasal şablonlar)
4. StorefrontSettings + PaymentConfig + DomainMapping kayıtları
5. Nginx config güncelle + SSL oluştur + reload
6. Health check → Dashboard'a + email bildirim

---

## 3. Dashboard: Sanal POS Yönetim Sayfası

Dashboard'da `/settings/payment` (veya `/settings/storefront/payment`) adresi.

### Sayfa İçeriği

```
┌─────────────────────────────────────────────────────────────┐
│  Sanal POS & Ödeme Ayarları                      [Kaydet]   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ── Aktif Ödeme Yöntemi ──                                  │
│  ○ iyzico        ● PayTR        ○ Param (yakında)           │
│                                                              │
│  ── PayTR Bilgileri ──                                       │
│  Mağaza No (Merchant ID): [________________]                 │
│  Mağaza Anahtarı (Key):   [________________] 👁              │
│  Mağaza Şifresi (Secret): [________________] 👁              │
│  Mod:  ○ Test (Sandbox)   ● Canlı (Production)              │
│                                                              │
│  ☑ Test bağlantısı yap   [🔌 Bağlantıyı Test Et]           │
│    ✅ Bağlantı başarılı! (Son test: 24.03.2026 14:32)       │
│                                                              │
│  ── Taksit Ayarları ──                                       │
│  ☑ Taksit aktif                                              │
│  Maksimum taksit sayısı: [▼ 12]                              │
│  Minimum taksitli tutar:  [₺ 100,00]                         │
│                                                              │
│  ── Banka Bazlı Taksit Oranları ──  (opsiyonel)             │
│  ┌──────────────┬──────┬──────┬──────┬──────┬──────┐        │
│  │ Banka        │ 2 Tk │ 3 Tk │ 6 Tk │ 9 Tk │12 Tk│        │
│  ├──────────────┼──────┼──────┼──────┼──────┼──────┤        │
│  │ Ziraat       │ 1.5% │ 2.0% │ 4.5% │ 7.0% │10.0%│        │
│  │ Garanti      │ 1.8% │ 2.5% │ 5.0% │ 7.5% │11.0%│        │
│  │ İş Bankası   │ 1.5% │ 2.0% │ 4.5% │ 7.0% │10.5%│        │
│  │ ...          │      │      │      │      │      │        │
│  └──────────────┴──────┴──────┴──────┴──────┴──────┘        │
│  ℹ️ Bu oranlar ödeme sayfasında müşteriye gösterilir         │
│                                                              │
│  ── Ek Ödeme Yöntemleri ──                                   │
│  ☑ Kapıda Nakit Ödeme        Ek ücret: [₺ 9,90]             │
│  ☑ Kapıda Kredi Kartı        Ek ücret: [₺ 4,90]             │
│  ☐ Havale/EFT                                                │
│    Banka: [________________]                                  │
│    IBAN:  [TR__________________________]                      │
│    Hesap Sahibi: [________________]                           │
│                                                              │
│  ── Genel ──                                                 │
│  Minimum sipariş tutarı: [₺ 50,00] (0 = limit yok)          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Teknik Detaylar
- API key/secret şifreli saklanır (AES-256 encryption at rest)
- "Bağlantıyı Test Et" butonu: sağlayıcının test endpoint'ine 1₺'lik ödeme başlatıp iptal eder
- Banka bazlı taksit oranları: iyzico/PayTR API'den otomatik çekilir veya manuel girilebilir
- Değişiklikler anında aktif — mağaza restart gerektirmez (DB'den okunur)

---

## 4. Üyelik Sistemi (Storefront Müşterileri)

### 4.0.1 Temel İlke

Storefront'taki son kullanıcılar (alışveriş yapan müşteriler) = Entegrasyon sistemindeki **Customer** entity'si. Bu sayede:
- Dashboard'dan müşteriler görünür/aranabilir/yönetilebilir
- Müşteri siparişleri, iade talepleri, değerlendirmeleri dashboard'da takip edilir
- Mevcut `RetailCustomer` / `CorporateCustomer` kalıtım yapısı kullanılır
- Müşteri segmentasyonu, CRM, kampanya hedefleme yapılabilir

### 4.0.2 Kayıt & Giriş

**Kayıt formu:**
- Ad, soyad, email (unique per tenant), telefon, şifre
- KVKK onayı checkbox (zorunlu)
- Kampanya email izni checkbox (opsiyonel)
- Kayıt sonrası: email doğrulama linki gönderilir
- Kayıt → `RetailCustomer` oluşturulur + `StorefrontCustomerAuth` kaydı

**Giriş:**
- Email + şifre
- "Beni hatırla" checkbox (persistent cookie)
- "Şifremi unuttum" → email ile sıfırlama token'ı

**Şifre sıfırlama:**
- Email gir → 1 saatlik token → yeni şifre belirle

### 4.0.3 Yeni Entity: StorefrontCustomerAuth

Mevcut `Customer` entity'sine şifre/auth bilgisi eklenmez (mevcut yapıyı bozmaz). Ayrı bir auth tablosu:

```
Entity/Storefront/StorefrontCustomerAuth.cs : BaseEntity
├── TenantId (int)
├── CustomerId (int, FK → Customer)
├── Email (string, unique per tenant — login identifier)
├── PasswordHash (string)
├── PasswordSalt (string)
├── EmailConfirmed (bool)
├── EmailConfirmationToken (string?)
├── PasswordResetToken (string?)
├── PasswordResetTokenExpiresAt (DateTimeOffset?)
├── LastLoginAt (DateTimeOffset?)
├── LoginFailedCount (int — brute force koruması)
├── LockedUntil (DateTimeOffset? — 5 başarısız deneme sonrası)
├── MarketingConsent (bool — kampanya email izni)
├── MarketingConsentDate (DateTimeOffset?)
└── KvkkConsentDate (DateTimeOffset — KVKK onay tarihi, yasal zorunluluk)
```

### 4.0.4 Müşteri Hesap Paneli (`/hesabim`)

| Sayfa | İçerik |
|-------|--------|
| **Hesap Özeti** | Hoşgeldin mesajı, son siparişler, favori sayısı, adres sayısı |
| **Siparişlerim** | Sipariş listesi: tarih, tutar, durum (badge), detay linki |
| **Sipariş Detay** | Ürünler, fiyatlar, adres, ödeme yöntemi, kargo takip no + takip linki, fatura PDF indir |
| **İade Talebi** | Sipariş seç → ürün seç → sebep (dropdown) → açıklama → fotoğraf yükle → gönder |
| **Adreslerim** | Adres CRUD: ad (ev/iş/diğer), il/ilçe/mahalle/sokak/posta kodu, varsayılan seç |
| **Favorilerim** | Beğenilen ürünler grid, sepete ekle, tükenmiş uyarısı |
| **Değerlendirmelerim** | Yazdığım yorumlar listesi, onay durumu |
| **Profilim** | Ad, soyad, email, telefon düzenleme |
| **Şifre Değiştir** | Mevcut şifre + yeni şifre |
| **Bildirim Tercihleri** | ☑ Sipariş güncellemeleri ☑ Kampanyalar ☑ Stok bildirimleri |
| **Hesabımı Sil** | KVKK gereği — veri silme talebi (soft delete + anonymize) |

### 4.0.5 Dashboard'da Müşteri Görünümü

Dashboard'da mevcut **Müşteriler** sayfası storefront üyelerini de gösterir:
- Müşteri listesinde "Kaynak" sütunu: POS / Trendyol / Hepsiburada / **Storefront**
- Müşteri detayında: sipariş geçmişi, harcama toplamı, son giriş tarihi, favori ürünler
- Filtre: kaynak bazlı (sadece storefront müşterileri)
- Müşteriye özel indirim kuponu oluşturma (mevcut DiscountVoucher)

---

## 4.1 SEO Üstünlük Stratejisi (Trendyol/Hepsiburada'dan İyi)

### Neden Üstün Olabiliriz?

Trendyol/Hepsiburada **marketplace** — milyonlarca ürün, binlerce satıcı, genel amaçlı. Bizim mağazalar **niş ve odaklı** — tek satıcı, belirli kategori, derin içerik. Google niş siteleri marketplace'lere tercih eder çünkü:

1. **Topic Authority**: Tek konuya odaklı site > genel marketplace
2. **Content Depth**: Ürün başına daha zengin içerik (uzun açıklama, SSS, blog)
3. **Page Speed**: Daha az JavaScript, daha hafif sayfa > marketplace'lerin ağır sayfaları
4. **User Experience**: Daha az reklam, daha temiz UX > marketplace karmaşası

### Teknik SEO Üstünlükleri

| Kriter | Trendyol/Hepsiburada | Bizim Mağazalar |
|--------|---------------------|-----------------|
| **Core Web Vitals** | LCP: 3-5s (ağır JS) | LCP: < 1.5s (saf HTML, MVC) |
| **JavaScript** | 2-5MB JS bundle | < 100KB (minimal JS) |
| **First Contentful Paint** | 2-3s | < 0.8s |
| **Cumulative Layout Shift** | 0.15-0.3 (reklam kaymaları) | < 0.05 (sabit layout) |
| **Mobile Usability** | İyi ama ağır | Mükemmel (Tailwind mobile-first) |
| **Structured Data** | Temel Product schema | Zengin: Product + Review + FAQ + Breadcrumb + Store + Offer + AggregateRating |
| **URL Yapısı** | `/p-urun-kodu-fiyat` (çirkin) | `/urun/siyah-basic-tisort` (temiz, anlamlı) |
| **Canonical** | Bazen sorunlu (duplicate) | Her zaman doğru |
| **Internal Linking** | Zayıf (marketplace yapısı) | Güçlü (ilgili ürünler, breadcrumb, kategori) |

### İçerik SEO Üstünlükleri

**Her ürün sayfasında otomatik üretilecek SEO zenginlikleri:**

1. **Dinamik SEO Title**: `{ÜrünAdı} - {Marka} | {FiyatBilgisi} | {MağazaAdı}`
   - Trendyol: "Siyah Tişört - Trendyol" (generic)
   - Biz: "Siyah Basic Tişört - XYZ Marka | ₺299,90 | Ücretsiz Kargo | Ahmet Tekstil"

2. **Rich Description**: Ürün açıklaması + özellikler + kargo bilgisi + iade politikası hepsi meta description'da

3. **FAQ Section (Her Ürün Sayfasında)**:
   - "Bu ürünün kargo süresi ne kadar?" → "{EstimatedDeliveryDays} iş günü içinde kapınızda"
   - "İade edebilir miyim?" → "14 gün içinde ücretsiz iade"
   - "Taksitli alabilir miyim?" → "12 taksit imkanı"
   - "Bedeni nasıl seçmeliyim?" → Beden tablosu
   - Bu SSS'ler FAQPage schema ile işaretlenir → Google/AI arama sonuçlarında doğrudan görünür

4. **Breadcrumb Derinliği**: Ana Sayfa > Giyim > Erkek Giyim > Tişört > Siyah Basic Tişört
   - Trendyol: genellikle 2 seviye
   - Biz: 4-5 seviye (daha zengin navigasyon sinyali)

5. **İlgili Ürün Linkleri**: Her ürün sayfasında 8-12 ilgili ürün → güçlü internal linking

### AEO Üstünlükleri (AI Arama)

**ChatGPT, Perplexity, Google AI Overview gibi AI arama motorları için:**

1. **Speakable Schema**: Ürün adı + fiyat + stok durumu sesli asistanlara uygun
2. **FAQ Schema**: Her ürün sayfasında 5-8 SSS → AI doğrudan yanıt üretir
3. **Review Schema**: Gerçek müşteri yorumları → AI güven sinyali olarak kullanır
4. **Detaylı Offer Schema**: Fiyat, stok, kargo süresi, iade politikası → AI karşılaştırma yapabilir
5. **HowTo Schema**: Ürün kullanım/bakım talimatları (opsiyonel ama güçlü)

### GEO SEO Üstünlükleri

1. **LocalBusiness Schema**: Fiziksel adres, telefon, çalışma saatleri → yerel arama
2. **Google Business Profile bağlantısı**: Haritada görünürlük
3. **İl bazlı teslimat sayfaları**: `/kargo/istanbul`, `/kargo/ankara` → yerel arama trafiği
4. **Bölgesel meta**: "İstanbul'a aynı gün kargo" → yerel intent yakalama

### SEO İzleme & Raporlama (Dashboard'da)

```
Dashboard → Raporlar → SEO Performansı
├── Lighthouse Skorları (otomatik haftalık tarama)
│   ├── Performance: 95+
│   ├── SEO: 100
│   ├── Accessibility: 95+
│   └── Best Practices: 95+
├── Google Search Console Verileri (API entegrasyonu — gelecek faz)
│   ├── Impressions, clicks, CTR, average position
│   ├── Top sayfalar, top queries
│   └── Index coverage (kaç sayfa indexlendi)
├── Core Web Vitals
│   ├── LCP, FID, CLS gerçek kullanıcı verileri
│   └── Kırmızı/sarı/yeşil gösterge
└── Structured Data Durumu
    ├── Kaç ürün Product schema'ya sahip
    ├── Kaç sayfada FAQ schema var
    └── Hata varsa uyarı
```

---

## 5. Storefront: Tam Sayfa ve Özellik Listesi

### 4.1 Ana Sayfa (`/`)

| Bölüm | Açıklama |
|-------|----------|
| **Duyuru Çubuğu** | Üst şerit: "🚚 500₺ üzeri ücretsiz kargo!" (kapatılabilir) |
| **Header** | Logo, arama çubuğu (autocomplete), hesap, sepet (ürün sayısı badge) |
| **Mega Menü** | Kategoriler + alt kategoriler hover ile açılır |
| **Hero Slider** | Banner'lar (kampanyalar, yeni koleksiyon) — otomatik kayma |
| **Öne Çıkan Kategoriler** | Görsel kartlarla kategori navigasyonu |
| **Vitrin: Yeni Ürünler** | Son eklenen ürünler carousel |
| **Vitrin: Çok Satanlar** | En çok satılan ürünler carousel |
| **Vitrin: İndirimli Ürünler** | Aktif kampanyalı ürünler |
| **Marka Bandı** | Logo carousel (güven sinyali) |
| **Güven Rozeti Şeridi** | Ücretsiz kargo, güvenli ödeme, kolay iade ikonları |
| **Newsletter** | Email abonelik formu (indirim kuponu teşvikli) |
| **Footer** | Firma bilgileri, yasal linkler, sosyal medya, iletişim, ödeme ikonları |

### 4.2 Ürün Listeleme (`/kategori/{slug}`, `/urunler`, `/arama`)

| Özellik | Detay |
|---------|-------|
| **Breadcrumb** | Ana Sayfa > Giyim > Tişört (schema.org BreadcrumbList) |
| **Kategori Başlığı** | Kategori adı + ürün sayısı + SEO açıklama |
| **Filtreler (sidebar)** | Fiyat aralığı (slider), renk (renk kutuları), beden, marka, puan, stok durumu |
| **Sıralama** | Önerilen, fiyat (artan/azalan), yeni eklenen, çok satan, çok değerlendirilen |
| **Görünüm** | Grid (2/3/4 sütun) / Liste toggle |
| **Ürün Kartı** | Görsel (hover'da 2. görsel), başlık, fiyat (eski/yeni), indirim %, yıldız puan, "Sepete Ekle", "Favorilere Ekle" ♡ |
| **Hızlı Görüntüleme** | Ürün kartındaki 👁 butonu → modal'da detay (sepete ekle dahil) |
| **Pagination** | Sayfa numaraları veya "Daha Fazla Yükle" butonu |
| **Boş Durum** | "Bu kriterlere uygun ürün bulunamadı" + filtre temizle butonu |

### 4.3 Ürün Detay (`/urun/{slug}`)

| Bölüm | Detay |
|-------|-------|
| **Breadcrumb** | Ana Sayfa > Giyim > Tişört > Siyah Basic Tişört |
| **Görsel Galeri** | Ana görsel + thumbnail'lar, zoom (hover/click), lightbox, kaydırma |
| **Başlık & Marka** | Ürün adı, marka (link), stok kodu |
| **Fiyat** | ~~Eski fiyat~~ Yeni fiyat, indirim % badge, KDV dahil notu |
| **Varyant Seçimi** | Renk (renk kutuları ile), beden (butonlar ile), seçime göre fiyat/stok güncelle |
| **Stok Durumu** | "Stokta ✓", "Son 3 ürün! 🔥", "Tükendi ✗" |
| **Miktar** | +/- butonlu sayı seçici |
| **Sepete Ekle** | Büyük CTA butonu, eklendikten sonra mini-cart açılır |
| **Favorilere Ekle** | ♡ ikonu |
| **Paylaş** | WhatsApp, Facebook, Twitter, Link kopyala |
| **Taksit Tablosu** | "Taksit Seçenekleri" tab'ı — banka bazlı taksit tablosu |
| **Ürün Açıklaması** | Tab: açıklama HTML |
| **Özellikler Tablosu** | Tab: renk, beden, materyal, ağırlık vb. |
| **Teslimat Bilgisi** | Tab: tahmini teslimat süresi, kargo firması, ücretsiz kargo bilgisi |
| **İade Bilgisi** | Tab: 14 gün cayma hakkı, iade koşulları |
| **Değerlendirmeler** | Tab: yıldız dağılımı, yorumlar listesi, "Yorum Yaz" butonu |
| **SSS** | Tab: Ürüne özel SSS (AEO için FAQPage schema) |
| **İlgili Ürünler** | Aynı kategorideki ürünler carousel |
| **Son Görüntülenenler** | Kullanıcının daha önce baktığı ürünler (localStorage) |
| **Stok Bildirimi** | Tükenmişse: "Stoğa gelince haber ver" → email gir |

### 4.4 Sepet (`/sepet`)

| Bölüm | Detay |
|-------|-------|
| **Ürün Listesi** | Görsel, ad, varyant (renk/beden), birim fiyat, miktar (+/-), satır toplamı, sil butonu |
| **Kupon Kodu** | Input + "Uygula" butonu → indirim göster |
| **Sipariş Özeti** | Ara toplam, kargo, indirim, KDV, genel toplam |
| **Kargo Tahmini** | "X₺ daha ekleyin, ücretsiz kargo kazanın!" progress bar |
| **CTA Butonlar** | "Alışverişe Devam Et" + "Sepeti Onayla" |
| **Çapraz Satış** | "Bunları da beğenebilirsiniz" ürün önerileri |
| **Boş Sepet** | "Sepetiniz boş" + öne çıkan ürünler |

### 4.5 Ödeme (`/odeme`)

| Adım | Detay |
|------|-------|
| **1. Hesap** | Giriş / Kayıt / Misafir olarak devam |
| **2. Teslimat Adresi** | Kayıtlı adres seçimi veya yeni adres formu (il/ilçe/mahalle/sokak/posta kodu) |
| **3. Fatura Adresi** | "Teslimat adresiyle aynı" checkbox veya ayrı adres |
| **4. Kargo Seçimi** | Kargo firması seçenekleri + tahmini süre + ücret |
| **5. Ödeme Yöntemi** | Kredi/banka kartı (iyzico/PayTR embedded form), kapıda ödeme, havale/EFT |
| **Taksit Seçimi** | Kart numarasının ilk 6 hanesiyle (BIN) banka tespit → taksit oranları göster |
| **Sözleşmeler** | ☑ Mesafeli Satış Sözleşmesi (okuyup onaylayın — modal ile göster) |
|  | ☑ Ön Bilgilendirme Formu |
|  | ☑ KVKK Aydınlatma Metni |
| **Sipariş Özeti** | Sağ panelde: ürünler, kargo, indirim, toplam |
| **"Siparişi Tamamla"** | → 3D Secure → callback → başarı/hata |

### 4.6 Sipariş Başarılı (`/odeme/basarili`)
- "Siparişiniz alındı! 🎉"
- Sipariş numarası
- Tahmini teslimat tarihi
- Sipariş detaylarına link
- "Alışverişe Devam Et" butonu

### 4.7 Müşteri Hesabı (`/hesabim`)

| Sayfa | İçerik |
|-------|--------|
| **Siparişlerim** | Sipariş listesi (tarih, tutar, durum badge), detay linki |
| **Sipariş Detay** | Ürünler, adres, ödeme bilgisi, kargo takip no + takip linki, fatura indir |
| **İade Talebi** | Sipariş seç → ürün seç → sebep → fotoğraf yükle → gönder |
| **Adreslerim** | Kayıtlı adresler listesi, ekle/düzenle/sil, varsayılan seç |
| **Favorilerim** | Beğenilen ürünler grid'i, "Sepete Ekle" butonu, tükenmiş uyarısı |
| **Profilim** | Ad, soyad, email, telefon, şifre değiştir |
| **Bildirim Tercihleri** | ☑ Sipariş güncellemeleri, ☑ Kampanyalar, ☑ Stok bildirimleri |

### 4.8 Sipariş Takip (`/Sipariş-takip`)
- Sipariş no veya kargo takip no ile sorgulama (giriş gerektirmez)
- Durum timeline: Sipariş Alındı → Hazırlanıyor → Kargoya Verildi → Yolda → Teslim Edildi
- Kargo firması + takip linki

### 4.9 Arama (`/arama?q=...`)
- **Autocomplete**: Yazmaya başlayınca ürün + kategori + marka önerileri (AJAX)
- **Sonuç sayfası**: Filtreler + grid (ürün listeleme ile aynı layout)
- **Boş sonuç**: "Sonuç bulunamadı" + öneri ürünler
- **Popüler aramalar**: Boş arama kutusunda göster

### 4.10 Bilgi Sayfaları

| Sayfa | URL | İçerik |
|-------|-----|--------|
| Hakkımızda | `/hakkimizda` | Firma tanıtımı (WYSIWYG HTML) |
| İletişim | `/iletisim` | Form (ad, email, mesaj) + harita embed + adres/telefon |
| Kullanıcı Sözleşmesi | `/kullanim-kosullari` | Yasal metin |
| Gizlilik Politikası | `/gizlilik-politikasi` | Yasal metin |
| KVKK | `/kvkk` | Yasal metin |
| Çerez Politikası | `/cerez-politikasi` | Yasal metin |
| Mesafeli Satış Sözleşmesi | `/mesafeli-satis-sozlesmesi` | Yasal metin |
| Ön Bilgilendirme Formu | `/on-bilgilendirme-formu` | Yasal metin |
| İade & Değişim | `/iade-politikasi` | Yasal metin |
| Teslimat Koşulları | `/teslimat-kosullari` | Yasal metin |
| Özel Sayfalar | `/{custom-slug}` | Dashboard'dan oluşturulan ek sayfalar |

### 4.11 Ortak UI Elementleri (Tüm Sayfalarda)

| Element | Detay |
|---------|-------|
| **Duyuru Çubuğu** | Sayfa üstünde ince renkli şerit, kapatılabilir |
| **Header** | Logo, mega menü, arama, hesap dropdown, sepet (badge ile ürün sayısı) |
| **Breadcrumb** | Navigasyon yolu (BreadcrumbList schema) |
| **Footer** | 4 sütun: Kurumsal linkler, müşteri hizmetleri, iletişim, sosyal medya |
| **Footer Alt** | © Copyright, ödeme yöntemleri logoları (Visa, MC, Troy, iyzico badge) |
| **WhatsApp Butonu** | Sağ alt köşe, floating, tıklayınca WhatsApp Web açar |
| **Çerez Onayı** | Alt'ta banner: "Çerezleri kabul edin" + detay linki (KVKK) |
| **Yukarı Kaydır** | Sayfa aşağı scrolllandığında görünen ↑ butonu |
| **Loading** | Sayfa geçişlerinde skeleton loader |
| **Responsive** | Mobile-first: hamburger menü, tek sütun grid, bottom nav bar |
| **Mobile Bottom Nav** | Ana Sayfa, Kategoriler, Sepet, Hesap — sabit alt çubuk |

---

## 6. SEO Optimizasyonu (Teknik Detaylar)

### 5.1 URL Yapısı (SEO-Friendly Slug)

```
/                                    → Ana sayfa
/urunler                             → Tüm ürünler
/kategori/{slug}                     → Kategori
/kategori/{parent}/{child}           → Alt kategori
/urun/{slug}                         → Ürün detay
/marka/{slug}                        → Marka sayfası
/arama?q={query}                     → Arama
/yeni-urunler                        → Yeni ürünler
/cok-satanlar                        → Çok satanlar
/indirimli-urunler                   → İndirimli ürünler
/sepet                               → Sepet
/odeme                               → Ödeme
/Sipariş-takip                       → Sipariş takip
/hesabim/*                           → Müşteri paneli
/{yasal-slug}                        → Yasal sayfalar
/robots.txt                          → Dinamik robots
/sitemap.xml                         → Dinamik sitemap
```

### 5.2 Mevcut Entity'lere Eklenecek SEO Alanları

```
Product  → + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Category → + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Brand    → + SeoSlug (unique)
```

### 5.3 Her Sayfada Meta Tags

```html
<title>{SeoTitle} | {StoreName}</title>
<meta name="description" content="{SeoDescription}" />
<link rel="canonical" href="https://{domain}/{slug}" />
<meta property="og:title/description/image/url/type/locale" />
<meta property="product:price:amount/currency" />  <!-- ürün sayfaları -->
<meta name="twitter:card/title/description/image" />
```

### 5.4 Dinamik Sitemap & Robots (tenant-aware)

`SeoController` → her tenant için kendi sitemap/robots üretir.

### 5.5 Performans

- Saf HTML output (MVC Razor — JS overhead yok)
- Response caching (ürün 5dk, kategori 10dk, statik 1 saat)
- Image: WebP, lazy loading, srcset, CDN
- Critical CSS inline, font preloading
- Core Web Vitals hedef: LCP < 2.5s, FID < 100ms, CLS < 0.1

---

## 7. AEO Optimizasyonu (AI Arama Motorları)

### 6.1 JSON-LD Structured Data

**Her ürün sayfasında:**
```json
{ "@type": "Product", "name", "image", "brand", "sku", "gtin13",
  "offers": { "price", "priceCurrency", "availability",
    "shippingDetails": { "deliveryTime" },
    "hasMerchantReturnPolicy": { "merchantReturnDays": 14 }
  },
  "aggregateRating": { "ratingValue", "reviewCount" },
  "review": [{ "@type": "Review", "author", "datePublished", "reviewBody", "reviewRating" }]
}
```

**Ana sayfa:** `Store` schema (ad, adres, telefon, çalışma saatleri, sosyal linkler)
**Kategori:** `CollectionPage` schema
**Yasal sayfalar:** `FAQPage` schema (SSS formatında — AEO için kritik)
**Breadcrumb:** `BreadcrumbList` schema (her sayfada)
**Arama:** `SearchAction` schema (sitelinks search box)

### 6.2 AEO-Spesifik

- **FAQ Schema**: Ürün sayfalarında otomatik SSS (kargo süresi, iade, garanti)
- **Speakable Schema**: Sesli asistanlar için ürün adı + fiyat + stok
- **Review Schema**: Müşteri değerlendirmeleri (güven sinyali)
- **HowTo Schema**: Ürün bakım/kullanım talimatları (opsiyonel)

---

## 8. GEO Optimizasyonu

- **LocalBusiness JSON-LD**: Adres, telefon, çalışma saatleri
- **Google Business Profile** bağlantısı
- **Yerel para birimi**: ₺1.234,56 formatı
- **İl bazlı teslimat**: "İstanbul'a 1 iş günü"
- **Hreflang**: Çoklu dil desteği (gelecek faz)

---

## 9. Dashboard Entegrasyonu

### 8.1 Storefront = MarketPlaceId = 10

Mevcut marketplace altyapısına tam entegre:
- **Ürün publish**: `ProductMarketplace` ile storefront'a yayınla
- **Stok sync**: `StockPriceChangedEvent` → storefront + 7 pazaryeri (fire-and-forget)
- **Siparişler**: `Order` tablosuna `MarketPlaceId=10` ile düşer
- **Raporlar**: Tüm raporlar storefront satışlarını da kapsar
- **Fiyat**: `ProductVariantMarketplaceOverride` ile mağaza özel fiyat

### 8.2 Sipariş Akışı

```
Müşteri → Sepet → Ödeme → 3D Secure → Sipariş
  ├── Order oluştur (MarketPlaceId=10)
  ├── DecreaseStockAtomicAsync → StockPriceChangedEvent → tüm kanallar sync
  ├── Email: sipariş onayı → müşteriye
  └── SignalR: bildirim → dashboard'a
```

### 8.3 Dashboard'a Eklenecek Yeni Sayfalar

| Sayfa | URL | İçerik |
|-------|-----|--------|
| Mağaza Kurulum Wizard | `/setup/storefront` | 8 adımlı wizard |
| Mağaza Ayarları | `/settings/storefront` | Tema, renk, logo, içerik düzenleme |
| Sanal POS Ayarları | `/settings/payment` | POS bilgileri, taksit, ödeme yöntemleri |
| Yasal Metinler | `/settings/legal` | WYSIWYG editörle yasal metin düzenleme |
| Banner Yönetimi | `/settings/banners` | Hero slider, kampanya banner ekle/düzenle |
| Özel Sayfalar | `/settings/pages` | Ek sayfalar oluştur/düzenle |
| Değerlendirmeler | `/reviews` | Müşteri yorumları, onayla/reddet, yanıtla |
| İade Talepleri | `/returns` | İade taleplerini yönet |

---

## 10. Yeni Entity'ler

### StorefrontSettings (tenant bazlı — tüm mağaza ayarları)
```
TenantId (unique), ThemeId, StoreName, StoreSlogan
LogoUrl, FaviconUrl, PrimaryColor, SecondaryColor, AccentColor, CustomCss
CompanyName, CompanyTaxOffice, CompanyTaxNumber, MersisNumber, KepAddress
ContactPhone, WhatsAppNumber, ContactEmail, Address, City, District
Instagram/Facebook/Twitter/YouTube/TikTokUrl
GoogleAnalyticsId, GoogleTagManagerId, FacebookPixelId
AnnouncementBarText, AnnouncementBarActive, AnnouncementBarColor
AboutHtml, ReturnPolicyHtml, PrivacyPolicyHtml, TermsHtml
KvkkHtml, CookiePolicyHtml, DistanceSalesContractHtml, PreInfoFormHtml, DeliveryTermsHtml
DefaultSeoTitle, DefaultSeoDescription, DefaultSeoKeywords
FreeShippingThreshold, FlatShippingRate, EstimatedDeliveryDays, DefaultCargoCompanyId
CashOnDeliveryEnabled, CashOnDeliveryFee, CashOnDeliveryCardEnabled, CashOnDeliveryCardFee
BankTransferEnabled, BankName, BankIban, BankAccountHolder
MinOrderAmount, IsMaintenanceMode, MaintenanceMessage
CookieConsentActive, CookieConsentText, IsWhatsAppWidgetActive
HomepageLayout (JSON), NewsletterEnabled
```

### StorefrontCustomerAuth (müşteri kimlik doğrulama)
```
TenantId, CustomerId (FK → Customer)
Email (unique per tenant), PasswordHash, PasswordSalt
EmailConfirmed, EmailConfirmationToken
PasswordResetToken, PasswordResetTokenExpiresAt
LastLoginAt, LoginFailedCount, LockedUntil
MarketingConsent, MarketingConsentDate, KvkkConsentDate
```

### StorefrontCustomerAddress (müşteri adresleri)
```
TenantId, CustomerId (FK)
AddressTitle ("Ev", "İş", "Diğer")
FullName, Phone
City, District, Neighborhood, Street, BuildingNo, ApartmentNo, PostalCode
FullAddress (computed/concat)
IsDefault (bool)
```

### StorefrontPaymentConfig (sanal POS)
```
TenantId, PaymentProvider (enum: Iyzico, PayTR, Param)
MerchantId, ApiKey (encrypted), SecretKey (encrypted)
IsLive (bool), InstallmentEnabled, MaxInstallmentCount, MinInstallmentAmount
LastTestedAt, LastTestResult (bool)
IsActive
```

### StorefrontInstallmentRate (banka bazlı taksit oranları)
```
PaymentConfigId (FK), BankName, InstallmentCount, CommissionRate (%)
```

### StorefrontDomainMapping
```
TenantId, DomainName (unique), IsSubdomain, SslStatus (enum), SslExpiresAt, IsPrimary, IsActive
```

### StorefrontBanner
```
TenantId, Title, ImageUrl, MobileImageUrl, LinkUrl
Position (enum: Hero/Sidebar/Footer/Popup), DisplayOrder
StartDate, EndDate, IsActive
```

### StorefrontPage (özel sayfalar)
```
TenantId, Title, Slug, ContentHtml, SeoTitle, SeoDescription
IsPublished, DisplayOrder, ShowInNavigation, ShowInFooter
```

### StorefrontReview (ürün değerlendirme)
```
TenantId, ProductId (FK), CustomerId (FK)
Rating (1-5), Title, Comment
IsApproved, IsVerifiedPurchase, ReplyText, RepliedAt
```

### StorefrontWishlist (favori)
```
TenantId, CustomerId (FK), ProductVariantId (FK), AddedAt
```

### StorefrontStockNotification (stok bildirimi)
```
TenantId, ProductVariantId (FK), Email, IsNotified, NotifiedAt
```

### Cart + CartItem (sepet)
```
Cart: Id (Guid), TenantId, CustomerId?, SessionId, CouponCode?, ExpiresAt
CartItem: CartId (FK), ProductVariantId (FK), Quantity, UnitPrice, AddedAt
```

### StorefrontReturnRequest (iade talebi)
```
TenantId, OrderId (FK), CustomerId (FK)
Status (enum: Pending/Approved/Rejected/Completed)
Reason, Description, ImageUrls (JSON)
CreatedAt, ReviewedAt, ReviewedByUserId, ReviewNote
RefundAmount, RefundMethod
```

### StorefrontNewsletter (email abonelik)
```
TenantId, Email (unique per tenant), Name?, SubscribedAt, IsActive
```

### StorefrontContactMessage (iletişim formu)
```
TenantId, Name, Email, Phone?, Subject?, Message
IsRead, ReadAt, ReplyText?, RepliedAt?
```

### Mevcut entity'lere SEO alanları
```
Product  → + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Category → + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Brand    → + SeoSlug (unique)
```

---

## 11. Ödeme Entegrasyonu

### IPaymentGatewayService (adapter pattern)
```csharp
public interface IPaymentGatewayService
{
    Task<IDataResult<PaymentInitResult>> InitiatePaymentAsync(PaymentRequest request);
    Task<IDataResult<PaymentResult>> HandleCallbackAsync(string callbackData);
    Task<IDataResult<RefundResult>> RefundAsync(RefundRequest request);
    Task<IDataResult<List<InstallmentOption>>> GetInstallmentsAsync(decimal amount, string binNumber);
    Task<IResult> TestConnectionAsync();  // bağlantı testi
}
```

### Implementasyonlar
- `IyzicoPaymentService` → iyzico API (3D Secure, taksit, iade)
- `PayTRPaymentService` → PayTR API (iframe, 3D Secure, taksit)
- Seçim: `StorefrontPaymentConfig.PaymentProvider`'a göre doğru servis inject

### Akış
```
Checkout → BIN (ilk 6 hane) → GetInstallments → Taksit seç →
  → InitiatePayment → 3D Secure (banka sayfası) →
  → Callback URL → HandleCallback → Başarılı? →
    → Order oluştur + Stok düş + Email + Redirect
```

---

## 12. Güvenlik

- **Auth**: Cookie-based session (dashboard'dan bağımsız), email+şifre, şifre sıfırlama
- **CSRF**: MVC anti-forgery (otomatik)
- **Rate Limiting**: Login, register, iletişim formu
- **XSS**: HTML sanitizer (yasal metin editörü + iletişim formu)
- **PCI DSS**: Kart bilgisi sunucuya gelmez (iframe/embedded form)
- **Encryption**: API key/secret AES-256 encrypted at rest
- **HTTPS Only**: Tüm mağazalar SSL zorunlu
- **KVKK**: Çerez onayı, aydınlatma metni, veri silme talebi hakkı

---

## 13. Performans & Caching

```
Response Cache:
├── Ana sayfa         → 60 sn
├── Kategori/ürünler  → 5 dk
├── Ürün detay        → 5 dk (stok değişince invalidate)
├── Statik sayfalar   → 1 saat
├── Sitemap/robots    → 1 saat
└── Taksit oranları   → 24 saat

Redis:
├── Tenant settings   → 10 dk
├── Kategori ağacı    → 30 dk
├── Sepet             → 7 gün
├── Son görüntülenen  → 30 gün
└── Autocomplete idx  → 1 saat
```

Image: MinIO → WebP resize (150/400/800px) → CDN Cache

---

## 14. Bildirimler

**Müşteriye (email):**
Kayıt hoşgeldin, sipariş onayı, kargo (takip no), teslim, şifre sıfırlama, iade durumu, stok bildirimi, newsletter

**Dashboard'a (SignalR):**
Yeni sipariş, düşük stok, yeni değerlendirme, yeni iletişim mesajı, iade talebi

---

## 15. Fazlama

### Faz 1: MVP
- MVC storefront projesi + tenant middleware + Default tema (Tailwind)
- Ana sayfa, ürün listeleme/detay, kategori, arama (autocomplete)
- Sepet (session-based), müşteri kayıt/giriş
- iyzico ödeme (3D Secure, taksit)
- Sipariş oluşturma (MarketPlaceId=10), email bildirim
- SEO: meta tags, sitemap, robots, JSON-LD structured data
- Kurulum wizard (dashboard'da)
- Sanal POS ayar sayfası (dashboard'da)
- Yasal metinler (varsayılan şablonlar)
- Nginx: domain routing + SSL

### Faz 2: Zenginleştirme
- Ek temalar (Modern, Classic, Minimal)
- Ürün değerlendirme/yorum + admin onay
- Kupon/indirim kodu (mevcut DiscountVoucher)
- Favori ürünler (wishlist)
- Son görüntülenenler
- İade talebi sistemi
- Banner/kampanya yönetimi
- PayTR entegrasyonu
- Kapıda ödeme, havale/EFT
- Newsletter + stok bildirimi
- İletişim formu + mesaj yönetimi
- Gelişmiş filtreleme (fiyat, renk, beden, marka, puan)

### Faz 3: İleri
- Çoklu dil (i18n), çoklu para birimi
- Blog/içerik yönetimi
- Social login (Google/Facebook)
- PWA, push bildirimler
- Canlı destek (chat widget)
- Gelişmiş analytics, A/B test

### Faz 4: Marketplace
- Multi-vendor, satıcı paneli, komisyon, payout

---

## 16. Email & Bildirim Altyapısı

### 16.1 Email Servis Altyapısı

**Transactional Email Provider:** SendGrid veya Mailgun (tenant bazlı API key)

```
Dashboard → Ayarlar → Email Ayarları
├── Provider seçimi: SendGrid / Mailgun / SMTP
├── API Key (encrypted at rest, AES-256)
├── Gönderici adı: "Ahmet Tekstil"
├── Gönderici email: info@magaza-ali.com (SPF/DKIM doğrulanmış)
├── Reply-to email: destek@magaza-ali.com
├── [🔌 Bağlantıyı Test Et] → test email gönder
└── Günlük gönderim limiti (rate limit koruması)
```

**Teknik:**
- `IEmailService` adapter pattern (SendGrid/Mailgun/SMTP implementasyonları)
- Tenant bazlı config: `StorefrontEmailConfig` entity
- Queue-based gönderim: background service + retry (3 deneme, exponential backoff)
- Email log: her gönderim kaydedilir (tarih, alıcı, tip, durum, hata mesajı)
- Bounce/complaint webhook: otomatik unsubscribe

### 16.2 Email Template Engine

**Razor-based template engine:** Her email tipi için özelleştirilebilir HTML template.

```
Dashboard → Ayarlar → Email Şablonları
├── Şablon listesi (tip bazlı)
├── WYSIWYG editör + HTML modu
├── Değişken listesi ({{ CustomerName }}, {{ OrderNumber }}, {{ TrackingUrl }} vb.)
├── Önizleme butonu (masaüstü + mobil)
├── Test email gönder butonu
└── "Varsayılana Sıfırla" butonu
```

**Zorunlu Email Şablonları:**

| Tip | Tetikleyici | Değişkenler |
|-----|-------------|-------------|
| **Hoş Geldin** | Kayıt | Ad, mağaza adı, doğrulama linki |
| **Email Doğrulama** | Kayıt / email değişikliği | Ad, doğrulama linki (1 saat geçerli) |
| **Şifre Sıfırlama** | Şifremi unuttum | Ad, sıfırlama linki (1 saat geçerli) |
| **Sipariş Onayı** | Sipariş tamamlandı | Sipariş no, ürünler, fiyatlar, adres, tahmini teslimat |
| **Ödeme Başarısız** | 3D Secure başarısız | Sipariş özeti, tekrar dene linki |
| **Kargoya Verildi** | Kargo takip no girildi | Takip no, kargo firması, takip linki |
| **Teslim Edildi** | Kargo teslim | Sipariş no, değerlendirme yazma linki |
| **İade Talebi Alındı** | İade oluşturuldu | İade no, ürünler, tahmini süre |
| **İade Onaylandı** | İade onay | İade no, iade tutarı, iade yöntemi |
| **İade Reddedildi** | İade red | İade no, red sebebi |
| **Sipariş İptali** | İptal onaylandı | Sipariş no, iade tutarı |
| **Fiyat Düşüş Bildirimi** | Favori ürünün fiyatı düştü | Ürün, eski fiyat, yeni fiyat, link |
| **Stok Bildirimi** | Tükenen ürün stoğa geldi | Ürün, link |
| **Terk Edilmiş Sepet** | 1 saat / 24 saat sonra | Sepet ürünleri, toplam, devam linki |
| **Hoş Geldin Kuponu** | Kayıt sonrası (opsiyonel) | Kupon kodu, indirim %, son geçerlilik |
| **Doğum Günü** | Müşteri doğum günü | Ad, özel kupon/indirim |
| **Newsletter** | Kampanya gönderimi | Dinamik içerik (dashboard'dan oluşturulur) |
| **Hesap Silme Onayı** | KVKK veri silme | Silme tarihi, veri silme özeti |

### 16.3 SMS Bildirim Altyapısı

**SMS Provider:** Netgsm / İleti Merkezi / Twilio

```
Dashboard → Ayarlar → SMS Ayarları
├── Provider seçimi: Netgsm / İleti Merkezi / Twilio
├── API credentials (encrypted)
├── Gönderici başlığı: "AHMET TEKS" (İYS kayıtlı)
├── İYS (İleti Yönetim Sistemi) entegrasyonu
├── [🔌 Bağlantıyı Test Et]
└── Aktif/Pasif toggle (SMS kullanmak istemeyen müşteri kapatabilir)
```

**SMS Şablonları:**

| Tip | İçerik |
|-----|--------|
| **Kayıt OTP** | "Doğrulama kodunuz: {OTP}. 5 dakika geçerlidir." |
| **Sipariş Onayı** | "Siparişiniz #{OrderNo} alındı. Takip: {Link}" |
| **Kargoya Verildi** | "Siparişiniz kargoya verildi. Takip no: {TrackingNo}" |
| **Teslim Edildi** | "Siparişiniz teslim edildi. İyi günlerde kullanın!" |
| **Şifre Sıfırlama OTP** | "Şifre sıfırlama kodunuz: {OTP}. 5 dakika geçerlidir." |

**Teknik:**
- `ISmsService` adapter pattern
- İYS (İleti Yönetim Sistemi) uyumu zorunlu (ticari SMS için yasal gereklilik)
- Müşteri SMS izni: kayıtta ayrı checkbox, istediği zaman iptal
- Rate limiting: aynı numaraya dakikada max 1 SMS

### 16.4 Push Bildirimler (Web Push — Faz 2)

- Service Worker tabanlı web push
- Müşteri izin isteme popup'ı (ilk ziyarette değil, 2. ziyarette veya sepete ekleme sonrası)
- Bildirim tipleri: sipariş güncelleme, kampanya, fiyat düşüşü, stok uyarısı

---

## 17. Email Doğrulama & Kimlik Doğrulama Detayları

### 17.1 Email Doğrulama Akışı

```
Kayıt → RetailCustomer + StorefrontCustomerAuth oluştur (EmailConfirmed = false)
  → Doğrulama emaili gönder (cryptographic token, 1 saat geçerli)
  → Kullanıcı linke tıklar → /hesabim/email-dogrula?token={token}
  → Token geçerli & süresi dolmamış → EmailConfirmed = true
  → Token süresi dolmuş → "Süre doldu, tekrar gönder" butonu
```

**Kurallar:**
- Doğrulanmamış hesap ile giriş yapılabilir AMA sipariş verilemez
- Doğrulama emaili tekrar gönderme: max 3/saat (rate limit)
- Token: `RandomNumberGenerator.GetBytes(32)` → Base64Url encode
- Email değişikliğinde yeniden doğrulama gerekir (eski email'e bildirim gönderilir)

### 17.2 Telefon Doğrulama (Opsiyonel — Tenant Ayarı)

```
Dashboard → Ayarlar → Güvenlik
├── ☑ Telefon doğrulama zorunlu (kayıtta)
├── ☑ Telefon doğrulama zorunlu (siparişte)
└── OTP süresi: 5 dakika
```

**Akış:**
- Kayıt/sipariş sırasında telefon numarasına 6 haneli OTP gönderilir
- Max 3 deneme hakkı, 5 dakika timeout
- Aynı numaraya max 5 OTP/gün

### 17.3 Sosyal Giriş (Social Login)

**Faz 1:** Google OAuth 2.0 (en yaygın, en kolay kurulum)
**Faz 2:** Apple Sign In + Facebook Login

```
Dashboard → Ayarlar → Sosyal Giriş
├── Google
│   ├── Client ID
│   ├── Client Secret (encrypted)
│   ├── Aktif/Pasif
│   └── [Bağlantıyı Test Et]
├── Apple (Faz 2)
└── Facebook (Faz 2)
```

**Akış:**
```
[Google ile Giriş] → Google OAuth → email alınır
  → Email ile StorefrontCustomerAuth var mı?
    → Varsa: giriş yap (email otomatik doğrulanmış sayılır)
    → Yoksa: otomatik kayıt (ad/soyad Google'dan, email doğrulanmış)
  → ExternalLoginProvider, ExternalLoginId alanları StorefrontCustomerAuth'a eklenir
```

**StorefrontCustomerAuth'a eklenen alanlar:**
```
+ ExternalLoginProvider (enum?: Google, Apple, Facebook — null = email/şifre ile kayıt)
+ ExternalLoginId (string? — provider'dan gelen unique ID)
+ PhoneNumber (string?)
+ PhoneNumberConfirmed (bool)
+ PhoneConfirmationOtp (string?)
+ PhoneConfirmationOtpExpiresAt (DateTimeOffset?)
+ TwoFactorEnabled (bool)
+ TwoFactorSecret (string? — TOTP secret key)
```

### 17.4 İki Faktörlü Doğrulama (2FA)

```
/hesabim/guvenlik → "İki Faktörlü Doğrulama" bölümü
├── TOTP (Google Authenticator / Authy) — QR kod göster → 6 haneli kod doğrula → aktifleştir
├── SMS OTP — mevcut SMS altyapısı kullanılır
└── Kurtarma kodları: 10 adet tek kullanımlık kod (göster + indir)
```

**Akış:**
```
Email + Şifre → doğru → 2FA aktif mi?
  → Hayır: giriş tamamla
  → Evet: 2FA sayfasına yönlendir → TOTP/SMS kodu gir → doğruysa giriş tamamla
```

**Entity: StorefrontTwoFactorRecoveryCode**
```
AuthId (FK → StorefrontCustomerAuth), Code (hashed), IsUsed, UsedAt
```

---

## 18. Terk Edilmiş Sepet Kurtarma (Abandoned Cart Recovery)

### 18.1 Tanım
Müşteri sepete ürün ekleyip satın alma işlemini tamamlamadan siteden ayrılırsa, otomatik email/bildirim ile geri kazanma.

### 18.2 Tetikleme Kuralları

| Zamanlama | Aksiyon | İçerik |
|-----------|---------|--------|
| **1 saat** | Email #1 | "Sepetinizde ürünler bekleniyor!" — ürün görselleri + "Alışverişi Tamamla" CTA |
| **24 saat** | Email #2 | "Hâlâ düşünüyor musunuz?" — ürünler + fiyat bilgisi |
| **72 saat** | Email #3 (opsiyonel) | "Son şans!" + %5-10 indirim kuponu (tenant ayarına bağlı) |
| **7 gün** | Sepet temizleme | Sepet verileri silinir (CartItem'lar kalır, fiyat snapshot olarak) |

### 18.3 Koşullar
- Sadece giriş yapmış (veya email bırakmış) müşterilere gönderilir
- Müşteri email tercihlerinde "Sepet hatırlatması" kapalıysa gönderilmez
- Sipariş verildiyse otomatik iptal (duplicate gönderim yok)
- Unsubscribe linki her emailde zorunlu

### 18.4 Dashboard Ayarları

```
Dashboard → Ayarlar → Pazarlama → Terk Edilmiş Sepet
├── ☑ Aktif / Pasif
├── 1. email süresi: [1 saat ▼]
├── 2. email süresi: [24 saat ▼]
├── 3. email aktif: ☑ / süre: [72 saat ▼]
├── 3. emailde indirim kuponu: ☑ / oran: [%5 ▼]
├── Email şablonlarını düzenle
└── İstatistik: gönderildi / açıldı / tıklandı / dönüşüm
```

### 18.5 Entity: StorefrontAbandonedCartEmail
```
TenantId, CartId (FK), CustomerId (FK)
EmailStep (1/2/3), SentAt, OpenedAt?, ClickedAt?, ConvertedAt?
CouponCode? (3. adım kuponu)
Status (enum: Pending/Sent/Opened/Clicked/Converted/Unsubscribed)
```

---

## 19. Sipariş İptali & Yönetimi

### 19.1 Müşteri Tarafından İptal

```
/hesabim/Siparişlerim → Sipariş Detay → [Siparişi İptal Et]
```

**İptal kuralları:**
| Sipariş Durumu | İptal Edilebilir? | Açıklama |
|----------------|-------------------|----------|
| Sipariş Alındı | ✅ Anında | Otomatik iptal, ödeme iadesi başlatılır |
| Hazırlanıyor | ✅ Onay gerekli | Dashboard'a bildirim, mağaza onayı beklenir |
| Kargoya Verildi | ❌ | İade süreci başlatılmalı |
| Teslim Edildi | ❌ | İade süreci başlatılmalı |
| İptal Edildi | ❌ | Zaten iptal |

**İptal akışı:**
```
Müşteri "İptal Et" → Sebep seç (dropdown: Vazgeçtim, Yanlış sipariş, Teslimat çok uzun, Diğer)
  → Durum "Sipariş Alındı" ise: otomatik iptal → ödeme iadesi başlat → email
  → Durum "Hazırlanıyor" ise: iptal talebi → dashboard bildirim → mağaza onay/red → email
```

### 19.2 Mağaza Tarafından İptal (Dashboard)

```
Dashboard → Siparişler → Sipariş Detay → [Siparişi İptal Et]
├── Sebep: Stok yok / Fiyat hatası / Müşteri talebi / Diğer
├── Müşteriye bildirim: email + SMS
├── Otomatik ödeme iadesi başlatılır
└── Stok geri eklenir
```

### 19.3 Kısmi İptal
- Birden fazla ürünlü siparişte tek ürün iptal edilebilir
- Kalan ürünler için sipariş devam eder
- İptal edilen ürünün tutarı iade edilir (kargo ücreti: son ürün iptalinde iade)

---

## 20. Fatura & Belge Oluşturma

### 20.1 Otomatik Fatura (PDF)

**Sipariş tamamlandığında otomatik fatura PDF oluşturulur:**
- Firma bilgileri (StorefrontSettings'ten)
- Müşteri bilgileri (fatura adresi)
- Ürün detayları: ad, miktar, birim fiyat, KDV oranı, satır toplamı
- Ara toplam, KDV, kargo, indirim, genel toplam
- Fatura no: otomatik sıralı (tenant bazlı `InvoiceSequence`)
- Fatura tarihi
- **Format:** PDF (QuestPDF veya iTextSharp ile oluşturulur)

### 20.2 Fatura Erişimi
- **Müşteri:** `/hesabim/Siparişlerim/{id}` → "Fatura İndir" butonu
- **Dashboard:** Sipariş detay → "Fatura İndir" / "Fatura Gönder" (email)
- **MinIO'da saklanır:** `invoices/{tenantId}/{year}/{invoiceNo}.pdf`

### 20.3 E-Fatura / E-Arşiv Entegrasyonu (Faz 2)

- Mevcut Trendyol e-fatura altyapısı ile entegre
- e-Arşiv fatura zorunlu (5.000₺ üzeri bireysel, tüm kurumsal)
- GİB entegrasyonu: Paraşüt / Logo / Foriba

### 20.4 Entity: StorefrontInvoice
```
TenantId, OrderId (FK), InvoiceNumber (unique per tenant)
InvoiceDate, InvoiceType (enum: Retail/Corporate/EArchive/EInvoice)
CustomerName, CustomerTaxId?, CustomerTaxOffice?
BillingAddress, SubTotal, TaxAmount, ShippingAmount, DiscountAmount, GrandTotal
PdfUrl (MinIO path), EInvoiceUuid?, Status (enum: Created/Sent/Cancelled)
```

---

## 21. Kupon & Kampanya Sistemi

### 21.1 Kupon Tipleri

| Tip | Örnek | Açıklama |
|-----|-------|----------|
| **Yüzde İndirim** | %10 indirim | Sepet toplamından yüzde |
| **Sabit Tutar** | 50₺ indirim | Sepet toplamından sabit tutar |
| **Ücretsiz Kargo** | Kargo bedava | Kargo ücreti sıfırlanır |
| **X Al Y Öde** | 3 al 2 öde | Belirli ürünlerde |
| **İlk Sipariş** | %15 indirim | Sadece ilk siparişte geçerli |
| **Minimum Tutar** | 200₺ üzeri %10 | Minimum sepet tutarı koşulu |

### 21.2 Dashboard Kupon Yönetimi

```
Dashboard → Pazarlama → Kuponlar
├── [+ Yeni Kupon]
│   ├── Kupon kodu: [AUTO-GENERATE] veya [MANUEL]
│   ├── İndirim tipi: Yüzde / Sabit / Ücretsiz Kargo / X Al Y Öde
│   ├── İndirim değeri: [%10] veya [50₺]
│   ├── Minimum sepet tutarı: [₺200]
│   ├── Maksimum indirim tutarı: [₺100] (yüzde için tavan)
│   ├── Geçerlilik: [Başlangıç] - [Bitiş] tarihi
│   ├── Kullanım limiti: toplam [1000] / kişi başı [1]
│   ├── Hedef: Tüm ürünler / Belirli kategoriler / Belirli ürünler / Belirli markalar
│   ├── Müşteri segmenti: Tümü / İlk sipariş / VIP / Belirli müşteriler
│   └── Aktif/Pasif
├── Kupon listesi: kod, tip, değer, kullanım/limit, durum, tarih
├── Kupon performansı: kullanım sayısı, toplam indirim, dönüşüm oranı
└── Toplu kupon oluştur (CSV export ile)
```

### 21.3 Otomatik Kampanyalar

```
Dashboard → Pazarlama → Otomatik Kampanyalar
├── ☑ Hoş geldin kuponu: kayıt sonrası otomatik %10 kupon (email ile gönderilir)
├── ☑ Doğum günü kuponu: doğum gününde otomatik %15 kupon
├── ☑ Terk edilmiş sepet kuponu: 72 saat sonra %5 kupon (bkz. Bölüm 18)
├── ☑ Tekrar alışveriş: 30 gün sipariş vermeyenlere hatırlatma + kupon
└── ☑ Değerlendirme ödülü: yorum yazana %5 kupon
```

### 21.4 Entity: StorefrontCoupon
```
TenantId, Code (unique per tenant), DiscountType (enum: Percentage/FixedAmount/FreeShipping/BuyXPayY)
DiscountValue, MinOrderAmount?, MaxDiscountAmount?
StartDate, EndDate, TotalUsageLimit, PerCustomerLimit, CurrentUsageCount
TargetType (enum: All/Category/Product/Brand), TargetIds (JSON — kategori/ürün/marka ID listesi)
CustomerSegment (enum: All/FirstOrder/Vip/Specific), SpecificCustomerIds (JSON?)
IsActive, IsAutoGenerated, CampaignType (enum?: Welcome/Birthday/AbandonedCart/Reactivation/ReviewReward)
```

### 21.5 Entity: StorefrontCouponUsage
```
CouponId (FK), CustomerId (FK), OrderId (FK), DiscountAmount, UsedAt
```

---

## 22. Ürün Soru-Cevap (Q&A)

### 22.1 Müşteri Tarafı (Storefront)

Ürün detay sayfasında "Soru-Cevap" tab'ı:
- "Soru Sor" butonu → soru formu (giriş gerekli)
- Mevcut sorular listesi: soru, yanıt, tarih, soran kişi (ad maskeli: "Ah***")
- Faydalı bulma: "Bu yanıt faydalıdır" 👍 sayacı
- Sıralama: en yeni / en faydalı

### 22.2 Dashboard Tarafı

```
Dashboard → Soru-Cevap
├── Bekleyen sorular (yanıtlanmamış) — badge ile sayı
├── Yanıtlanmış sorular
├── Soru detay: soru metni, ürün linki, soran müşteri
├── Yanıtla: metin gir → "Yanıtla" → müşteriye email bildirimi
└── Sil / Spam olarak işaretle
```

### 22.3 Entity: StorefrontProductQuestion
```
TenantId, ProductId (FK), CustomerId (FK)
QuestionText, AnswerText?, AnsweredAt?, AnsweredByUserId?
IsPublished, HelpfulCount
```

### 22.4 SEO Etkisi
- Soru-cevaplar `FAQPage` schema ile işaretlenir → AEO'da doğrudan görünür
- Ürün sayfasına doğal anahtar kelime zenginliği ekler

---

## 23. Ürün Karşılaştırma

### 23.1 Akış
- Ürün kartında / detay sayfasında "Karşılaştır" ikonu (⇄)
- Floating bar: "3 ürün karşılaştırılıyor" → [Karşılaştır] butonu
- Max 4 ürün (aynı veya farklı kategori)

### 23.2 Karşılaştırma Sayfası (`/karsilastir`)

```
┌──────────────────┬──────────┬──────────┬──────────┐
│                  │ Ürün A   │ Ürün B   │ Ürün C   │
├──────────────────┼──────────┼──────────┼──────────┤
│ Görsel           │ [img]    │ [img]    │ [img]    │
│ Fiyat            │ ₺299     │ ₺349     │ ₺279     │
│ Marka            │ XYZ      │ ABC      │ XYZ      │
│ Renk             │ Siyah    │ Beyaz    │ Mavi     │
│ Beden            │ S-XL     │ S-XXL   │ M-XL     │
│ Materyal         │ Pamuk    │ Polyester│ Pamuk    │
│ Puan             │ ★4.5     │ ★4.2     │ ★4.8     │
│ Stok             │ ✓ Var    │ ✗ Yok   │ ✓ Var    │
│ Kargo            │ Ücretsiz │ ₺29,90  │ Ücretsiz │
├──────────────────┼──────────┼──────────┼──────────┤
│                  │ [Sepete] │ [Sepete] │ [Sepete] │
└──────────────────┴──────────┴──────────┴──────────┘
- "Sadece farklılıkları göster" toggle
- localStorage'da saklanır (giriş gerektirmez)
```

---

## 24. Hediye Kartı & Hediye Çeki

### 24.1 Hediye Kartı (Gift Card)

```
/hediye-karti → Hediye kartı satın al
├── Tutar seçimi: ₺50 / ₺100 / ₺250 / ₺500 / Özel tutar
├── Gönderim: Email / SMS / Fiziksel kart (Faz 3)
├── Alıcı bilgileri: ad, email/telefon
├── Gönderici mesajı: "Doğum günün kutlu olsun!"
├── Teslimat tarihi: hemen veya ileri tarih
└── Ödeme → hediye kartı kodu oluştur → alıcıya gönder
```

**Kullanım:**
- Ödeme sayfasında "Hediye Kartı / Hediye Çeki" input → kodu gir → bakiye düşülür
- Kalan tutar kredi kartı ile ödenir (kısmi kullanım)
- Bakiye sorgulama: `/hediye-karti-sorgula`

### 24.2 Entity: StorefrontGiftCard
```
TenantId, Code (unique), InitialAmount, RemainingAmount
PurchasedByCustomerId (FK), RecipientEmail?, RecipientPhone?
RecipientName?, SenderMessage?
PurchaseOrderId (FK), ScheduledDeliveryDate?
ExpiresAt (satın alma + 1 yıl varsayılan), Status (enum: Active/Used/Expired/Cancelled)
IsDelivered, DeliveredAt
```

### 24.3 Entity: StorefrontGiftCardTransaction
```
GiftCardId (FK), OrderId (FK?), Amount, TransactionType (enum: Purchase/Use/Refund)
BalanceBefore, BalanceAfter, CreatedAt
```

---

## 25. Sadakat & Puan Programı

### 25.1 Puan Kazanma

| Aksiyon | Puan |
|---------|------|
| Sipariş (her ₺1) | 1 puan (tenant ayarlanabilir) |
| İlk sipariş bonusu | 100 puan |
| Değerlendirme yazma | 50 puan |
| Arkadaşını getir (referans) | 200 puan |
| Doğum günü bonusu | 100 puan |
| Kayıt | 50 puan |

### 25.2 Puan Kullanma

- Ödeme sayfasında "Puanlarınızı kullanın" toggle
- 100 puan = ₺1 (tenant ayarlanabilir oran)
- Minimum kullanım: 500 puan
- Maksimum kullanım: sipariş tutarının %50'si
- Puan + kredi kartı kombine ödeme

### 25.3 Dashboard Ayarları

```
Dashboard → Pazarlama → Sadakat Programı
├── ☑ Aktif / Pasif
├── Kazanma oranı: her [₺1] = [1] puan
├── Kullanma oranı: [100] puan = [₺1]
├── Minimum kullanım: [500] puan
├── Puan son kullanma: [12] ay
├── Bonus aksiyonlar: kayıt [50], değerlendirme [50], referans [200]
└── Müşteri puan raporu: en çok puanlı müşteriler, toplam dağıtılan/kullanılan
```

### 25.4 Entity: StorefrontLoyaltyPoints
```
TenantId, CustomerId (FK), TotalEarned, TotalSpent, CurrentBalance, LastEarnedAt
```

### 25.5 Entity: StorefrontLoyaltyTransaction
```
TenantId, CustomerId (FK), Points (+ kazanım / - harcama)
TransactionType (enum: Purchase/Welcome/Review/Referral/Birthday/Registration/Redemption/Expiry)
ReferenceId? (OrderId, ReviewId vb.), Description, CreatedAt, ExpiresAt
```

---

## 26. Referans Programı (Arkadaşını Getir)

### 26.1 Akış

```
/hesabim → "Arkadaşını Davet Et"
├── Benzersiz referans linki: magaza-ali.com/?ref=ABC123
├── Paylaşma: WhatsApp, email, link kopyala
├── Davetli: linke tıklar → kayıt olur → ilk sipariş verir
├── Davet eden: puan/kupon kazanır
└── Davetli: hoş geldin kuponu kazanır
```

### 26.2 Ödül Yapısı (Tenant Ayarlanabilir)

| Kişi | Ödül |
|------|------|
| Davet eden | 200 sadakat puanı VEYA ₺25 kupon |
| Davetli | İlk siparişte %10 indirim kuponu |

### 26.3 Entity: StorefrontReferral
```
TenantId, ReferrerCustomerId (FK), ReferralCode (unique)
ReferredCustomerId (FK?), ReferredEmail?
Status (enum: Pending/Registered/FirstOrderCompleted/Rewarded)
ReferrerRewardType (enum: Points/Coupon), ReferrerRewardValue
ReferredCouponId (FK?), CompletedAt?
```

---

## 27. Beden Rehberi & Ürün Kılavuzları

### 27.1 Beden Tablosu

```
Ürün detay → Varyant seçimi altında "📏 Beden Rehberi" linki → Modal
```

**Kategori bazlı beden tabloları (dashboard'dan yönetilir):**

```
Dashboard → Ürünler → Beden Rehberleri
├── [+ Yeni Rehber]
│   ├── Rehber adı: "Erkek Tişört Beden Tablosu"
│   ├── Kategoriler: Bu rehberin uygulanacağı kategoriler
│   ├── Ölçüler: satır (beden: S/M/L/XL) × sütun (göğüs/bel/boy cm)
│   ├── Ölçüm talimatları görseli (opsiyonel)
│   └── "Nasıl ölçülür?" metin + görsel
└── Ürüne özel rehber override (opsiyonel)
```

### 27.2 Entity: StorefrontSizeGuide
```
TenantId, Name, CategoryIds (JSON), MeasurementImageUrl?
MeasurementInstructions?, SizeData (JSON — matrix: beden × ölçü)
DisplayOrder, IsActive
```

---

## 28. Hata & Bakım Sayfaları

### 28.1 Hata Sayfaları

| HTTP Kodu | Sayfa | İçerik |
|-----------|-------|--------|
| **404** | `/404` | "Aradığınız sayfa bulunamadı" + arama kutusu + popüler kategoriler + ana sayfa linki |
| **500** | `/500` | "Bir hata oluştu, kısa sürede düzelteceğiz" + ana sayfa linki + iletişim bilgisi |
| **403** | `/403` | "Bu sayfaya erişim izniniz yok" + giriş yap linki |

**Tasarım:** Mağazanın temasıyla uyumlu, logo + header görünür, arama fonksiyonel.

### 28.2 Bakım Modu

```
Dashboard → Ayarlar → Genel → Bakım Modu
├── ☑ Bakım modunu aktifleştir
├── Bakım mesajı: "Sitemizi yeniliyoruz, kısa sürede döneceğiz!"
├── Tahmini süre: "Yaklaşık 2 saat"
├── Countdown timer (opsiyonel)
├── Admin IP bypass: [192.168.1.x] (belirtilen IP'ler siteyi görebilir)
└── Bakım sayfası arka plan görseli (opsiyonel)
```

**Teknik:**
- Tüm sayfalarda 503 döner (SEO zarar görmez — Google 503'ü geçici bilir)
- `/robots.txt` → `Retry-After` header eklenir
- Bakım sayfası statik serve edilir (DB bağlantısı gerekmez)

---

## 29. KVKK Tam Uyum

### 29.1 Veri Dışa Aktarma (Data Portability — KVKK Md.11)

```
/hesabim/guvenlik → "Verilerimi İndir"
→ Talebimi oluştur → 48 saat içinde hazırlanır → email ile indirme linki
```

**Dışa aktarılan veriler (JSON + PDF):**
- Profil bilgileri (ad, email, telefon, adresler)
- Sipariş geçmişi (tüm siparişler, ürünler, tutarlar)
- Değerlendirmeler ve sorular
- Favori ürünler
- Bildirim tercihleri
- Oturum geçmişi (son 10 giriş — IP, tarih, cihaz)

### 29.2 Hesap Silme (Right to Erasure — KVKK Md.11/e)

```
/hesabim/guvenlik → "Hesabımı Sil"
→ Şifre onayı → Sebep (opsiyonel dropdown) → "Kalıcı olarak sil"
→ 14 gün bekleme süresi (geri dönüş imkanı) → email ile geri alma linki
→ 14 gün sonra: soft delete + anonymize
```

**Anonymize kuralları:**
- Ad/soyad → "Silinmiş Kullanıcı"
- Email → `deleted_{timestamp}@anonymized.local`
- Telefon → null
- Adresler → silinir
- Siparişler → korunur (yasal zorunluluk — vergi mevzuatı 5 yıl)
- Değerlendirmeler → anonim ("Anonim Kullanıcı")

### 29.3 Çerez Yönetimi Detayları

```
İlk ziyarette → Çerez onay banner'ı (alt kısımda)
├── "Tümünü Kabul Et" (büyük buton)
├── "Sadece Zorunlu" (küçük link)
├── "Tercihlerimi Ayarla" → modal
│   ├── ☑ Zorunlu çerezler (kapatılamaz)
│   ├── ☐ Analitik çerezler (Google Analytics)
│   ├── ☐ Pazarlama çerezleri (Facebook Pixel, GTM)
│   └── ☐ Kişiselleştirme çerezleri (son görüntülenenler)
└── Seçim localStorage + cookie'de saklanır

/cerez-politikasi sayfasında tercihleri tekrar değiştirebilir
```

### 29.4 Oturum Geçmişi

```
/hesabim/guvenlik → "Oturum Geçmişi"
├── Son 20 giriş: tarih, IP adresi (maskeli: 192.168.x.x), cihaz/tarayıcı, konum (şehir)
├── Aktif oturumlar (birden fazla cihaz)
└── "Tüm oturumları sonlandır" butonu (şifre onayı ile)
```

### 29.5 Entity: StorefrontLoginHistory
```
AuthId (FK → StorefrontCustomerAuth), IpAddress (hashed + encrypted)
UserAgent, DeviceType (enum: Desktop/Mobile/Tablet), City?
LoginAt, IsSuccessful, FailureReason?
```

---

## 30. Kargo API Entegrasyonu

### 30.1 Kargo Sağlayıcı Adapter Pattern

```csharp
public interface ICargoService
{
    Task<IDataResult<ShipmentResult>> CreateShipmentAsync(ShipmentRequest request);
    Task<IDataResult<TrackingResult>> GetTrackingAsync(string trackingNumber);
    Task<IDataResult<decimal>> CalculateShippingCostAsync(ShippingCostRequest request);
    Task<IResult> CancelShipmentAsync(string trackingNumber);
    Task<IDataResult<byte[]>> GetShippingLabelAsync(string trackingNumber); // PDF etiket
}
```

### 30.2 Desteklenen Kargo Firmaları

| Firma | API | Faz |
|-------|-----|-----|
| Yurtiçi Kargo | REST API | Faz 1 |
| Aras Kargo | REST API | Faz 1 |
| Sürat Kargo | REST API | Faz 2 |
| MNG Kargo | REST API | Faz 2 |
| PTT Kargo | REST API | Faz 2 |
| Trendyol Express | REST API | Faz 3 |
| HepsiJet | REST API | Faz 3 |

### 30.3 Dashboard Kargo Ayarları

```
Dashboard → Ayarlar → Kargo
├── Aktif kargo firmaları: ☑ Yurtiçi ☑ Aras ☐ Sürat
├── Her firma için:
│   ├── API credentials (encrypted)
│   ├── Müşteri/Bayi kodu
│   ├── [Bağlantıyı Test Et]
│   ├── Desi/ağırlık hesaplama tercihi
│   └── Ücretlendirme: API'den otomatik / sabit ücret / ücretsiz
├── Varsayılan kargo firması
├── Ücretsiz kargo limiti: [₺500]
├── Otomatik kargo etiketi oluşturma: ☑
└── Otomatik takip no alma: ☑ (sipariş onayında otomatik shipment oluştur)
```

### 30.4 Kargo Takip Otomasyonu

```
Sipariş onayı → CreateShipment (API) → takip no alınır
  → Kargo etiketi PDF oluşturulur (dashboard'dan yazdırılabilir)
  → Müşteriye kargo bildirim emaili + SMS
  → Background job: her 4 saatte GetTracking → durum güncelleme
    → "Teslim edildi" → müşteriye email + sipariş durumu güncelle
```

### 30.5 Entity: StorefrontShipment
```
TenantId, OrderId (FK), CargoProvider (enum: Yurtici/Aras/Surat/Mng/Ptt)
TrackingNumber, ShippingLabelUrl (PDF — MinIO)
Status (enum: Created/PickedUp/InTransit/OutForDelivery/Delivered/Returned)
EstimatedDeliveryDate, ActualDeliveryDate?
ShippingCost, Weight?, Desi?
LastCheckedAt, StatusHistory (JSON — [{status, date, location}])
```

---

## 31. Dashboard Email Ayarları Sayfası

```
Dashboard → Ayarlar → Bildirimler
├── Tab: Email Ayarları
│   ├── Provider: SendGrid / Mailgun / SMTP
│   ├── API Key / SMTP bilgileri
│   ├── Gönderici ad + email
│   ├── [Test Et]
│   └── Günlük limit
├── Tab: SMS Ayarları
│   ├── Provider: Netgsm / İleti Merkezi
│   ├── Credentials
│   ├── Gönderici başlığı
│   └── [Test Et]
├── Tab: Email Şablonları
│   ├── Her template tipi için WYSIWYG editör
│   ├── Değişken listesi
│   ├── Önizleme + test gönder
│   └── Varsayılana sıfırla
├── Tab: Bildirim Kuralları
│   ├── Hangi olayda email gönderilsin
│   ├── Hangi olayda SMS gönderilsin
│   ├── Dashboard'a hangi bildirimler gelsin
│   └── Alıcı email adresleri (mağaza sahibi — birden fazla)
└── Tab: Email Logları
    ├── Gönderilmiş emaillerin listesi: tarih, alıcı, tip, durum
    ├── Başarısız gönderimler (retry bilgisi)
    └── Bounce/spam oranı
```

---

## 32. Yeni Entity'ler (Özet — Bölüm 16-31'de Tanımlanan)

```
StorefrontEmailConfig          → Email provider ayarları (tenant bazlı)
StorefrontSmsConfig            → SMS provider ayarları (tenant bazlı)
StorefrontEmailLog             → Gönderilmiş email kayıtları
StorefrontSmsLog               → Gönderilmiş SMS kayıtları
StorefrontEmailTemplate        → Özelleştirilebilir email şablonları
StorefrontTwoFactorRecoveryCode → 2FA kurtarma kodları
StorefrontAbandonedCartEmail   → Terk edilmiş sepet email takibi
StorefrontInvoice              → Fatura kayıtları
StorefrontCoupon               → Kupon/kampanya tanımları
StorefrontCouponUsage          → Kupon kullanım kayıtları
StorefrontProductQuestion      → Ürün soru-cevap
StorefrontGiftCard             → Hediye kartları
StorefrontGiftCardTransaction  → Hediye kartı hareketleri
StorefrontLoyaltyPoints        → Müşteri puan bakiyesi
StorefrontLoyaltyTransaction   → Puan kazanım/harcama hareketleri
StorefrontReferral             → Referans programı kayıtları
StorefrontSizeGuide            → Beden rehberleri
StorefrontLoginHistory         → Oturum geçmişi
StorefrontShipment             → Kargo gönderim kayıtları
```

**StorefrontCustomerAuth'a eklenen alanlar:**
```
+ ExternalLoginProvider (enum?: Google/Apple/Facebook)
+ ExternalLoginId (string?)
+ PhoneNumber (string?)
+ PhoneNumberConfirmed (bool)
+ PhoneConfirmationOtp (string?)
+ PhoneConfirmationOtpExpiresAt (DateTimeOffset?)
+ TwoFactorEnabled (bool)
+ TwoFactorSecret (string?)
+ BirthDate (DateOnly? — doğum günü kuponu için)
+ ReferralCode (string? — unique per tenant)
+ Gender (enum?: Male/Female/Other/Unspecified — kişiselleştirme için)
```

---

## 33. Güncellenmiş Fazlama

### Faz 1: MVP
- _(mevcut içerik aynen kalır)_
- **+ Email altyapısı:** SendGrid/Mailgun adapter, template engine, zorunlu transactional emailler
- **+ Email doğrulama:** kayıt → doğrulama linki → onay akışı
- **+ Google OAuth 2.0:** sosyal giriş (en yaygın provider)
- **+ Fatura PDF:** sipariş sonrası otomatik fatura oluşturma (QuestPDF)
- **+ 404/500 hata sayfaları** + bakım modu sayfası
- **+ Kupon sistemi (temel):** yüzde / sabit tutar / ücretsiz kargo + dashboard yönetimi
- **+ Kargo API (Yurtiçi + Aras):** otomatik takip no + etiket + durum takibi
- **+ KVKK:** çerez onay yönetimi (zorunlu/analitik/pazarlama ayrımı), hesap silme (anonymize)

### Faz 2: Zenginleştirme
- _(mevcut içerik aynen kalır)_
- **+ SMS altyapısı:** Netgsm/İleti Merkezi adapter, OTP, sipariş bildirimleri
- **+ Telefon doğrulama (OTP)**
- **+ 2FA:** TOTP (Google Authenticator) + SMS + kurtarma kodları
- **+ Terk edilmiş sepet kurtarma:** 3 aşamalı email + opsiyonel kupon
- **+ Sipariş iptali:** müşteri + mağaza tarafı, kısmi iptal
- **+ Ürün soru-cevap** + FAQ schema (AEO)
- **+ Ürün karşılaştırma** (max 4 ürün)
- **+ Beden rehberi** (kategori bazlı beden tablosu)
- **+ Gelişmiş kupon:** X al Y öde, minimum tutar, müşteri segmenti
- **+ Otomatik kampanyalar:** hoş geldin, doğum günü, tekrar alışveriş kuponu
- **+ E-Fatura / E-Arşiv entegrasyonu** (Paraşüt / Logo / Foriba)
- **+ Oturum geçmişi** + aktif oturum yönetimi
- **+ KVKK veri dışa aktarma** (JSON + PDF)
- **+ Ek kargo firmaları:** Sürat, MNG, PTT

### Faz 3: İleri
- _(mevcut içerik aynen kalır)_
- **+ Apple Sign In + Facebook Login**
- **+ Hediye kartı sistemi** (satın al, gönder, kullan)
- **+ Sadakat puan programı** (kazanım + harcama + seviyelendirme)
- **+ Referans programı** (arkadaşını getir)
- **+ Web Push bildirimleri** (Service Worker)
- **+ Email pazarlama:** kampanya oluşturma, segmentasyon, A/B test
- **+ Ek kargo:** Trendyol Express, HepsiJet

---

## 34. Doğrulama

Her faz:
1. `dotnet build` — 0 error
2. `dotnet test` — yeşil
3. Lighthouse: SEO/Performance/Accessibility ≥ 90
4. Schema.org validator: structured data doğru
5. Google Search Console: sitemap + indexing
6. Core Web Vitals: LCP < 2.5s
7. OWASP ZAP: güvenlik taraması
8. Yük testi: 100 concurrent, < 500ms response
9. Email deliverability test: SPF/DKIM/DMARC doğrulama
10. KVKK uyum checklist: çerez onayı, aydınlatma metni, veri silme/export
11. PCI DSS compliance check: kart bilgisi sunucuya gelmiyor mu?
12. 2FA test: TOTP + SMS + kurtarma kodu akışları
13. Accessibility (a11y): WCAG 2.1 AA uyumluluğu
14. Sepet birleştirme testi: misafir → üye geçişi

---

## 35. Tekrar Sipariş Ver (Buy Again)

### 35.1 Müşteri Tarafı

**Amazon'un en güçlü özelliği.** Sık alışveriş yapan müşteriler aynı ürünleri defalarca alır.

```
/hesabim/Siparişlerim → her siparişte [🔄 Tekrar Sipariş Ver] butonu
  → Siparişteki tüm ürünleri (mevcut fiyat + stokla) sepete ekler
  → Stokta olmayanlar: uyarı gösterilir, eklenmez

/hesabim/tekrar-satin-al → özel sayfa
  → Daha önce satın alınan tüm ürünler (unique, son sipariş tarihine göre sıralı)
  → Her üründe: [Sepete Ekle] + son alım tarihi + kaç kez alındığını göster
  → Filtre: kategori, tarih aralığı
```

**Hesap paneli sidebar:** "Tekrar Satın Al" linki (siparişlerimden sonra)

### 35.2 Ana Sayfada "Tekrar Al" Vitrini

Giriş yapmış müşteriye ana sayfada:
```
"Tekrar almak isteyebileceğiniz ürünler"
→ Daha önce aldığı ürünlerden → stokta olanlar → son alımdan en uzun süre geçenler önce
```

---

## 36. Sepette Sakla / Sonra Al (Save for Later)

### 36.1 Akış

```
Sepet sayfasında her ürünün altında:
├── [🗑 Sil]
└── [🔖 Sonra Al]

"Sonra Al" tıklanınca:
  → Ürün sepetten çıkar
  → "Sonra almak istedikleriniz" bölümüne taşınır (sepet sayfasının altında)
  → Bu bölümde: ürün kartı + [Sepete Taşı] + [Sil] butonları
  → Fiyat değişirse: eski fiyat üstü çizili → yeni fiyat gösterilir
```

**Neden önemli:** Müşteri "almak istiyorum ama bu ay bütçem yok" dediğinde sepeti boşaltmak yerine saklayabilir. Sepet toplamı da düşer, checkout'a yaklaştırır.

### 36.2 Entity: StorefrontSavedCartItem
```
TenantId, CustomerId (FK), ProductVariantId (FK)
OriginalPrice (saklandığı andaki fiyat — fiyat değişim karşılaştırması için)
SavedAt
```

---

## 37. Fotoğraflı & Videolu Değerlendirme

### 37.1 Yorum Yazma (Güncellenmiş)

Mevcut spec'te değerlendirme var ama sadece yıldız + metin. Gerçek dünyada fotoğraf/video **satın alma kararının #1 etkeni**.

```
Ürün Detay → Değerlendirmeler → [Yorum Yaz]
├── Yıldız (1-5, zorunlu)
├── Başlık (opsiyonel)
├── Yorum metni (zorunlu, min 20 karakter)
├── 📸 Fotoğraf ekle (max 5 fotoğraf, her biri max 5MB, JPEG/PNG/WebP)
├── 🎥 Video ekle (max 1 video, max 30MB, MP4, 60 sn limit)
├── Beden/ölçü uyumu: Küçük kalıyor / Tam ölçü / Büyük kalıyor (giyim kategorisi)
├── "Bu ürünü tavsiye ediyor musunuz?" Evet / Hayır
└── [Yorumu Gönder]
```

### 37.2 Değerlendirme Filtreleme & Sıralama

```
Değerlendirmeler tab'ında:
├── Özet bar: ★★★★☆ 4.3 (128 değerlendirme)
│   ├── ★5 ████████████ 72
│   ├── ★4 ██████ 34
│   ├── ★3 ██ 12
│   ├── ★2 █ 6
│   └── ★1 █ 4
├── Filtre butonları:
│   ├── [Tümü] [5★] [4★] [3★] [2★] [1★]
│   ├── [📸 Fotoğraflı] [🎥 Videolu]
│   ├── [✓ Doğrulanmış Alım]
│   └── [Beden uyumu: Küçük / Tam / Büyük]
├── Sıralama: En yeni / En faydalı / En yüksek puan / En düşük puan
├── Her yorumda:
│   ├── "Faydalı buldum" 👍 (42) / 👎 (3)
│   ├── Fotoğraf galerisi (tıkla → lightbox)
│   └── Mağaza yanıtı (varsa)
└── "Tüm fotoğrafları gör" → tüm müşteri fotoğrafları grid
```

### 37.3 StorefrontReview Güncelleme
```
Mevcut StorefrontReview entity'sine ekle:
+ ImageUrls (JSON — max 5 URL, MinIO'da saklanır)
+ VideoUrl (string? — MinIO'da saklanır)
+ SizeFit (enum?: TooSmall/TrueToSize/TooLarge — giyim kategorisi)
+ IsRecommended (bool?)
+ HelpfulCount (int)
+ NotHelpfulCount (int)
```

---

## 38. Birlikte Sıkça Alınan Ürünler (Frequently Bought Together)

### 38.1 Ürün Detay Sayfasında

```
Ürün detay → "Birlikte sıkça alınan ürünler" bölümü

┌─────────────────────────────────────────────────────────────┐
│  Birlikte sıkça alınanlar                                    │
│                                                               │
│  [Ürün A ✓]  +  [Ürün B ✓]  +  [Ürün C ☐]                 │
│   ₺299,90        ₺149,90        ₺89,90                      │
│                                                               │
│  Toplam: ₺449,80 (3'ünü al: ₺429,90 — %4 tasarruf)        │
│                                     [🛒 Seçilenleri Sepete Ekle] │
└─────────────────────────────────────────────────────────────┘
```

### 38.2 Veri Kaynağı

**Otomatik (algoritma):** Aynı sepette/siparişte birlikte en çok bulunan ürünler
**Manuel override (dashboard):** Mağaza sahibi ürün eşleştirmelerini elle belirleyebilir

```
Dashboard → Ürünler → Ürün Detay → "İlişkili Ürünler" tab
├── Otomatik öneriler (algoritma bazlı — düzenlenemez, sadece göster/gizle)
├── Manuel eşleştirme: ürün ara → ekle
├── "Bundle indirim" opsiyonu: birlikte alınca %X indirim
└── Sıralama: drag & drop
```

### 38.3 Entity: StorefrontProductBundle
```
TenantId, PrimaryProductId (FK), BundledProductId (FK)
Source (enum: Algorithm/Manual), BundleDiscountPercentage? (opsiyonel)
DisplayOrder, IsActive, PurchaseCount (algoritma için)
```

---

## 39. Kayıtlı Kartlar (Tokenized Payment)

### 39.1 Müşteri Tarafı

```
Ödeme sayfasında:
├── "Kayıtlı kartlarım" (daha önce kaydedilen kartlar)
│   ├── **** **** **** 4532 (Visa, Garanti) [Varsayılan ✓] [Sil]
│   ├── **** **** **** 7891 (MC, İş Bankası) [Sil]
│   └── [+ Yeni kart ekle]
├── Yeni kart girerken: ☑ "Bu kartı kaydet" checkbox
└── Kayıtlı kart ile ödeme → CVV tekrar girilir (güvenlik)

/hesabim/kartlarim
├── Kayıtlı kartlar listesi
├── Varsayılan kart seç
├── Kart sil
└── (Kart ekleme sadece ödeme sırasında — güvenlik)
```

### 39.2 Teknik

- **Kart bilgisi bizde saklanmaz** — iyzico/PayTR token döner, biz sadece token saklarız
- PCI DSS Level 4 uyumu (SAQ A — tüm kart işlemi provider'da)
- Token ile ödeme: provider'a token + CVV gönderilir

### 39.3 Entity: StorefrontSavedCard
```
TenantId, CustomerId (FK)
CardToken (provider'dan dönen token), CardAssociation (enum: Visa/MC/Troy/Amex)
BankName, LastFourDigits, CardHolderName (maskeli)
IsDefault, PaymentProvider (enum: Iyzico/PayTR)
CreatedAt
```

---

## 40. Dijital Cüzdan & Bakiye (Customer Wallet)

### 40.1 Neden Gerekli?

İade yapıldığında müşteriye 3 seçenek sunulur:
1. Kredi kartına iade (3-14 gün banka süreci)
2. **Cüzdana iade (anında)** ← müşteri bunu tercih eder çünkü hızlı
3. Havale ile iade

Cüzdan bakiyesi = hediye kartı bakiyesi + iade bakiyesi + promosyon bakiyesi → tek hesap.

### 40.2 Müşteri Tarafı

```
/hesabim/cuzdanim
├── Bakiye: ₺142,50
├── Hareket geçmişi:
│   ├── +₺99,90  İade (Sipariş #1234)     12.03.2026
│   ├── -₺50,00  Sipariş #1301            15.03.2026
│   ├── +₺50,00  Hediye kartı (ABC123)    18.03.2026
│   └── +₺42,60  İade (Sipariş #1289)     20.03.2026
└── Ödeme sayfasında: "Cüzdan bakiyenizi kullanın (₺142,50)" toggle
    → Kalan tutar kredi kartı / kapıda ile ödenir
```

### 40.3 Entity: StorefrontWallet
```
TenantId, CustomerId (FK, unique), Balance
```

### 40.4 Entity: StorefrontWalletTransaction
```
WalletId (FK), Amount (+ kredi / - kullanım)
TransactionType (enum: Refund/GiftCard/Promotion/LoyaltyRedemption/OrderPayment)
ReferenceId? (OrderId, GiftCardId, vb.), Description
BalanceBefore, BalanceAfter, CreatedAt
```

---

## 41. Sipariş Notu & Hediye Paketi

### 41.1 Sipariş Notu

```
Ödeme sayfasında → Teslimat bilgilerinin altında:
├── "Sipariş notu ekle (opsiyonel)" → textarea
│   → "Kapıcıya bırakın", "Zile basmayın", "Saat 18'den sonra gelin"
└── Kargo paketi içine not kağıdı konmaz — sadece iç iletişim
```

### 41.2 Hediye Paketi

```
Sepet sayfasında veya ödeme adımında:
├── ☑ "Hediye paketi istiyorum" (+ ₺X,XX ek ücret)
├── ☑ "Fiyat etiketini çıkarmayın / Fatura koymayın" (hediye gönderimlerinde)
└── Hediye mesajı: "Doğum günün kutlu olsun!" (max 200 karakter, kart olarak pakete eklenir)
```

### 41.3 Dashboard Ayarları

```
Dashboard → Ayarlar → Sipariş
├── ☑ Sipariş notu aktif
├── ☑ Hediye paketi aktif
├── Hediye paketi ücreti: [₺14,90]
└── Hediye mesajı aktif: ☑
```

### 41.4 Order Entity'sine Eklenen Alanlar
```
+ OrderNote (string?)
+ IsGiftWrapped (bool)
+ GiftWrappingFee (decimal?)
+ GiftMessage (string?)
+ HideInvoice (bool — hediye gönderimde fatura koymama)
```

---

## 42. Ön Sipariş (Pre-order)

### 42.1 Akış

Stokta olmayan ama yakında gelecek ürünlerde:

```
Ürün detay sayfasında:
├── Stok durumu: "Ön sipariş — Tahmini teslimat: 15 Nisan 2026"
├── [🛒 Ön Sipariş Ver] butonu (normal "Sepete Ekle" yerine)
├── Ödeme alınır (tam veya kısmi — tenant ayarı)
├── Ürün stoğa gelince:
│   → Email: "Ön siparişiniz hazırlanıyor!"
│   → Otomatik kargo süreci başlar
└── Müşteri kargoya verilmeden önce iptal edebilir (tam iade)
```

### 42.2 Dashboard

```
Dashboard → Ürünler → Ürün Düzenle → "Ön Sipariş" tab
├── ☑ Ön sipariş aktif
├── Tahmini stok tarihi: [15.04.2026]
├── Ön sipariş limiti: [100 adet] (0 = limitsiz)
├── Ön sipariş ödeme: ○ Tam ödeme  ○ Kapora (%25)
└── Mevcut ön sipariş sayısı: 34/100
```

### 42.3 Product Entity'sine Eklenen Alanlar
```
+ IsPreOrder (bool)
+ PreOrderEstimatedDate (DateOnly?)
+ PreOrderLimit (int? — 0 = limitsiz)
+ PreOrderPaymentType (enum?: Full/Deposit)
+ PreOrderDepositPercentage (int? — varsayılan 25)
```

---

## 43. Misafir Sepet → Üye Sepet Birleştirme (Cart Merge)

### 43.1 Senaryo

Kullanıcı giriş yapmadan sepete ürün ekler (session-based cart), sonra giriş yapar veya kayıt olur.

```
Misafir sepet: [Ürün A x2, Ürün B x1]
Üye sepet (önceki oturum): [Ürün B x1, Ürün C x3]

Giriş sonrası birleştirme:
→ Birleşik sepet: [Ürün A x2, Ürün B x1 (max miktar), Ürün C x3]
→ Aynı ürün varsa: misafir sepetteki miktar korunur (güncel niyet)
→ Üye sepetindeki farklı ürünler eklenir
→ Misafir session temizlenir
```

### 43.2 Teknik
- `CartMergeService.MergeAsync(sessionCart, customerCart)` → birleştirme stratejisi
- Çakışma kuralı: aynı variant → misafir sepetteki miktar kazanır (son niyet)
- Stok kontrolü: birleştirme sırasında stok aşılıyorsa max stoğa indirilir + uyarı

---

## 44. Hızlı Satın Al (Express Checkout)

### 44.1 Akış

Kayıtlı adresi ve kayıtlı kartı olan müşteriler için:

```
Ürün detay → [⚡ Hızlı Satın Al] butonu ("Sepete Ekle"nin yanında)
  → Varsayılan adres + varsayılan kart ile doğrudan ödeme modal'ı
  → Modal:
    ├── Ürün özeti (ad, varyant, miktar, fiyat)
    ├── Teslimat: Ev adresi — Fatih, İstanbul [Değiştir]
    ├── Ödeme: **** 4532 Visa [Değiştir]
    ├── Toplam: ₺299,90 + ₺0 kargo = ₺299,90
    └── [Siparişi Onayla] → 3D Secure → tamamla
```

**Koşullar:**
- Sadece giriş yapmış müşterilere gösterilir
- En az 1 kayıtlı adres + 1 kayıtlı kart olmalı
- Çok varyantlı ürünlerde varyant seçildikten sonra görünür
- Dashboard'da aktif/pasif toggle

---

## 45. Adres Otomatik Tamamlama

### 45.1 Akış

Adres formlarında (kayıt, ödeme, adres ekleme):

```
İl: [İstanbul ▼] → İlçe otomatik filtre: [Kadıköy ▼]
  → Mahalle otomatik filtre: [Caferağa ▼]
  → Sokak: autocomplete (yazarken öneri)
  → Posta kodu: otomatik doldur (mahalle seçimine göre)
```

### 45.2 Veri Kaynağı
- **İl/İlçe/Mahalle:** PTT adres veritabanı (statik seed data, periyodik güncelleme)
- **Posta kodu:** Mahalle bazlı otomatik eşleştirme
- **Harita:** Google Maps embed (iletişim sayfası için, adres formu için opsiyonel)

### 45.3 Entity: AddressHierarchy (Seed Data)
```
Province: Id, Name, PlateCode (01-81)
District: Id, ProvinceId (FK), Name
Neighborhood: Id, DistrictId (FK), Name, PostalCode
```

---

## 46. İade Kargo Etiketi & İade Akış Detayları

### 46.1 Güncellenmiş İade Akışı

```
/hesabim/iade-talebi → Sipariş seç → Ürün seç → Sebep + açıklama + fotoğraf
  → Talep gönderildi
  → Dashboard'da mağaza onaylar/reddeder
  → Onaylandıysa:
    ├── Müşteriye email: iade talimatları + kargo kodu
    ├── İade kargo etiketi PDF (otomatik oluşturulur — kargo API)
    ├── Müşteri paketi kargo şubesine bırakır (etiket hazır)
    ├── Ürün mağazaya ulaşır → mağaza kontrol eder
    └── İade tamamlandı:
        ├── ○ Kredi kartına iade (3-14 gün)
        ├── ○ Cüzdana iade (anında) ← varsayılan öneri
        └── ○ Değişim (aynı üründen farklı beden/renk)
```

### 46.2 İade Sebepleri (Dropdown)

| Sebep | Açıklama |
|-------|----------|
| Ürün kusurlu/hasarlı geldi | Zorunlu fotoğraf |
| Yanlış ürün gönderildi | Zorunlu fotoğraf |
| Beden/ölçü uymuyor | |
| Ürün açıklamasıyla uyuşmuyor | |
| Beğenmedim/vazgeçtim | |
| Diğer | Açıklama zorunlu |

### 46.3 StorefrontReturnRequest Güncelleme
```
Mevcut entity'ye ekle:
+ ReturnShippingLabel (string? — PDF URL, MinIO)
+ ReturnTrackingNumber (string?)
+ ReturnCargoProvider (enum?)
+ RefundDestination (enum: CreditCard/Wallet/BankTransfer/Exchange)
+ ExchangeProductVariantId (int? — değişim ürünü)
+ ExchangeOrderId (int? — değişim siparişi)
```

---

## 47. Ürün Rozeti / Badge Sistemi

### 47.1 Otomatik Rozetler

Ürün kartlarında ve detay sayfasında gösterilen görsel badge'ler:

| Rozet | Koşul | Renk |
|-------|-------|------|
| **YENİ** | Son 7 günde eklenen | Yeşil |
| **ÇOK SATAN** | Son 30 günde en çok satılan top %10 | Turuncu |
| **SON STOK** | Stok ≤ 5 | Kırmızı |
| **İNDİRİMLİ** | Aktif indirim var | Kırmızı |
| **ÜCRETSİZ KARGO** | Ücretsiz kargo ürünü | Mavi |
| **ÖN SİPARİŞ** | Pre-order aktif | Mor |

### 47.2 Manuel Rozetler (Dashboard)

```
Dashboard → Ürünler → Ürün Düzenle → "Rozetler" bölümü
├── Otomatik rozetler: göster/gizle toggle
├── Manuel rozet ekle:
│   ├── Rozet metni: "Editörün Seçimi"
│   ├── Rozet rengi: [color picker]
│   ├── Başlangıç - bitiş tarihi
│   └── Pozisyon: Sol üst / Sağ üst
```

### 47.3 Entity: StorefrontProductBadge
```
TenantId, ProductId (FK), BadgeText, BadgeColor, BadgeType (enum: Auto/Manual)
Position (enum: TopLeft/TopRight), StartDate?, EndDate?, IsActive
```

---

## 48. Çoklu Favori Listesi & Paylaşma

### 48.1 Çoklu Liste

Tek bir "Favoriler" yerine, müşteri birden fazla liste oluşturabilir:

```
/hesabim/favorilerim
├── "Favorilerim" (varsayılan — silinemez)
├── "Doğum Günü Fikirleri" (müşteri oluşturdu)
├── "Yaz Koleksiyonu" (müşteri oluşturdu)
└── [+ Yeni Liste Oluştur]

Ürün detayda ♡ tıklayınca:
→ Tek liste varsa: direkt ekle
→ Birden fazla: liste seçim dropdown'ı göster
```

### 48.2 Favori Listesi Paylaşma

```
Liste detay sayfasında → [🔗 Paylaş] butonu
├── Link oluştur: magaza-ali.com/liste/ABC123 (public link)
├── WhatsApp, email, kopyala
└── Paylaşılan liste: read-only, "Sepete Ekle" butonlu (giriş gerektirmez)
```

**Kullanım:** Doğum günü dilek listesi, düğün listesi, hediye önerileri.

### 48.3 StorefrontWishlist Güncelleme
```
Mevcut StorefrontWishlist yerine:

StorefrontWishlistCollection:
  TenantId, CustomerId (FK), Name, ShareToken (unique, nullable)
  IsDefault (bool), IsPublic (bool), CreatedAt

StorefrontWishlistItem:
  CollectionId (FK), ProductVariantId (FK), AddedAt, Note? (opsiyonel kısa not)
```

---

## 49. Erişilebilirlik (Accessibility — WCAG 2.1 AA)

### 49.1 Zorunlu Standartlar

| Kriter | Uygulama |
|--------|----------|
| **Klavye navigasyonu** | Tüm etkileşimli öğeler Tab ile erişilebilir, Enter/Space ile aktive edilir |
| **Screen reader** | Tüm görsellerde anlamlı `alt` text, ARIA label'lar, semantic HTML |
| **Renk kontrastı** | Metin/arka plan minimum 4.5:1 kontrast oranı |
| **Focus göstergesi** | Klavye focus'unda belirgin `outline` (gizleme YOK) |
| **Form label** | Her input'un `label` etiketi (placeholder ≠ label) |
| **Hata mesajları** | `aria-live="polite"` ile screen reader'a duyurulur |
| **Skip navigation** | Sayfa başında "İçeriğe geç" gizli link |
| **Responsive text** | `rem`/`em` birim, 200% zoom'da düzgün görünüm |
| **Video alt yazı** | Ürün videoları için closed captions (opsiyonel) |
| **Hareket azaltma** | `prefers-reduced-motion` media query — animasyonları durdur |

### 49.2 Test
- axe DevTools ile otomatik tarama (CI/CD'de)
- Manuel screen reader testi (NVDA / VoiceOver)
- Lighthouse Accessibility skoru ≥ 95

---

## 50. Teslimat Zaman Dilimi Seçimi

### 50.1 Müşteri Tarafı

```
Ödeme → Kargo Seçimi adımında:
├── Standart Teslimat (2-3 iş günü) — ₺0
├── Hızlı Teslimat (ertesi gün) — ₺29,90 (varsa)
└── Teslimat zaman dilimi (opsiyonel — kargo firması destekliyorsa):
    ├── ○ 09:00 - 12:00
    ├── ○ 12:00 - 17:00
    ├── ○ 17:00 - 21:00
    └── ○ Fark etmez (varsayılan)
```

### 50.2 Dashboard Ayarları

```
Dashboard → Ayarlar → Kargo → Teslimat Seçenekleri
├── ☑ Hızlı teslimat aktif → ek ücret: [₺29,90]
├── ☑ Zaman dilimi seçimi aktif
├── Zaman dilimleri: [düzenle] (09-12, 12-17, 17-21)
└── Hangi illerde zaman dilimi aktif: [İstanbul, Ankara, İzmir]
```

---

## 51. Arama Geliştirmeleri

### 51.1 Arama Geçmişi

```
Arama kutusuna tıklayınca (boş iken):
├── "Son aramalarınız" (giriş yapmışsa DB, misafirse localStorage)
│   ├── siyah tişört [✕]
│   ├── nike ayakkabı [✕]
│   └── [Geçmişi Temizle]
├── "Popüler aramalar" (tüm müşterilerden top 10)
│   ├── 🔥 yazlık elbise
│   ├── 🔥 spor ayakkabı
│   └── ...
└── "Önerilen kategoriler" (en çok ziyaret edilen)
```

### 51.2 Arama Önerileri (Autocomplete Geliştirme)

```
Kullanıcı "si" yazdığında:
├── Ürünler: [img] Siyah Basic Tişört — ₺299,90
│             [img] Siyah Slim Jean — ₺449,90
├── Kategoriler: Siyah Giyim (24 ürün)
├── Markalar: Simoni (12 ürün)
└── "siyah" ile ara → (tüm sonuçlar)
```

### 51.3 "Bunu mu demek istediniz?" (Typo Correction)

```
Kullanıcı "siya tişrt" araması yaptığında:
→ "Bunu mu demek istediniz: siyah tişört?" linki (Levenshtein distance)
→ Sonuç bulunamazsa otomatik düzeltme uygulanır
```

### 51.4 Entity: StorefrontSearchHistory
```
TenantId, CustomerId (FK?), Query, ResultCount, SearchedAt
```

### 51.5 Entity: StorefrontPopularSearch
```
TenantId, Query (normalized), SearchCount, LastSearchedAt, IsPromoted (bool — manuel öne çıkarma)
```

---

## 52. Yeni Entity'ler (Bölüm 35-51 Özeti)

```
StorefrontSavedCartItem         → "Sonra al" saklanan ürünler
StorefrontProductBundle         → Birlikte sıkça alınan ürün eşleştirmeleri
StorefrontSavedCard             → Tokenized kayıtlı kartlar
StorefrontWallet                → Müşteri dijital cüzdan bakiyesi
StorefrontWalletTransaction     → Cüzdan hareket geçmişi
StorefrontProductBadge          → Ürün rozet/badge'leri
StorefrontWishlistCollection    → Çoklu favori listeleri (mevcut Wishlist'in yerine)
StorefrontWishlistItem          → Favori listesi öğeleri
StorefrontSearchHistory         → Arama geçmişi
StorefrontPopularSearch         → Popüler aramalar
AddressHierarchy (Province/District/Neighborhood) → İl/ilçe/mahalle seed data
```

**Mevcut entity güncellemeleri:**
```
StorefrontReview  → + ImageUrls, VideoUrl, SizeFit, IsRecommended, HelpfulCount, NotHelpfulCount
StorefrontReturnRequest → + ReturnShippingLabel, ReturnTrackingNumber, RefundDestination, Exchange*
Product → + IsPreOrder, PreOrderEstimatedDate, PreOrderLimit, PreOrderPaymentType, PreOrderDepositPercentage
Order → + OrderNote, IsGiftWrapped, GiftWrappingFee, GiftMessage, HideInvoice
```

---

## 53. Güncellenmiş Fazlama (Final)

### Faz 1: MVP
- _(Bölüm 33'teki mevcut içerik)_
- **+ Misafir → üye sepet birleştirme** (cart merge)
- **+ Adres otomatik tamamlama** (il/ilçe/mahalle/posta kodu seed data)
- **+ Otomatik ürün rozetleri** (yeni, çok satan, son stok, indirimli)
- **+ 404/500 hata sayfaları** (tema uyumlu)
- **+ Erişilebilirlik** (WCAG 2.1 AA temel uyum — klavye, kontrast, semantic HTML)

### Faz 2: Zenginleştirme
- _(Bölüm 33'teki mevcut içerik)_
- **+ Fotoğraflı/videolu değerlendirme** + filtreleme (puan, fotoğraflı, doğrulanmış)
- **+ Tekrar sipariş ver** (buy again) + ana sayfada "tekrar al" vitrini
- **+ Sepette sakla / sonra al** (save for later)
- **+ Kayıtlı kartlar** (tokenized — iyzico/PayTR)
- **+ Dijital cüzdan** (iade → cüzdana anında iade)
- **+ Sipariş notu + hediye paketi** + hediye mesajı
- **+ İade kargo etiketi** (otomatik PDF) + iade → cüzdan/değişim seçeneği
- **+ Birlikte sıkça alınanlar** (otomatik algoritma + manuel override)
- **+ Çoklu favori listesi** + liste paylaşma (doğum günü listesi vb.)
- **+ Arama geçmişi** + popüler aramalar + yazım düzeltme
- **+ Manuel ürün rozetleri** (editörün seçimi, kampanya badge)

### Faz 3: İleri
- _(Bölüm 33'teki mevcut içerik)_
- **+ Hızlı satın al** (express checkout — tek tıkla ödeme)
- **+ Ön sipariş** (pre-order — kapora/tam ödeme)
- **+ Teslimat zaman dilimi** seçimi (saat aralığı)
- **+ Hediye kartı** (Bölüm 24) + **sadakat programı** (Bölüm 25) + **referans** (Bölüm 26)
