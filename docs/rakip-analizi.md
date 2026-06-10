# Rakip Analizi: E-Ticaret Entegrasyon Yazilimlari

**Tarih:** Mart 2026 · **Güncelleme:** 2026-06-10 (PA kod-önce doğrulama)
**Amac:** Turkiye ve yurtdisi e-ticaret entegrasyon yazilimlarinin karsilastirmali analizi, rekabet avantaji tespiti ve oncelikli gelistirme onerileri.

---

## ⚠️ 2026-06-10 GÜNCELLEME — Kod-Önce Doğrulama (PA)

> Bu dokümanın §4 "Bizde Olmayan Özellikler" listesi **kısmen BAYAT** çıktı. Doküman projeyi "Blazor Server" sanıyor — **proje ASP.NET Core MVC'ye geçti.** Aşağıdaki teyit KODA dayanır (grep + dosya:satır), dokümana değil. §4 okunurken bu tabloyu esas al:

| Rakip-eksik iddiası (§4) | KOD-ÖNCE GERÇEK DURUM (2026-06-10) | Task |
|---|---|---|
| Rekabet analizi / rakip fiyat takibi (repricing) | ❌ **GERÇEKTEN YOK** (grep boş) — rakiplerin en güçlü silahı | C1 [HIGH] |
| Akıllı/dinamik fiyatlandırma | ⚠️ **KISMEN VAR** — `PricingRuleManager` (kategori/brand scope, %/sabit oran kuralı) + `CommissionCalculator` var; AMA maliyet/marj-bazlı otomatik + platform-strateji YOK | C2 [MEDIUM] |
| E-Fatura / E-Arşiv | ⚠️ **KISMEN VAR** — Trendyol e-fatura API+polling çalışıyor; genel GİB gönderimi placeholder, otomatik tetik yok (bkz. Konu 3: F1/F3) | F1/F3 |
| Depo yönetimi (temel) | ✅ **VAR** — BranchOffice tam CRUD + stok devri + transfer (tasks.json DONE). WMS/barkodlu-toplama ayrı eksik | — |
| Raporlama / analitik dashboard | ✅ **GENİŞ VAR** — `ReportManager`: Sales/Inventory/ProfitLoss/Marketplace/Performance + Dashboard. Doküman "yok" diyor, YANLIŞ | — |
| Otomatik sipariş yönlendirme (order routing) | ⚠️ **HARDCODED** — checkout `branchOfficeId:1` sabit, çoklu-depo seçimi yok (bkz. Konu 1: S2) | S2 |
| Toplu işlem (bulk) | ⚠️ **KISMEN VAR** — Export + BulkOperationManager var; toplu fatura/etiket yok (F6) | F6 |
| İade yönetimi | ⚠️ **KISMEN VAR** — SaleReturn + Pazarama refund polling var; iade faturası yok (F4) | F4 |
| POS / omnichannel | ✅ **VAR** — SaleManager (POS), stok event'iyle omnichannel sync (Konu 1) | — |
| Set/Paket (Bundle/Kit) | ❌ **GERÇEKTEN YOK** (grep boş) | C3 [MEDIUM] |
| Satın Alma (Purchase Order) | ❌ **GERÇEKTEN YOK** (grep boş) — tedarikçi/min-stok uyarı yok | C4 [MEDIUM] |
| AI içerik üretimi | ❌ **YOK** — Ollama sadece attribute/kategori eşleştirme + AiCredit accounting; ürün açıklaması/görsel AI üretimi yok | C5 [LOW] |
| B2B portal / toptan | ❌ **GERÇEKTEN YOK** (grep boş) | C6 [LOW] |
| Marketplace reklam yönetimi | ❌ **YOK** — sadece Storefront email kampanya (`StorefrontCampaignManager`); pazaryeri reklam yok | C7 [LOW] |

**Özet:** Gerçek rakip boşlukları (bizde yok, rakipte var, değerli): **repricing (C1, en kritik)**, maliyet/marj-bazlı fiyatlandırma (C2), bundle/kit (C3), purchase-order (C4), AI içerik (C5), B2B (C6), pazaryeri reklam (C7). Diğer "eksik" sanılanlar (e-fatura, depo, rapor, bulk, iade, POS) kısmen/tamamen VAR — ilgili boşluklar S/F serisi task'larında. Repricing **rakiplerin (Entegra/Dopigo) en güçlü satış argümanı** — en yüksek öncelikli rekabet açığımız.

---

## Icerik

