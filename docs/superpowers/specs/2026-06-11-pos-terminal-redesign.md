# Spec: POS Terminal Redesign — 2-Panel Satış Ekranı (Task #83)

**Tarih:** 2026-06-11  
**Durum:** TL onayı bekleniyor  
**Bağlı task:** tasks.json #83  
**Risk Seviyesi:** 🔴 YÜKSEK — POS canlı satış aracı; regresyon = esnaf satış yapamaz

---

## ⚠️ Risk Notu (Önce Oku)

POS, `zekidsbebe.com`'un **fiziksel mağaza satış aracı**. Herhangi bir sepet/ödeme/kasa regresyonu anlık gelir kaybına yol açar. Bu spec'i uygularken:

1. Tüm HTMX hedef ID'leri (`#pos-cart`, `#modal-container`) değiştirilemez.
2. Tüm JS fonksiyon adları (`posAddVariantToCart`, `syncFinalizeButton`, `closePosPaymentModal`) değiştirilemez.
3. Tüm form `name` attribute'ları (`payments[i].PaymentMethodId`, `payments[i].Amount`, `submitToken` vb.) değiştirilemez.
4. `data-pos-auto-add` attribute'u barkod auto-add akışının temelidir — kaldırılamaz.
5. **Her değişiklik sonrası tam satış senaryosu (ürün ekle → adet → indirim → ödeme → fiş) test edilmeli.**

---

## Mevcut Durum — Kod Teyidi

**Dosyalar (okundu):**
- `Features/POS/Views/Index.cshtml` (359 satır)
- `Features/POS/Views/Partials/_POSCart.cshtml`
- `Features/POS/Views/Partials/_POSPaymentDialog.cshtml`
- `Features/POS/ViewModels/POSTerminalVm.cs`
- `Features/POS/POSController.cs`

### Mevcut 2-Panel Layout

```
[Kasa: CashierName | OpenedAt | X Raporu btn | Kasayı Kapat btn]
─────────────────────────────────────────────────────────────────
col-lg-8 (Sol)              col-lg-4 (Sağ)
┌─ Arama ────────────────┐  ┌─ Müşteri ──────────────────┐
│  input + dropdown      │  │  search + badge + quick-add │
│  #posSearchInput       │  └────────────────────────────┘
├─ Sepet (#pos-cart) ────┤  ┌─ Oturum Özeti ─────────────┐
│  tablo: ürün/adet/fiyat│  │  datagrid: açılış/satış/net │
│  tfoot: ara/kdv/toplam │  └────────────────────────────┘
│  İndirim btn           │  ┌─ Satışı Tamamla ────────────┐
└────────────────────────┘  │  btn-lg btn-success          │
                             │  (disabled = sepet boş)     │
                             └────────────────────────────┘
```

### Tüm Endpoint'ler (Değiştirilemez)

| HTTP | Route | Görev |
|------|-------|-------|
| GET | `/pos` | Ana sayfa (kasa aç veya aktif panel) |
| POST | `/pos/open-session` | Kasa açma |
| POST | `/pos/close-session` | Kasa kapama |
| GET | `/pos/search` | HTMX ürün arama (→ `_POSSearchResults`) |
| POST | `/pos/add-item` | Sepete ekle (→ `#pos-cart` innerHTML) |
| POST | `/pos/update-quantity` | Adet güncelle (→ `#pos-cart` innerHTML) |
| POST | `/pos/remove-item` | Satırı kaldır |
| POST | `/pos/clear-cart` | Sepeti temizle |
| GET | `/pos/search-customer` | Müşteri arama HTMX |
| POST | `/pos/select-customer` | Müşteri seç |
| GET | `/pos/quick-customer-dialog` | Hızlı müşteri formu |
| POST | `/pos/quick-customer` | Hızlı müşteri kaydet |
| POST | `/pos/clear-customer` | Müşteriyi kaldır |
| GET | `/pos/payment-dialog` | Ödeme modalı (→ `#modal-container`) |
| GET | `/pos/close-session-dialog` | Kasa kapama dialogu |
| GET | `/pos/item-discount-dialog` | Satır indirim dialogu |
| POST | `/pos/apply-item-discount` | Satır indirimini uygula |
| GET | `/pos/cart-discount-dialog` | Sepet indirim dialogu |
| POST | `/pos/apply-cart-discount` | Sepet indirimini uygula |
| POST | `/pos/clear-cart-discount` | Sepet indirimini kaldır |
| POST | `/pos/complete-sale` | **Satışı tamamla** (kritik) |
| GET | `/pos/x-report` | X Raporu |
| GET | `/pos/z-report/{sessionId}` | Z Raporu (kasa kapanışı) |

