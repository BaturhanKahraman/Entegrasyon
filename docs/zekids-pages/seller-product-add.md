---
title: Satıcı Ürün Ekle
target_view: Views/SellerProduct/Add.cshtml
controller_action: SellerProduct/Add
route: /satici/urun-ekle
model: List<Product> (wizard)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Ürün Ekle

**Bağlam (tek paragraf):** Satıcının yeni ürün eklediği 4 adımlı wizard — sol form + sağ sticky canlı önizleme. `@model List<Product>`. Satıcı sidebar sabit. En karmaşık satıcı formu.

## Tasarım istekleri
- Satıcı sidebar.
- Stepper üstte 4 adım: Temel / Görseller / Varyantlar / SEO & Yayın.
- Layout: sol form (2/3) + sağ sticky preview kart (1/3) — preview anasayfa ürün kartı stilinde.
- **Adım 1 — Temel:** Ürün adı (slug otomatik); Marka Tom Select; Kategori Tom Select ağaç (Kız → Üst Giyim → Tişört); Yaş aralığı chip multi (10 chip); Cinsiyet radio; Mevsim chip multi (4 chip); Kısa açıklama textarea; Uzun açıklama (basit rich text: bold/italic/list).
- **Adım 2 — Görseller:** Drag-drop multi upload (kapak: ilk yüklenen veya seç); SortableJS sıralama; her görsele alt text input.
- **Adım 3 — Varyantlar:** Beden chip multi; Renk input row (ad + hex picker + ekle); otomatik matrix tablo (beden × renk), her hücre genişler: SKU, fiyat, eski fiyat, stok, barkod; toplu fiyat/stok düzenle button (üstte).
- **Adım 4 — SEO & Yayın:** SEO slug; Meta title (60 char sayaç); Meta description (160 char sayaç); Etiketler Tom Select multi; "Hemen Yayınla" / "Taslak Kaydet" radio.
- "Kaydet" primary full.

## Sample data ipucu
- Boş wizard; sağ önizleme form değiştikçe güncellenir; model yok/inline.

## JS etkileşim ipucu
- Stepper geçişleri; Tom Select (marka/kategori/etiket); SortableJS (görsel sıra, lib mevcut); drag-drop upload; hex color picker; matrix tablo render; char counter; canlı önizleme binding.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

SellerProduct/Add (@model List<Product> — wizard)
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

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiUrunEkle.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
