---
title: Satıcı Paneli (Dashboard)
target_view: Views/Seller/Panel.cshtml
controller_action: Seller/Panel
route: /satici/panel
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Paneli (Dashboard)

**Bağlam (tek paragraf):** Satıcının en kompleks sayfası — sol satıcı sidebar (storefront sidebar'dan farklı) + metric grid + gelir grafiği + aksiyon kartı + son siparişler + en çok satanlar. Sample data: bugün 8 sipariş, 2.450₺ gelir, 245 aktif ürün, 5 bekleyen, 4.7 puan, %3.2 dönüşüm; son 7 gün gelir mock; son 5 sipariş + top 5 ürün.

## Tasarım istekleri
- **Satıcı sidebar** (`lg:w-64 sticky lg:top-24`, storefront sidebar'dan FARKLI): üst mağaza logo + ad + "Mağaza Profilini Gör" link; menü Panel (aktif) / Ürünlerim / + Ürün Ekle / Siparişlerim / Bakiye / Profil / Çıkış.
- İçerik (sağ):
  - H1 "Mağaza Paneli" + sağda tarih range picker (Flatpickr "Son 7 gün").
  - **Metric grid** (`md:grid-cols-3 lg:grid-cols-6 gap-4`), her kart `rounded-2xl p-5` farklı pastel: Bugün Gelir 2.450₺ +12% yeşil / Bugün Sipariş 8 +2 / Aktif Ürün 245 / Bekleyen Sipariş 5 (turuncu) / Mağaza Puanı 4.7 yıldız / Dönüşüm %3.2 trend.
  - **Gelir Grafiği** kart `bg-white rounded-2xl p-6`: "Son 7 Gün Gelir" + SVG polyline çizgi (primary, gradient fill); X 7 gün, Y 0/500/1000/2000/3000.
  - **Aksiyon kart** `bg-warning/10 border border-warning rounded-2xl p-5`: warning ikon + "5 yeni siparişiniz bekliyor" + "Görüntüle" primary.
  - **Son Siparişler** card `divide-y`: 5 satır # + alıcı (masked Ayşe Y.) + adet + tutar + durum chip + "Detay".
  - **En çok satan ürünler** card `divide-y`: 5 satır thumbnail + ad + adet + gelir.

## Sample data ipucu
- Tüm metrikler yukarıda; grafik mock 7 günlük dizi; model yok → ViewBag/inline.

## JS etkileşim ipucu
- Flatpickr range picker; SVG polyline statik (veya küçük render JS); satıcı sidebar mobilde drawer (`site.js` openDrawer pattern reuse).

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı. Küçük bir ekibin küçük admin paneli — minimal, fonksiyonel, kid-brand'a uygun.

Seller/Panel — Dashboard (en kompleks)
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
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiPanel.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