### Kritik JS Davranışları (Değiştirilemez)

| Davranış | Tetikleyici | Açıklama |
|----------|-------------|----------|
| Barkod auto-add | `data-pos-auto-add` attr | 300ms debounce → `/pos/add-item` HTMX POST |
| Finalize sync | `syncFinalizeButton()` | `#pos-cart` güncellendikçe → sepet boşsa disable |
| Ödeme hesabı | JS `recalcTotals()` | Ödenen/Kalan/Para Üstü anlık hesap |
| Double-submit koruma | `submitToken` hidden input | Tamamla butonu → disabled + spinner |
| Toast listener | HTMX event | Sepet güncellemelerinde toast |

---

## Redesign Kapsamı

### Faz 1 — Görsel Cila (Güvenli, Önce Yap)

Fonksiyonel hiçbir şeye dokunmadan saf görsel iyileştirmeler:

#### 1.1 Kasa Aç Ekranı

**Mevcut:** `card col-md-4` basit form — başlık yok, sayfa başlığı yok.

**Hedef:**
- `page-header` + `page-title`: "POS Terminal" + `ti-cash-register` ikonu
- Kasa Aç kartı: `card-sm` → daha dar (`col-md-5 col-lg-4`), dikey ortada
- Form alanları: `form-label` doğru hizalama, `ti-terminal` ikonu input prefix

#### 1.2 Aktif Oturum — Top Bar

**Mevcut:** `d-flex justify-content-between` + basit metin + butonlar.

**Hedef:**
```
[ti-cash-register] POS Terminal   [badge bg-blue-lt: CashierName]  [badge bg-azure-lt: OpenedAt]   [btn-sm X Raporu]  [btn-sm btn-danger Kasayı Kapat]
```
- Kasiyer adı + açılış saati badge olarak göster
- Butonlar `btn-sm` + ikon

#### 1.3 Sol Panel — Arama Alanı

**Mevcut:** `input-group` + placeholder text.

**Hedef:**
- `input-group-lg` → büyük arama kutusu (barkod okuyucu hedef)
- Prefix ikon: `ti-barcode`
- Placeholder: `"Barkod okutun veya ürün adı yazın..."`
- Focus ring: `border-primary`
- Dropdown sonuçları: ürün küçük resmi + varyant badge + stok uyarısı (stok=0 → `text-danger`)

#### 1.4 Sepet Tablosu (_POSCart.cshtml)

**Mevcut:** `table-hover` ile düz tablo, tfoot toplamları. Bozuk Türkçe metin.

**Hedef:**
- **Türkçe düzeltme:** `"Sepet bos"` → `"Sepet boş"`, `"arama yapin"` → `"arama yapın"`
- Adet kontrolü: `-` / `+` butonları `input-group-sm` width `100px` (kompakt)
- **tfoot iyileştirme:**
  - Ara Toplam + KDV satırları: `text-secondary` ile daha soluk
  - TOPLAM satırı: `fs-3 fw-bold text-primary` → belirgin
  - İndirim satırı: `bg-green-lt` arka plan + `text-success fw-bold`
