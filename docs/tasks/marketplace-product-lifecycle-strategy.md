# Marketplace Urun Yasam Dongusu Yonetimi — Arastirma & Strateji

**Tarih:** 2026-03-29
**Durum:** Arastirma tamamlandi, implementasyon parcalara ayrilacak
**Iliskili spec:** `docs/superpowers/specs/2026-03-29-leaf-node-enforcement-design.md`

---

## Problem Tanimi

Sistemimizde bir kategori, attribute veya eslestirme degistiginde, bu degisikligin marketplace'te yayinda olan urunlere etkisi kontrol edilmiyor. Kullanici bilgilendirilmiyor ve etkilenen urunleri platform bazinda yonetme imkani yok.

Ayrica eslestirme sayfalarinda zaten eslestirilen platformlar tekrar gosteriliyor (duplicate matching riski).

---

## Buyuk Platformlarin Yaklasimi

| Ilke | Aciklama | Uygulayan |
|------|----------|-----------|
| **Grandfathering** | Mevcut urunler eski semayla calismaya devam eder | Amazon, Trendyol, Hepsiburada |
| **Grace Period** | Yeni kurala uyum icin sure taninir | Amazon (6-12 ay), Trendyol (degisken) |
| **Kademeli Yaptirim** | Uyari → listing kalitesi dusurme → pasife alma | Tum platformlar |
| **Asla Otomatik Silme** | Hicbir platform urunu otomatik silmez | Evrensel |
| **Schema Versioning** | Kategori attribute semalari versiyonlanir | Amazon (PTD), Trendyol (periyodik) |

**Bizim stratejimiz:** Pasif yaklasim + aktif uyari. Marketplace'e dokunma, kullaniciyi bilgilendir, platforma ozel aksiyon alma imkani sun.

---

## Mevcut Marketplace API Yetenekleri

### Capability Matrix

| Islem | Trendyol | Hepsiburada | N11 |
|-------|----------|-------------|-----|
| Icerik guncelleme (baslik, aciklama, gorsel) | Onaylanmis urunlerde | Ayri API (ticket-api, henuz implement edilmedi) | Tam destek |
| Fiyat/stok guncelleme | Sinir yok, gercek zamanli | Listing API ile | Tam destek |
| Attribute guncelleme | Sadece content-bulk-update ile | Hayir | Hayir (yeniden yayinlama gerekir) |
| Urunu gizle/pasife al | Archive endpoint | Deactivate endpoint | Stop Selling |
| Urunu kaldir/sil | Delete (onayli urunlerde archive once) | Sadece onaylanmamis urunler | Tam destek |
| Urunu yeniden aktifle | Unlock endpoint | Activate endpoint | Start Selling |
| Durum sorgulama | Batch polling (5dk) | TrackingId + Listing status | Manuel |
| Yapisal degisiklik (marka, kategori, barkod) | IMKANSIZ | IMKANSIZ | IMKANSIZ (yeniden yayinlama) |

### Implementasyon Durumlari

**Trendyol (MarketPlaceId=1):**
- Implement edilmis: Yayinlama, durum sync, icerik guncelleme, fiyat/stok sync
- Eksik: Archive, Unlock, Delete, Buybox

**Hepsiburada (MarketPlaceId=3):**
- Implement edilmis: Yayinlama, listing yonetimi (fiyat/stok/kargo), activate/deactivate
- Eksik: Urun bilgi guncelleme (ticket API), bulk unlock

**N11 (MarketPlaceId=2):**
- Implement edilmis: Yayinlama, silme, temel guncelleme, start/stop selling
- Eksik: Yok (tum operasyonlar mevcut)

---

## Degisiklik Etki Zinciri (Blast Radius)

### Kategori Attribute Degistirme

```
Attribute eklendi (yeni zorunlu attribute)
  → Mevcut urunlerde bu attribute yok
    → Marketplace'e sonraki guncelleme denemesi başarısız olur
      → Urun "guncelleme gerekli" durumuna duser

Attribute kaldirildi
  → Urunlerdeki mevcut degerler orphan olur (kullanilmaz ama zarar vermez)
    → Marketplace'teki urun etkilenmez (orada eski haliyle kalir)
      → Sorun yok — sadece temizlik gerekir

Attribute zorunluluk degisti (optional → required)
  → Degeri olmayan urunler "eksik zorunlu attribute" durumuna duser
    → Marketplace'e gonderilemez
```

### Marketplace Eslestirme Degistirme

```
Kategori eslestirmesi kaldirildi
  → O kategorideki TUM urunler o marketplace'e gonderilemez
    → Yayindaki urunler marketplace'te eski haliyle kalir
      → Fiyat/stok guncellemesi bile yapilamaz

Attribute eslestirmesi kaldirildi
  → Zorunlu attribute ise → yayinlama başarısız
  → Opsiyonel ise → sessizce atlanir, marketplace'te eski deger kalir

Attribute value eslestirmesi kaldirildi
  → O degeri kullanan urunler → custom value veya hata
```

---

## Onerilen Implementasyon Parcalari

### Parca 1: Duplicate Matching Prevention (Kucuk — 1-2 gun)

**Scope:** UI filtreleme. Zaten eslestirilen platform/attribute/value tekrar gosterilmesin.

**Detaylar:**
- Kategori eslestirme sayfasinda: Zaten eslestirilen marketplace'ler "Eslestrildi" chip'i ile gosterilir, yeniden secim icin once mevcut eslestirmeyi kaldirmak gerekir
- Attribute eslestirme sayfasinda: Zaten eslestirilen marketplace attribute'lari seceneklerden cikarilir
- Value eslestirme sayfasinda: Ayni mantik