1. [Turkiye Pazari](#1-turkiye-pazari)
2. [Yurtdisi Pazari](#2-yurtdisi-pazari)
3. [Karsilastirma Tablosu](#3-karsilastirma-tablosu)
4. [Bizde Olmayan Ozellikler](#4-bizde-olmayan-ozellikler)
5. [Yurtdisindan Getirilebilecek Yenilikler](#5-yurtdisindan-getirilebilecek-yenilikler)
6. [Rekabet Avantaji Onerileri](#6-rekabet-avantaji-onerileri)
7. [Oncelikli Gelistirme Onerileri](#7-oncelikli-gelistirme-onerileri)

---

## 1. Turkiye Pazari

### 1.1 Ticimax

**Web:** ticimax.com
**Kurulis:** 2013 | **Kullanici:** 30.000+ marka
**Tip:** E-ticaret altyapisi + pazaryeri entegrasyonu (SaaS)

#### Temel Ozellikler
- E-ticaret sitesi altyapisi (tasarim, tema, SEO)
- Pazaryeri entegrasyonu (paket bazinda sinirli veya sinirsiz)
- B2B satis modulu (Advance ve ustu)
- Mobil uygulama (iOS/Android hazir)
- E-Fatura ve E-Arsiv entegrasyonu
- Yapay zeka ozellikleri (Ticimax AI): logo olusturucu, icerik asistani, banner tasarim, urun gorseli olusturma
- Sepet hatirlatma, stok/fiyat dusus bildirimleri
- Excel ile toplu urun yukleme
- 7/24 destek

#### Pazaryeri Entegrasyonlari
- Advantage: Sadece Trendyol
- Advance: 3 pazaryeri
- Advance Plus: Sinirsiz yurtici + 3 yurtdisi pazaryeri
- Desteklenen: Trendyol, Hepsiburada, N11, Amazon, Ciceksepeti, PttAVM, Pazarama, Etsy, eBay vb.

#### Fiyatlandirma (2026)
| Paket | Aylik | Yillik |
|-------|-------|--------|
| Advantage | 3.250 TL | 39.000 TL |
| Advance | 4.991 TL | 59.900 TL |
| Advance Plus | 6.583 TL | 79.000 TL |

- Odeme: Havale veya 12 taksit kredi karti
- Kargo bakiyesi hediyesi (25.000-40.000 TL arasi)

#### Kullanici Yorumlari ve Sikayetler
- **Olumlu:** Genis ozellik seti, AI ozellikleri, kargo anlasmasi avantajlari
- **Olumsuz:** Destek taleplerine gec donus, kurulum surecinde sorunlar, altyapi gecislerinde uzun sureli problemler (25+ gun panel erisim sorunu gibi), lisans Bitiş tarihi bilgi eksikligi, satis sonrasi destek kalitesinde dusus
- **Sikayetvar'da aktif:** 10+ yildir pro uye, sikayetlere donus yapiyor

#### Benzersiz Ozellikler
- Ticimax AI (icerik + gorsel uretimi)
- Kargo bakiyesi hediyesi (pazarda nadir)
- E-ihracat paketleri (yurtdisi odakli ozel paketler)
- Akinsoft ERP entegrasyonu

---

### 1.2 Entegra Bilisim

**Web:** entegrabilisim.com
**Kurulis:** 2014 | **Kullanici:** 8.000+ aktif
**Tip:** Pazaryeri entegrasyon platformu (SaaS)

#### Temel Ozellikler
- **Pazaryeri entegrasyonu** (tum Turk pazaryerleri dahil, ek ucret yok)
- **Rekabet Analizi:** Rakip fiyatlarini otomatik takip, belirlenen sinirlar icerisinde fiyat guncelleme (Trendyol, Hepsiburada, N11, Amazon)
- **Akilli Fiyatlandirma (Smart Price):** Degisen maliyetlere gore platform bazli otomatik fiyat guncelleme
- **Mobil Depo Otomasyonu:** Depo ici toplama, paketleme islemleri
- **Fulfillment (3PL) Entegrasyonu:** Dis depolama ve lojistik cozumleriyle cift tarafli entegrasyon
- **Tek Tusla Fatura:** Toplu e-fatura kesimi
- **Set/Paket Ozeligi:** Birden fazla urunu set olarak satma
- **Kampanyaya Stok Ayirma:** Kampanya bazinda stok rezervasyonu
- **Toplu islemler ve Excel destegi**
- **Hizli urun ekleme**
- **Muhasebe entegrasyonu** (Parasut, Logo, Mikro vb.)

#### Pazaryeri Entegrasyonlari
- Tum yurtici pazaryerleri (Trendyol, Hepsiburada, N11, Amazon TR, Ciceksepeti, PttAVM, Pazarama, Temu vb.)
- 100+ entegre platform
- Paket farki gozetmeksizin tum pazaryerleri dahil

#### Fiyatlandirma
- Başlangıç paketi ile basla, paketler arasi fiyat farkini odeyerek yukselt
- Tum paketlerde tum pazaryerleri dahil (ek ucret yok)
- Yeni gelistirmeler ve platform entegrasyonlari ek ucret gerektirmez
- Spesifik fiyatlar web sitesinde gizli (teklif bazli), ancak yillik yenileme ~20.000 TL civari (kullanici yorumlarina gore)

#### Kullanici Yorumlari ve Sikayetler
- **Olumlu:** Genis pazaryeri destegi, rekabet analizi modulu, cozum odakli destek
- **Olumsuz:** Yillik yenileme fiyatinin ilk alis fiyatiyla neredeyse ayni olmasi (kiralama hissi), Trendyol urun guncelleme sorunlari (2+ ay cozumsuz), destek İletişiminde gecikmeler
- **Destek:** Haftanin 7 gunu 09:00-24:00

#### Benzersiz Ozellikler
- **Rekabet Analizi modulu** (pazarda en gelismis)
- **Akilli Fiyatlandirma** (maliyet bazli otomatik)
- **Mobil Depo Otomasyonu** (barkod okuyucu ile depo yonetimi)
- **Fulfillment entegrasyonu** (3PL Şirketleriyle)

---

### 1.3 Parasut

**Web:** parasut.com
**Sahibi:** Mikro Yazilim (Logo Yazilim grubu)
**Tip:** On muhasebe + e-fatura programi (e-ticaret entegrasyonu modulu ile)

#### Temel Ozellikler
- On muhasebe programi (birincil islev)
- E-Fatura, E-Arsiv, E-Irsaliye
- Gelir-gider takibi
- Stok ve coklu depo yonetimi
- Nakit akim yonetimi
- Teklif ve Sipariş yonetimi
- Banka entegrasyonu (otomatik mutabakat)
- E-ticaret entegrasyonu (pazaryeri Siparişleri -> otomatik faturalastirma)

#### Pazaryeri Entegrasyonlari
- Trendyol, Hepsiburada, N11, Amazon, Ciceksepeti, PttAVM
- E-ticaret altyapilari: Shopify, ikas, Ticimax, IdeaSoft, WooCommerce vb.
- Kargo entegrasyonlari

#### Fiyatlandirma (2026)
| Plan | Fiyat |
|------|-------|
| Yillik abonelik | 66 TL/ay (792 TL/yil) + KDV |
| Aylik abonelik | 95 TL/ay + KDV |
| e-Kontor (100 adet) | 400 TL + KDV |
| e-Kontor (1000 adet) | 2.500 TL + KDV |

- 14 gun ucretsiz deneme
- E-faturaya gecis maliyeti: 0 TL (abonelik disinda)

#### Kullanici Yorumlari
- **Olumlu:** Kullanim kolayligi, uygun fiyat, genis entegrasyon agi
- **Olumsuz:** Sinirli envanter yonetimi, pazaryeri entegrasyonu "tek yonlu" (fatura odakli), stok senkronizasyonu zayif

#### Benzersiz Ozellikler
- Turkiye'nin en yaygin on muhasebe yazilimi
- Mali musavir portali (Parasut Atlas)
- Cok Düşük fiyat noktasi
- Logo Yazilim ekosistemi ile entegrasyon

> **Not:** Parasut dogrudan bir rakip degil, tamamlayici bir urun. Ancak "tek panelden her sey" vizyonumuz icin muhasebe modulu olarak referans alinabilir.

---

### 1.4 Diger Turk Rakipler

#### ikas
**Web:** ikas.com | **Tip:** E-ticaret altyapisi (SaaS)

- Yeni nesil, bulut tabanli e-ticaret altyapisi
- Sinirsiz trafik ve urun
- Pazaryeri entegrasyonu (Trendyol, Hepsiburada, Amazon, Etsy)
- SEO optimizasyonu, mobil uyumluluk
- Pazarlama otomasyonlari: sepet hatirlatma, e-posta, cross-sell, upsell
- Coklu dil ve para birimi (e-ihracat)
- Paketler: Start (ucretsiz), Grow, Scale, Scale Plus, Premium
- 60.000 TL'ye varan kargo destegi kampanyasi
- %0 sanal POS komisyonu (sinirli)
- **Odak:** E-ticaret sitesi altyapisi, entegrasyon ikincil

#### T-Soft
**Web:** tsoft.com.tr | **Kurulis:** 2003 | **Tip:** E-ticaret altyapisi

- Turkiye'nin en eski e-ticaret altyapilarindan
- Orta-buyuk olcekli markalar icin
- Pazaryeri entegrasyonu (stok, fiyat, Sipariş senkronizasyonu)
- Muhasebe programi entegrasyonu
- E-ihracat altyapisi (coklu dil, doviz, ETGB, mikro ihracat)
- Kurumsal teklif modeli (fiyatlar yuksek, bazi moduller ekstra ucretli)
- **Odak:** Kurumsal e-ticaret altyapisi

#### Dopigo
**Web:** dopigo.com | **Kurulis:** 2017 | **Tip:** Pazaryeri entegrasyon platformu

- Pazaryeri + e-ticaret sitesi + e-fatura + muhasebe + 3PL entegrasyonu
- Fiyat karsilastirma ozelligi
- AI destekli fiyat optimizasyonu ve chatbot (2026 yenilikleri)
- KOBi odakli, esnek paketler
- Aylik ve yillik plan secenekleri
- E-fatura entegrasyonu (Parasut, Bizim Hesap vb.)
- ikas, Shopify, WooCommerce entegrasyonlari
- **Odak:** KOBi segmenti, basitlik

#### Shopside
**Web:** shopside.io | **Tip:** Pazaryeri entegrasyon platformu

- Tek fiyatla tum entegrasyonlar
- Kurulum sihirbazi (teknik destek gerektirmez)
- Tek tusla e-fatura
- Gercek zamanli stok yonetimi
- Varyant yonetimi (varyant bazinda farkli fiyat)
- Kargo entegrasyonu (pazaryeri + kendi anlasmali kargo)
- Gizli ucret yok politikasi
- **Odak:** Basitlik, seffaf fiyatlandirma

#### Obase
**Web:** obase.com | **Kurulis:** 1995 | **Tip:** Kurumsal e-ticaret + perakende cozumleri

- Entegre uctan uca e-ticaret cozum seti
- Yapay zeka destekli, bulut tabanli pazaryeri yonetimi
- Gercek zamanli analitik ve donusum orani optimizasyonu
- Kisisellestirilmis musteri deneyimi
- Fulfillment entegrasyonu (Ekol360)
- Cok kanalli perakende altyapisi
- **Odak:** Buyuk kurumsal musteriler, perakende zincirleri

#### Omniens
**Web:** omniens.com | **Tip:** Fulfillment + pazaryeri yonetim platformu

- Merkezi urun katalogu (tum kanallar icin)
- Kanal bazinda fiyatlandirma ve stok kurallari
- Varyant yonetimi
- Urun listeleme otomasyonu
- Fulfillment odakli yaklasim
- **Odak:** Depo operasyonlari, fulfillment

---

## 2. Yurtdisi Pazari

### 2.1 Linnworks (UK)

**Web:** linnworks.com
**Sahibi:** Bagimsiz | **Tip:** Cok kanalli Sipariş ve envanter yonetimi

#### Temel Ozellikler
- Envanter yonetimi (coklu depo, gercek zamanli senkronizasyon)
- Sipariş yonetimi ve fulfillment otomasyonu
- Otomatik Sipariş yonlendirme (order routing)
- Depo yonetimi (WMS)
- Satin alma Siparişi yonetimi (purchase orders)
- Kitting ve bundling
- Gonderim yonetimi (coklu kargo firmasiyla entegrasyon)
- Raporlama ve analitik
- 100+ marketplace entegrasyonu

#### Pazaryeri Entegrasyonlari
Amazon, eBay, Shopify, TikTok Shop, Walmart, Etsy, BigCommerce, WooCommerce ve 100+ diger platform. Entegrasyonlar sinirrsiz ve genellikle ucretsiz.

#### Fiyatlandirma
- Sipariş hacmine gore fiyatlandirma (gelir yuzdesI YOK)
- Başlangıç: $449/ay
- Ek moduller: Sadece kullandigin icin ode
- Her pakete ozel onboarding plani ve uzman dahil

#### Kullanici Yorumlari
- **Olumlu:** Guclu otomasyon, genis entegrasyon agi, Sipariş yonlendirme
- **Olumsuz:** Yuksek fiyat, kurulum zorlugu, performans sorunlari, ozellestirme sinirlamalari, buyuyen isletmeler icin esneklik yetersizligi

#### Benzersiz Ozellikler
- **Otomatik Sipariş yonlendirme** (kurallara gore en uygun depodan gonderim)
- **Purchase order yonetimi** (tedarikci Sipariş sureci)
- **Kitting/bundling** (set urun yonetimi)

---

### 2.2 ChannelAdvisor / Rithum (US)

**Web:** channeladvisor.com (simdi rithum.com)
**Tip:** Enterprise cok kanalli ticaret platformu

#### Temel Ozellikler
- Urun listeleme yonetimi (tum kanallarda)
- Envanter ve fiyat senkronizasyonu
- Sipariş isleme ve fulfillment koordinasyonu
- **Otomatik yeniden fiyatlandirma (repricing):** Makine ogrenmesi ile pazaryeri trendlerine gore dinamik fiyat ayarlama
- **Talep tahmini (demand forecasting):** ML tabanli envanter planlama
- Gonderim yonetimi (coklu tasiyici, dinamik oran karsilastirma, iade destegi)
- Reklam yonetimi (marketplace ads optimizasyonu)
- Dropship yonetimi
- Brand Analytics

#### Pazaryeri Entegrasyonlari
**420+ global pazaryeri:** Amazon, Walmart, Target+, Google Shopping, eBay, Etsy ve yuzlerce uluslararasi/nis kanal.

#### Fiyatlandirma
- Abonelik bazli, yillik sozlesme
- $12.000 - $50.000+/yil
- Gelir belirli bir Eşiği astiktan sonra gelir yuzdesi de alinabilir
- Entegrasyon Sayısı, satis hacmi ve secilen ozelliklere gore degisir

#### Kullanici Yorumlari
- **Olumlu:** En genis pazaryeri destegi, enterprise sinif, guclu repricing
- **Olumsuz:** Cok pahali, ogrenme egrisi dik, KOBi'ler icin uygun degil, sozlesme esnekligi az

#### Benzersiz Ozellikler
- **420+ pazaryeri** (sektordeki en genis)
- **ML tabanli repricing ve demand forecasting**
- **Marketplace reklam yonetimi** (Amazon Ads, Walmart Ads vb. tek panelden)
- **Dropship yonetimi**
- **Brand Analytics** (marka koruma ve izleme)

---

### 2.3 Sellbrite (US - GoDaddy)

**Web:** sellbrite.com
**Sahibi:** GoDaddy (2019'da satin alindi) | **Tip:** Cok kanalli listeleme ve envanter yonetimi

#### Temel Ozellikler
- Cok kanalli urun listeleme (tek arayuzden)
- Envanter senkronizasyonu (gercek zamanli, tum kanallarda)
- Toplu listeleme ve sablon destegi
- Listing kurallari ve ozel sablonlar
- Mevcut listelemeler icin guncelleme/bitirme/yeniden listeleme
- Sipariş yonetimi

#### Pazaryeri Entegrasyonlari
Amazon, eBay, Walmart, Etsy, Shopify, BigCommerce, WooCommerce

#### Fiyatlandirma
| Plan | Fiyat | Sipariş Limiti |
|------|-------|----------------|
| Starter | $29/ay | Sinirli |
| Standard | $49/ay | Orta |
| Premium | $179/ay | Yuksek |
| Shopify Edition | $19/ay | - |

- 30 gun ucretsiz deneme

#### Kullanici Yorumlari
- **Olumlu:** Basit ve kullanici dostu, uygun fiyat, hizli kurulum
- **Olumsuz:** Sinirli ozellik seti, enterprise ihtiyaclari icin yetersiz, raporlama zayif

#### Benzersiz Ozellikler
- GoDaddy ekosistemi ile entegrasyon (domain + hosting + e-ticaret + pazaryeri)
- En Düşük giris fiyati ($19/ay)
- %115 Sipariş artisi (GoDaddy entegrasyonu sonrasi ortalama)

---

### 2.4 Brightpearl (UK - Sage)

**Web:** brightpearl.com
**Sahibi:** Sage (2022'de satin alindi) | **Tip:** Perakende isletme sistemi (Retail Operating System)

#### Temel Ozellikler
- Sipariş yonetimi ve otomasyon
- Envanter planlama (talep tahmini)
- Depo yonetimi (WMS)
- Muhasebe (entegre, ayri yazilim gerektirmez)
- Iade yonetimi
- CRM
- Satin alma Siparişi yonetimi
- B2B Sipariş portali
- POS entegrasyonu (fiziksel Mağaza)
- Dropship otomasyonu
- Gercek zamanli muhasebe guncellemeleri

#### Pazaryeri Entegrasyonlari
Shopify, Magento, Amazon, eBay, BigCommerce + genis ucuncu taraf entegrasyon marketplace'i (odeme, kargo, raporlama vb.)

#### Fiyatlandirma
- Ozel teklif (her musteriye ozel cozum)
- Onboarding ucreti: $10.000+ (kurulum, egitim, veri goci)
- Sinirsiz kullanici (ek ucret yok)
- Tahmini yillik maliyet: $15.000 - $50.000+

#### Kullanici Yorumlari
- **Olumlu:** Kapsamli all-in-one cozum, sinirsiz kullanici, envanter planlama, hizli destek
- **Olumsuz:** Cok yuksek onboarding maliyeti, veri goci zorlugu, buyudukce bug'lar artabiliyor, destek bazen cozum odakli degil

#### Benzersiz Ozellikler
- **Entegre muhasebe** (ayri yazilim gerektirmez)
- **Talep tahmini ve envanter planlama** (veri odakli satin alma onerileri)
- **Sinirsiz kullanici** (tum planlarda)
- **B2B Sipariş portali** (toptan satis)
- **%20-30 idari maliyet azaltimi** (firma iddiasi)

---

### 2.5 Cin7 (NZ/Global)

**Web:** cin7.com
**Tip:** Omnichannel envanter ve Sipariş yonetimi + ERP

#### Temel Ozellikler
- Envanter yonetimi (sinirsiz lokasyon)
- Sipariş yonetimi (omnichannel)
- Entegre POS (fiziksel Mağaza)
- Depo yonetimi
- Satin alma Siparişi yonetimi
- Uretim yonetimi (BOM - Bill of Materials)
- Kitting ve bundling
- B2B Sipariş portali
- EDI entegrasyonu
- Coklu is birimi yonetimi (multi-entity)
- Otomasyon isakislari (workflow builder)
- 700+ platform entegrasyonu

#### Pazaryeri Entegrasyonlari
Amazon, eBay, Walmart, Etsy, Shopify, BigCommerce + POS + 3PL + kargo + 700+ entegrasyon

#### Fiyatlandirma
| Urun | Hedef |
|------|-------|
| Cin7 Core | Kucuk isletmeler |
| Cin7 Omni | Buyuk/karmasik operasyonlar |

- Sipariş hacmine ve modullere gore fiyatlandirma
- Sik ve onemli fiyat artislari (kullanici sikayeti)

#### Kullanici Yorumlari
- **Olumlu:** Cok genis entegrasyon, omnichannel yetkinlik, ozellestirilebilir is akislari
- **Olumsuz:** Sik fiyat artislari, destek sorunlari, uretim modulu bug'lari, Shopify entegrasyonunda sorunlar

#### Benzersiz Ozellikler
- **Uretim yonetimi (BOM)** (hammadde -> son urun sureci)
- **EDI entegrasyonu** (buyuk perakendecilerle veri alisverisi)
- **Workflow builder** (gorsel otomasyon olusturucu)
- **700+ entegrasyon** (sektordeki en genis ekosistemlerden)
- **Multi-entity** (holding yapisindaki Şirketler icin)

---

## 3. Karsilastirma Tablosu

### Turkiye Pazari

| Ozellik | Ticimax | Entegra | Dopigo | Shopside | Parasut |
|---------|---------|---------|--------|----------|---------|
| E-ticaret sitesi | Var | Yok | Yok | Yok | Yok |
| Pazaryeri entegrasyonu | Var (paket bazli) | Var (tumu dahil) | Var | Var | Sinirli |
| E-fatura | Var | Var | Var | Var | Var (birincil) |
| Muhasebe | Entegrasyon | Entegrasyon | Entegrasyon | Yok | Var (birincil) |
| Rekabet analizi | Yok | Var | Var | Yok | Yok |
| Akilli fiyatlandirma | Yok | Var | Var | Yok | Yok |
| Depo yonetimi | Yok | Var (mobil) | Var (3PL) | Yok | Sinirli |
| AI ozellikleri | Var (icerik/gorsel) | Var (fiyat) | Var (fiyat/chatbot) | Yok | Yok |
| B2B modulu | Var | Yok | Yok | Yok | Yok |
| Fiyat araligi (yillik) | 39-79K TL | ~20K+ TL | Degisken | Seffaf | ~800 TL |

### Yurtdisi Pazari

| Ozellik | Linnworks | ChannelAdvisor | Sellbrite | Brightpearl | Cin7 |
|---------|-----------|----------------|-----------|-------------|------|
| Pazaryeri Sayısı | 100+ | 420+ | 7-8 | 10+ | 700+ entg. |
| Envanter yonetimi | Gelismis | Gelismis | Temel | Gelismis | Gelismis |
| Sipariş yonlendirme | Var | Var | Yok | Var | Var |
| Depo yonetimi (WMS) | Var | Yok | Yok | Var | Var |
| Muhasebe | Entegrasyon | Yok | Yok | Entegre | Entegre |
| Repricing (AI) | Yok | Var (ML) | Yok | Yok | Yok |
| Talep tahmini | Yok | Var (ML) | Yok | Var | Yok |
| Uretim (BOM) | Yok | Yok | Yok | Yok | Var |
| POS | Yok | Yok | Yok | Entegrasyon | Entegre |
| B2B portal | Yok | Yok | Yok | Var | Var |
| Fiyat | $449+/ay | $12K+/yil | $29+/ay | Ozel teklif | Degisken |

---

## 4. Bizde Olmayan Ozellikler

Mevcut projemiz: Blazor Server + PostgreSQL, Trendyol/N11/HB/Pazarama/Amazon/PttAVM/Ciceksepeti/Temu entegrasyonlari, urun/Sipariş/stok yonetimi, kategori eslestirme, multi-tenant hazirlik, kargo entegrasyonlari (Aras, Surat, Yurtici).

### Kritik Eksikler (Yuksek Oncelik)

1. **Rekabet Analizi / Rakip Fiyat Takibi**
   - Entegra ve Dopigo'nun en guclu silahi
   - Pazaryerlerindeki rakip fiyatlarini otomatik takip
   - Belirlenen sinirlar icinde otomatik fiyat guncelleme
   - *Etki: Dogrudan satis arttirici, musteri icin en degerli ozellik*

2. **Akilli/Dinamik Fiyatlandirma**
   - Maliyet degisikliklerinde otomatik fiyat guncelleme
   - Kar marji koruma kurallari
   - Platform bazinda farkli fiyatlandirma stratejileri
   - *Etki: Operasyonel yukun en buyuk paylasi fiyat yonetimi*

3. **E-Fatura / E-Arsiv Entegrasyonu**
   - Turkiye'de zorunlu, tum rakiplerde var
   - Tek tusla toplu fatura kesimi
   - Parasut/Logo/Bizim Hesap entegrasyonu
   - *Etki: Musterilerin entegrasyon tercihindeki #1 kriter*

4. **Depo Yonetimi (WMS)**
   - Barkod tabanli toplama/paketleme
   - Mobil cihaz destegi (el terminali/telefon)
   - Coklu depo yonetimi
   - Raf/konum tanimi
   - *Etki: Orta-buyuk isletmelerin olmazsa olmazi*

5. **Raporlama ve Analitik Dashboard**
   - Satis raporlari (kanal bazli, urun bazli, donem bazli)
   - Kar/zarar analizi (komisyon, kargo, vergi hesabi dahil)
   - Stok devir hizi, en cok/az satan urunler
   - *Etki: Karar verme mekanizmasi, upsell firsati*

### Onemli Eksikler (Orta Oncelik)

6. **Otomatik Sipariş Yonlendirme (Order Routing)**
   - Birden fazla depo/Mağaza varsa en uygun noktadan gonderim
   - Stok durumu + mesafe + maliyet bazli karar
   - Linnworks'un en guclu ozelligi

7. **Toplu Islem Araclari (Bulk Operations)**
   - Excel import/export (urun, fiyat, stok)
   - Toplu fiyat guncelleme (yuzde/sabit artirim)
   - Toplu urun aktarma (kanallar arasi)
   - Toplu gorsel yukleme/Değiştirme

8. **Set/Paket (Bundle/Kit) Yonetimi**
   - Birden fazla urunu tek SKU olarak satma
   - Otomatik stok dusumu (parcalarina gore)
   - Cin7 ve Linnworks'ta standart

9. **Satin Alma Siparişi (Purchase Order) Yonetimi**
   - Tedarikci Siparişleri olusturma ve takip
   - Minimum stok uyarilari
   - Otomatik satin alma onerileri
   - Tedarikci performans takibi

10. **Iade Yonetimi (Returns Management)**
    - Iade talebi olusturma ve takip
    - Iade nedeni analizi
    - Otomatik stok iadesi
    - Pazaryeri iade entegrasyonu

### Gelecek Icin Degerli (Düşük Oncelik - Ama Fark Yaratici)

11. **AI Destekli Ozellikler**
    - Urun aciklamasi olusturma (ChatGPT/Claude entegrasyonu)
    - Gorsel olusturma/iyilestirme
    - Talep tahmini
    - Otomatik kategori eslestirme onerileri

12. **B2B Sipariş Portali**
    - Toptan musteriler icin ozel giris
    - Ozel fiyat listeleri
    - Minimum Sipariş miktarlari

13. **Marketplace Reklam Yonetimi**
    - Trendyol/HB/Amazon reklam kampanyasi yonetimi
    - Reklam performans raporlama
    - Butce optimizasyonu

14. **POS Entegrasyonu**
    - Fiziksel Mağaza satislariyla entegrasyon
    - Omnichannel stok yonetimi

---

## 5. Yurtdisindan Getirilebilecek Yenilikler

Turkiye pazarinda henuz yaygin olmayan, yurtdisi yazilimlardan alinabilecek fikirler:

### 5.1 ML Tabanli Talep Tahmini (Brightpearl, ChannelAdvisor)
- Gecmis satis verisi + sezonluk trendler + dis faktorler
- "Bu urun 2 hafta icinde tukenir" uyarilari
- Otomatik satin alma onerisi
- **Turkiye'de kimse yapmiyor.** Ilk yapan buyuk avantaj kazanir.

### 5.2 Gorsel Workflow/Otomasyon Builder (Cin7)
- Surukle-birak ile is kurali olusturma
- "Stok 5'in altina duserse -> bildirim gonder -> tedarikciye Sipariş olustur"
- "Sipariş gelince -> en yakin depodan ata -> kargo fisi olustur"
- **Turkiye'de sadece statik kurallar var, gorsel builder yok.**

### 5.3 Entegre Muhasebe (Brightpearl, Cin7)
- Ayri muhasebe yazilimi gerektirmeyen, entegre muhasebe modulu
- Satis -> fatura -> muhasebe kaydi otomatik
- **Turkiye'de herkes Parasut/Logo'ya entegre oluyor, kimse entegre muhasebe sunmuyor.**

### 5.4 Omnichannel / POS Birlestirme (Cin7, Brightpearl)
- Online + offline satislari tek stok havuzunda yonetme
- "Online Sipariş, Mağazadan teslim" (BOPIS)
- **Turkiye'de Nebim/Logo/Mikro ayri, pazaryeri ayri. Birlestiren yok.**

### 5.5 Dropship Otomasyon (ChannelAdvisor, Brightpearl)
- Tedarikci urunlerini kendi stogunuz gibi satma
- Sipariş gelince otomatik tedarikciye iletme
- **Turkiye'de dropship yapan cok ama otomasyon sunan entegrator yok.**

### 5.6 EDI Entegrasyonu (Cin7)
- Buyuk perakendecilerle (Migros, A101, BIM) elektronik veri degisimi
- Sipariş, fatura, sevk irsaliyesi otomasyonu
- **Turkiye'de bu alan tamamen manuel.**

### 5.7 Brand Analytics / Marka Koruma (ChannelAdvisor)
- Pazaryerlerinde marka ihlali tespiti
- Yetkisiz satici takibi
- Fiyat bozma analizi
- **Turkiye'de buyuk markalarin acil ihtiyaci.**

---

## 6. Rekabet Avantaji Onerileri

Projemizin vizyonu "tek yazilimda her sey" -- yani Nebim + entegrasyon + ekstra araclar yerine tek cozum. Bu vizyonu guclendirecek stratejik oneriler:

### 6.1 "All-in-One" Konumlandirma
Turkiye'deki mevcut durum:
- Stok/muhasebe icin: Nebim, Logo, Mikro
- Pazaryeri entegrasyonu icin: Entegra, Dopigo
- E-fatura icin: Parasut
- Kargo icin: ayri anlasmalar
- Raporlama icin: Excel

**Bizim firsatimiz:** Tek yazilimda hepsini sunmak. Bu, musteri icin:
- Daha az lisans maliyeti
- Tek ogrenim egrisi
- Verilerin tek yerde olmasi (daha iyi analiz)
- Tek destek noktasi

### 6.2 Fiyatlandirma Stratejisi
Rakiplerin zayifligi: Yuksek fiyatlar + paket kisitlamalari + gizli ucretler.

**Onerimiz:**
- **Tek paket, sinirsiz pazaryeri** (Entegra modeli)
- **Sipariş hacmine gore fiyatlandirma** (Linnworks modeli)
- **Gelir yuzdesi almama** garantisi
- **Başlangıç icin Düşük giris noktasi**, buyudukce yukseltme

### 6.3 Teknik Ustunlukler
- **Multi-tenant mimari** (zaten hazirlaniyoruz): Rakiplerin cogu single-tenant
- **Gercek zamanli senkronizasyon** (SignalR zaten var)
- **Modern tech stack** (Blazor Server + PostgreSQL): Rakiplerin cogu eski teknolojiler
- **API-first tasarim**: Musteri kendi araclariyla entegre olabilsin

### 6.4 Dikey Entegrasyon Firsatlari
- **Kargo anlasmasi brokerage**: Toplu kargo anlasmasi yapmak ve musterilere indirimli sunmak (Ticimax ve ikas bunu yapiyor)
- **Sanal POS brokerage**: Toplu POS anlasmasi
- **E-fatura brokerage**: E-kontor toptan alip perakende satmak

### 6.5 Kilitlenme Stratejisi (Lock-in)
- Veri ne kadar cok birikirse, musteri o kadar ayrilamaz
- Raporlama ve analitik bu verinin uzerine kurulur
- Talep tahmini ve AI ozellikleri gecmis veriye bagimlidir
- **Veri = en buyuk rekabet avantaji**

---

## 7. Oncelikli Gelistirme Onerileri

Asagidaki siralama, deger/maliyet orani ve rekabet etkisine gore yapilmistir.

### Faz 1: Temel Rekabet Paritesi (0-3 Ay)

| # | Ozellik | Neden Oncelikli | Referans |
|---|---------|-----------------|----------|
| 1 | **E-Fatura Entegrasyonu** | Turkiye'de zorunlu, entegrasyon seciminde #1 kriter | Parasut API, GIB |
| 2 | **Raporlama Dashboard** | Musteri karar mekanizmasi, upsell | Tum rakipler |
| 3 | **Toplu Islem Araclari** | Operasyonel verimlilik, gunluk kullanim | Entegra, Ticimax |
| 4 | **Iade Yonetimi** | Sipariş dongusunun tamamlanmasi | Tum rakipler |

### Faz 2: Rekabet Avantaji (3-6 Ay)

| # | Ozellik | Neden Oncelikli | Referans |
|---|---------|-----------------|----------|
| 5 | **Rekabet Analizi** | En cok talep edilen ozellik, satis arttirici | Entegra, Dopigo |
| 6 | **Akilli Fiyatlandirma** | Rekabet analizinin dogal devami | Entegra |
| 7 | **Depo Yonetimi (WMS)** | Orta-buyuk musteri segmenti | Entegra, Linnworks |
| 8 | **Set/Paket Yonetimi** | Sik sorulan ozellik | Cin7, Linnworks |

### Faz 3: Fark Yaraticilar (6-12 Ay)

| # | Ozellik | Neden Oncelikli | Referans |
|---|---------|-----------------|----------|
| 9 | **AI Urun Aciklamasi** | Turkiye'de az, WOW etkisi | Ticimax AI |
| 10 | **Talep Tahmini** | Turkiye'de HICBIR rakipte yok | Brightpearl, ChannelAdvisor |
| 11 | **Workflow Builder** | Turkiye'de yok, guclu otomasyon | Cin7 |
| 12 | **B2B Portal** | Toptan satis segmenti | Brightpearl, Cin7 |

### Faz 4: Pazar Liderligi (12+ Ay)

| # | Ozellik | Neden Oncelikli | Referans |
|---|---------|-----------------|----------|
| 13 | **Entegre On Muhasebe** | "Tek yazilimda her sey" vizyonu | Brightpearl |
| 14 | **POS Entegrasyonu** | Omnichannel | Cin7 |
| 15 | **Dropship Otomasyon** | Buyuyen segment | ChannelAdvisor |
| 16 | **EDI Entegrasyonu** | Kurumsal musteriler | Cin7 |
| 17 | **Brand Analytics** | Marka musterileri | ChannelAdvisor |

---

## Sonuc

Turkiye e-ticaret entegrasyon pazari hizla buyuyor ancak rakipler genellikle bir alanda uzmanlasmis durumda:
- Ticimax: E-ticaret altyapisi + pazaryeri
- Entegra: Pazaryeri entegrasyonu + rekabet analizi
- Parasut: Muhasebe + e-fatura
- Dopigo: KOBi odakli basit entegrasyon

**Kimse "hepsini" yapmiyor.** Bu bizim en buyuk firsatimiz.

Yurtdisi yazilimlar (ozellikle Brightpearl ve Cin7) "Retail Operating System" konseptini basariyla uyguluyor. Bu konsepti Turkiye pazarina adapte edecek ilk oyuncu, pazarin lideri olur.

Oncelikli adimlar:
1. E-fatura entegrasyonu (zorunlu)
2. Raporlama (karar verme)
3. Rekabet analizi (satis arttirma)
4. AI ozellikleri (WOW etkisi + fark yaratma)

**Hedef:** 12 ay icinde Turkiye'nin ilk "All-in-One E-Ticaret Isletme Sistemi" olmak.

---

## Kaynaklar

### Turkiye
- [Ticimax E-Ticaret Paketleri](https://www.ticimax.com/e-ticaret-paketleri/)
- [Ticimax Ana Sayfa](https://www.ticimax.com/)
- [Entegra Bilisim](https://www.entegrabilisim.com/)
- [Entegra Genel Ozellikler](https://www.entegrabilisim.com/genel-ozellikler)
- [Entegra Rekabet Analizi](https://www.entegrabilisim.com/rekabet-analizi-b-NjA=)
- [Parasut Fiyatlandirma](https://www.parasut.com/on-muhasebe-fiyatlari)
- [Parasut E-Ticaret Entegrasyonlari](https://www.parasut.com/e-ticaret-entegrasyonlari)
- [Dopigo Fiyatlar](https://www.dopigo.com/fiyatlar/)
- [Dopigo Entegrasyonlar](https://www.dopigo.com/tum-entegrasyonlar/)
- [Shopside Ozellikler](https://shopside.io/shopside-ozellikler/)
- [ikas E-Ticaret](https://ikas.com/tr)
- [T-Soft Pazaryeri Entegrasyonlari](https://www.tsoft.com.tr/pazaryeri-entegrasyonlari)
- [Obase E-Ticaret](https://obase.com/cozumlerimiz/sektorel-cozumler/e-ticaret)
- [Omniens](https://www.omniens.com/en)
- [Yengec Entegrator Karsilastirmasi](https://yengec.co/blog/e-ticaret-entegratorleri-ozellik-ve-fiyatlari/)
- [Entegra vs Dopigo vs StockMount Karsilastirma 2026](https://www.eticaretmerkezim.com/pazaryeri-entegrasyonlari-karsilastirma-2026-entegra-vs-dopigo-vs-stockmount/)
- [Ticimax Sikayetvar](https://www.sikayetvar.com/ticimax)
- [Entegra Bilisim Sikayetvar](https://www.sikayetvar.com/entegra-bilisim)

### Yurtdisi
- [Linnworks Pricing](https://www.linnworks.com/pricing/)
- [Linnworks Capterra](https://www.capterra.com/p/116088/Linnworks/)
- [ChannelAdvisor/Rithum Capterra](https://www.capterra.com/p/32810/ChannelAdvisor-Enterprise/)
- [ChannelAdvisor Pricing Analysis](https://www.oreateai.com/blog/unpacking-channeladvisor-pricing-what-you-need-to-know/b4b073fccfe409a062887c24f95f41b4)
- [Sellbrite Pricing](https://www.sellbrite.com/pricing-pro/)
- [Sellbrite GoDaddy Acquisition](https://techcrunch.com/2019/04/10/godaddy-acquires-sellbrite-to-launch-cross-marketplace-tools/)
- [Brightpearl Capterra](https://www.capterra.com/p/124180/Brightpearl/)
- [Brightpearl G2 Reviews](https://www.g2.com/products/brightpearl/reviews)
- [Cin7 Pricing](https://www.cin7.com/pricing/)
- [Cin7 Capterra](https://www.capterra.com/p/133133/Cin7/)
- [Linnworks Alternatives 2025](https://threadgoldconsulting.com/insights/best-linnworks-alternatives)

### Trendler
- [2026 E-Ticaret Yapay Zeka Trendleri](https://www.kanaliztv.com/2026/02/25/2026-e-ticaret-stratejileri-akinsoft-ve-ticimax-entegrasyonlarinda-yapay-zeka-devrimi-bolum-3-final/)
- [Turkiye E-Ticaret Mega Trendleri 2026](https://www.medyaloji.net/son-haber/turkiyede-e-ticaret-ekosisteminin-gelecegi-2026ya-yon-veren-mega-trendler_23684695.html)
- [2026 Yapay Zeka Uygulama Rehberi](https://www.kanaliztv.com/2026/02/24/yapay-zeka-uygulama-rehberi-2026-teoriden-pratige-dijital-donusum-stratejileri/)
