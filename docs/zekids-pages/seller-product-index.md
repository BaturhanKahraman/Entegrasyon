---
title: Satıcı Ürünleri
target_view: Views/SellerProduct/Index.cshtml
controller_action: SellerProduct/Index
route: /satici/urunler
model: List<SellerProduct>
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Ürünleri

**Bağlam (tek paragraf):** Satıcının ürün kataloğunu yönettiği tablo sayfası — toplu seçim aksiyon barı, durum filtreleri. `@model List<SellerProduct>`. Satıcı sidebar sabit.

## Tasarım istekleri
- Satıcı sidebar.
- H1 "Ürünlerim" + "+ Yeni Ürün" primary.
- Filter: Aktif / Pasif / Stok Bitti / Onay Bekliyor.
- Tablo sütunları: Checkbox | Görsel 50x50 | Ürün adı (slug alt) | SKU | Fiyat | Stok | Durum chip | Eylem dropdown (Düzenle, Pasif Yap, Sil). Hover `bg-cream/50`.
- Toplu seçim aksiyon bar (sticky alt): "X seçildi · Toplu Pasif Yap / Sil / Fiyat Güncelle".
- Boş: "Henüz ürün eklemediniz" + "İlk Ürünü Ekle" CTA.

## Sample data ipucu
- `@model` boşsa 10-15 örnek ürün satırı, karışık durum; ayrıca empty state'i göster.

## JS etkileşim ipucu
- Checkbox toplu seçim → sticky aksiyon bar göster; filter chip; eylem dropdown.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

SellerProduct/Index (@model List<SellerProduct>)
- Satıcı sidebar
- H1 "Ürünlerim" + "+ Yeni Ürün" primary
- Filter: Aktif / Pasif / Stok Bitti / Onay Bekliyor
- Tablo:
  - Sütunlar: Checkbox | Görsel 50x50 | Ürün adı (slug alt) | SKU | Fiyat | Stok | Durum chip | Eylem dropdown (Düzenle, Pasif Yap, Sil)
  - Hover bg-cream/50
- Toplu seçim aksiyon bar (sticky alt): "X seçildi · Toplu Pasif Yap / Sil / Fiyat Güncelle"
- Boş: "Henüz ürün eklemediniz" + "İlk Ürünü Ekle" CTA
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiUrunler.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