**Etkilenen dosyalar:**
- `CategorySync.razor` / `.razor.cs`
- `BulkCategoryMatchPage.razor.cs`
- `AttributeDetailPanel.razor` / `.razor.cs`

---

### Parca 2: Degisiklik Etki Analizi + Uyari (Orta — 3-5 gun)

**Scope:** Kullanici bir degisiklik yaptiginda, etkilenen yayindaki urunleri gosterip uyarmak.

**Senaryolar ve uyarilar:**

| Degisiklik | Uyari |
|-----------|-------|
| Kategori attribute ekleme (zorunlu) | "Bu kategoride X urun var. Yeni zorunlu attribute eklendiginde bu urunler marketplace'e guncelleme gonderilemeyecek." |
| Kategori attribute kaldirma | "X urunun bu attribute degeri orphan olacak. Marketplace'teki urunler etkilenmez." |
| Kategori marketplace eslestirmesi kaldirma | "Bu eslestirmeyi kaldirirseniz X yayindaki urun artik {Platform}'a guncelleme gonderemez." |
| Attribute marketplace eslestirmesi kaldirma | "Bu eslestirmeyi kaldirirseniz zorunlu attribute eslestirmesi kalkacak. X urun etkilenir." |

**Teknik yaklasim:**
- `ImpactAnalysisService` — degisiklik oncesi etkilenen urun Sayısıni hesaplar
- Business layer'daki ilgili metodlara entegre edilir (save oncesi)
- UI'da uyari dialog'u gosterilir

---

### Parca 3: Marketplace-Specific Urun Yonetim Sayfasi (Buyuk — 1-2 hafta)

**Scope:** Her platformdaki urunu bagimsiz yonetebilecek sayfa.

**Sayfa yapisi:** `/marketplace-sync/products/{marketplaceId}`

**Ozellikler:**
- Secilen marketplace'te yayinlanmis urunlerin listesi (ProductMarketplace tablosundan)
- Her urun icin durum gosterimi (Published, Pending, Failed, Rejected, Archived)
- Platforma ozel aksiyonlar:

| Aksiyon | Trendyol | Hepsiburada | N11 |
|---------|----------|-------------|-----|
| Icerik guncelle | content-bulk-update | ticket-api (yeni) | UpdateProductBasic |
| Fiyat/stok guncelle | price-and-inventory | listing price/stock | UpdateProductBasic |
| Pasife al | Archive | Deactivate | Stop Selling |
| Aktifle | Unlock | Activate | Start Selling |
| Kaldir | Delete (archive once) | Delete (sadece onaylanmamis) | Delete |
| Yeniden gonder | Re-publish | Re-submit | SaveProduct |

**Platform-agnostic interface:**
```
IMarketplaceProductOperations
  ├── UpdateContentAsync(productId, dto)
  ├── UpdatePriceStockAsync(productId, dto)
  ├── DeactivateAsync(productId)
  ├── ActivateAsync(productId)
  ├── DeleteAsync(productId)
  └── RepublishAsync(productId)
```

Her marketplace bu interface'i implement eder. UI platform secildikten sonra dogru implementasyonu kullanir.

---

### Parca 4: Grandfathering + Urun Durum Yonetimi (Buyuk — 1-2 hafta)

**Scope:** Degisiklik sonrasi etkilenen urunlerin durumunu otomatik yonetme.

**Yeni entity/field:**

```
ProductMarketplace tablosuna eklenmesi gerekenler:
  - NeedsUpdate: bool (default false)
  - NeedsUpdateReason: string? ("Zorunlu attribute eklendi", "Eslestirme kaldirildi" vb.)
  - NeedsUpdateSince: DateTimeOffset?
```

**Background job:** `MarketplaceComplianceCheckService`
- Periyodik (gunluk) calisir
- Her yayindaki urun icin:
  1. Kategori eslestirmesi var mi?
  2. Zorunlu attribute'lar eslestirili mi?
  3. Urun tum zorunlu attribute degerlerine sahip mi?
- başarısız olanlar → `NeedsUpdate = true` + sebep

**UI gosterim:**
- Dashboard'da "Guncelleme gereken urunler" karti
- Marketplace urun sayfasinda "Needs Update" filtresi
- Urun detayinda platform bazinda uyari

---

## Onerilen Implementasyon Sirasi

```
Parca 1: Duplicate Prevention (kucuk, hemen deger katar)
    ↓
Parca 2: Etki Analizi + Uyari (kullaniciyi bilgilendirir)
    ↓
Parca 3: Marketplace Urun Yonetim Sayfasi (aksiyon alma imkani)
    ↓
Parca 4: Grandfathering + Otomatik Durum Yonetimi (uzun vadeli)
```

Her parca bagimsiz olarak spec → plan → implementasyon dongusunden gecer. Parca 1 ve 2 olmadan Parca 3 ve 4 eksik kalir (uyari yoksa aksiyon sayfasi niye var?). Parca 3 olmadan Parca 4 eksik kalir (durum belirledik ama kullanici ne yapacak?).

---

## Acik Sorular (Gelecek spec'lerde cevaplanacak)

1. **Trendyol yapisal degisiklik:** Kategori degistiginde Trendyol'daki urunu silip yeniden mi olusturmak lazim? (Buyuk risk — satis gecmisi kaybolur)
2. **Hepsiburada ticket-api:** Urun bilgi guncelleme API'si implement edilmeli mi yoksa sadece listing operasyonlari yeterli mi?
3. **Multi-tenant:** Compliance check service tenant bazinda mi calisacak?
4. **Bildirim:** "Guncelleme gerekli" durumundaki urunler icin email/push bildirim gonderilmeli mi?
5. **Otomatik retry:** başarısız guncelleme denemeleri otomatik tekrarlanmali mi?