- Satır indirim butonu (`ti-percentage`): zaten ghost-primary ✓
- Silme butonu (`ti-x`): zaten ghost-danger ✓

#### 1.5 Sağ Panel — Müşteri + Özet + Tamamla

**Mevcut:** üç ayrı kart, düz layout.

**Hedef:**
- **Müşteri kartı:** `<i class="ti ti-user me-2"></i> Müşteri` kart başlığı + `card-sm`
  - Müşteri seçilmemişse: "Anonim satış" `text-secondary small` bilgisi
  - Müşteri seçilmişse: `_POSCustomerBadge` partial zaten var ✓
- **Oturum Özeti kartı:** `datagrid` — mevcut yeterli ✓
- **Satışı Tamamla:** zaten `btn-lg btn-success w-100` ✓; padding/margin normalize

#### 1.6 Ödeme Modalı (_POSPaymentDialog.cshtml)

**Mevcut:** `modal-lg`, summary cards, method buttons, payment rows JS.

**Hedef (minimal):**
- Summary 3'lü kart: zaten `card-sm` ✓; Toplam kart `bg-green-lt` ✓
- Ödeme method butonları: `btn-outline-primary` + ikon zaten var ✓
- **Para üstü:** daha belirgin (`fw-bold text-success fs-4`)
- Tamamla butonu + spinner + disabled: zaten var ✓ (dokunma)

---

### Faz 2 — Layout Yeniden Düzenleme (Yüksek Risk — Ayrı Task)

> ⚠️ **Bu faz Faz 1'den ayrı bir task olarak ele alınmalı.** DOM yapısı değiştiği için tüm HTMX hedefleri yeniden doğrulanmalı, tam E2E paketi yeşil olmalı.

**TL'nin vizyonu:** `sol: ürün arama + kategori filtresi | sağ: sepet + toplam + ödeme`

```
col-lg-5 (Sol)                col-lg-7 (Sağ)
┌─ Arama ───────────────────┐  ┌─ Müşteri ──────────────────┐
│  barcode input             │  │  search + badge             │
│  [kategori chip filtreleri]│  └────────────────────────────┘
├─ Arama Sonuçları ──────────┤  ┌─ Sepet (#pos-cart) ─────────┐
│  ürün grid/listesi         │  │  tablo + tfoot               │
└────────────────────────────┘  └────────────────────────────┘
                                 ┌─ Satışı Tamamla ────────────┐
                                 │  btn-lg btn-success w-100   │
                                 └────────────────────────────┘
```

**Bu layout'un gerektireceği değişiklikler:**
- `#pos-cart` HTMX target ID aynı kalır (ID bazlı) ✓
- `#modal-container` tam genişlik dışında kalır ✓
- `syncFinalizeButton()` → `#pos-cart` observe eder, konum fark etmez ✓
- **Risk:** Barkod auto-add sonrası sepet scroll → sağ panelde gizlenmemeli

**Faz 2 öncesi şartlar:**
1. Faz 1 canlıda, regresyon yok
2. Tam Playwright E2E paketi yeşil (barkod → ekle → ödeme → fiş)
3. DB transaction test (concurrent add-item)

---

## Kapsam Dışı (Bu Task İçin)

- Backend değişikliği yok (endpoint, VM, controller)
- Kategori filtresi ekleme — Faz 2 kapsamında ayrıca belirlenir
- Klavye kısayolları (F-tuşları vb.) — ayrı task
- POS raporlama (X/Z Raporu) sayfası redesign — ayrı task
- Çoklu kasa / terminal yönetimi — gelecek özellik

---

## Rol Dağılımı

| Kim | Ne |
|-----|----|
| **Designer** | Faz 1: `Index.cshtml` + `_POSCart.cshtml` görsel cila (layout değiştirmeden) |
| **QA** | Faz 1 sonrası: tam satış senaryosu E2E (barkod → sepet → ödeme → fiş) |
| **SWE** | Faz 2 (ayrı task): layout yeniden yapılandırma + HTMX target doğrulama |
| **QA** | Faz 2 sonrası: Playwright regresyon paketi + edge case (boş stok, çoklu ödeme) |

