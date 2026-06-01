---
title: Hediye Çeki Satın Al
target_view: Views/GiftCard/Index.cshtml
controller_action: GiftCard/Index
route: /hediye-ceki
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: medium
---

# Hediye Çeki Satın Al

**Bağlam (tek paragraf):** Hediye çeki satın alma sayfası — tema seç, tutar seç, alıcı bilgileri gir, gerçek zamanlı önizleme. Ortalı max-w-4xl. Sample data: 6 tema (Doğum Günü, Bebek Hoş Geldin, Bayram, Açılış, Sevgiyle, Yeni Yıl).

## Tasarım istekleri
- Ortalı `max-w-4xl py-12`.
- **Hero kart** `bg-primary/10 rounded-3xl p-10 relative`: sol H1 Fraunces "Zekids Bebe Hediye Çeki" + açıklama; sağ 3D rotated hediye çeki görseli (CSS gradient + Fraunces logo, `rotate-[-3deg]`).
- **Tasarım Seç** carousel: 6 tema kart yan yana scroll-snap, `rounded-2xl aspect-[3/4]` tema illustration + ad, seçili `ring-2 ring-primary`.
- **Tutar Seç** chip row: 100 / 250 / 500 / 1.000 / 2.500₺ / "Diğer" → input açılır.
- **Alıcı bilgileri** form: ad-soyad, e-posta, teslimat tarihi (Flatpickr bugün+), "Mesajınız" textarea (200 char sayaç).
- **Önizleme** kart altta: gerçek zamanlı çek (alıcı adı, tutar, mesaj görünür).
- "Hediye Çekini Sepete Ekle" primary full CTA.

## Sample data ipucu
- 6 tema, fallback tutar 250₺. Model yok → ViewBag/inline.

## JS etkileşim ipucu
- Tema seç + tutar seç + form input → canlı önizleme güncelle (`checkout.js`/yeni `giftcard.js`).
- Flatpickr (lib mevcut) teslimat tarihi; char counter; carousel scroll-snap.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

GiftCard/Index — Hediye çeki satın al
Sample data: 6 tema (Doğum Günü, Bebek Hoş Geldin, Bayram, Açılış, Sevgiyle, Yeni Yıl).
- Ortalı max-w-4xl py-12
- **Hero kart** bg-primary/10 rounded-3xl p-10 relative:
  - Sol: H1 Fraunces "Zekids Bebe Hediye Çeki" + açıklama "Sevdiklerinize özel hediye çekiyle alışveriş özgürlüğü hediye edin."
  - Sağ: 3D rotated hediye çeki kart görseli (CSS gradient + Fraunces logo, hafif rotate-[-3deg])
- **Tasarım Seç** carousel: 6 tema kart yan yana scroll-snap, her biri rounded-2xl aspect-[3/4] tema illustration + tema adı, seçili ring-2 ring-primary
- **Tutar Seç**: chip row (rounded-full border px-5 py-2):
  - 100₺ / 250₺ / 500₺ / 1.000₺ / 2.500₺ / "Diğer" → input açılır
- **Alıcı bilgileri** form:
  - Alıcı ad-soyad
  - Alıcı e-posta
  - Teslimat tarihi (Flatpickr — bugün veya ileri)
  - "Mesajınız" textarea (200 char sayaç)
- **Önizleme** kart altta: gerçek zamanlı çek görüntüsü güncelleniyor (alıcı adı, tutar, mesaj görünür)
- "Hediye Çekini Sepete Ekle" primary full CTA
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/HediyeCeki.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