---

## Kabul Kriterleri — Faz 1

1. Kasa Aç ekranı `page-header` ile açılıyor.
2. Aktif oturum top bar'da kasiyer adı + açılış saati badge görünüyor.
3. Arama inputu büyük (`input-group-lg`), barkod ikonu var.
4. Sepet tablosunda TOPLAM satırı belirgin (`fs-3`), Türkçe metin düzgün.
5. Ödeme modalı para üstü okunaklı.
6. **Tüm işlevsel akış bozulmamış** (aşağıdaki Manuel Test Adımları geçiyor).

---

## Manuel Test Adımları — Tam Satış Senaryosu (Regresyon Gate)

### A. Kasa Açma
1. `/pos` aç → "Kasa Aç" form görünmeli (oturum yok).
2. Açılış Kasası `500` TL gir, Terminal ID boş → "Kasayı Aç" POST.
3. Aktif POS paneli açılmalı; top bar'da kasiyer adı + saat görünmeli.

### B. Ürün Ekleme — Barkod
4. Arama inputuna odaklan.
5. Bilinen barkod yaz → 300ms sonra otomatik sepete eklenmeli (HTMX, sayfa yenilemesi yok).
6. "Satışı Tamamla" butonu aktif hale gelmeli.

### C. Ürün Ekleme — Arama
7. Ürün adının 3 harfini yaz → dropdown açılmalı.
8. Varyantlı ürün: varyant seçim butonları görünmeli, seç → sepete eklenmeli.

### D. Adet + Satır İndirimi
9. `+` butonuna tıkla → adet +1, toplam güncellenmeli.
10. Adet `1`'deyken `-` → ürün sepetten silinmeli.
11. Satır `%` butonu → indirim modalı açılmalı.
12. `%10` gir, uygula → üstü çizili eski fiyat + yeni fiyat görünmeli.

### E. Sepet İndirimi
13. tfoot "İndirim" butonu → sepet indirim modalı açılmalı.
14. `%5` genel indirim → TOPLAM güncellenmeli, indirim satırı `bg-green-lt`.
15. `X` ile kaldır → indirim satırı yok, TOPLAM eski değere döner.

### F. Müşteri Seçimi
16. Müşteri arama → sonuçlar dropdown'da, seç → `_POSCustomerBadge` görünmeli.
17. "Müşteriyi Kaldır" → anonim duruma dön.

### G. Ödeme — Nakit
18. "Satışı Tamamla" → ödeme modalı açılmalı.
19. 3 özet kart: Ara Toplam, KDV, Toplam (yeşil) görünmeli.
20. Nakit seç → tutar alanı açılmalı, varsayılan `GrandTotal`.
21. Nakit `200 TL` gir, toplam `150 TL` → Para Üstü `50 TL` görünmeli.
22. Kalan `0.00` olunca → "Satışı Tamamla" aktif + yeşil.
23. Tıkla → spinner görünmeli, ikinci tıklamada buton devre dışı (double-submit koruması).
24. Başarı → modal kapanmalı, sepet temizlenmeli.

### H. Çoklu Ödeme
25. Hem Nakit hem Kart ekle → toplamlar senkronize (paid + remaining doğru).
26. Kart → Provizyon Kodu alanı görünmeli.

### I. Kasa Kapama
27. "Kasayı Kapat" → kasa kapama dialogu açılmalı.
28. Kapanış kasası gir, onayla → `/pos` "Kasa Aç" ekranı görünmeli.

### J. Mobil (768px)
29. Sol/sağ paneller `col-12` olmalı, dikey sıralı.
30. Barkod input tam genişlik, "Satışı Tamamla" görünür/erişilebilir.
