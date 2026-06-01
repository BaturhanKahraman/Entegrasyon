---
title: Hediye Çeki Oluşturuldu
target_view: Views/GiftCard/Created.cshtml
controller_action: GiftCard/Created
route: /hediye-ceki/olusturuldu
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: medium
---

# Hediye Çeki Oluşturuldu

**Bağlam (tek paragraf):** Hediye çeki satın alımı tamamlandıktan sonra gösterilen onay/sonuç sayfası. Büyük çek önizlemesi + kod + PDF indirme. Ortalı card max-w-2xl.

## Tasarım istekleri
- Ortalı `card max-w-2xl py-16`.
- Yumuşak success ikonu.
- H1 "Hediye Çekiniz Hazır".
- "{RecipientName} adına {DeliveryDate} tarihinde gönderilecek."
- **Büyük çek önizlemesi** card: brand themed gradient + tema görseli + alıcı + tutar + kod mono.
- Kod kutu: mono font + "Kopyala" + "PDF İndir" buttonlar.
- 2 CTA: "Başka Çek Al" + "Anasayfa".

## Sample data ipucu
- RecipientName "Ayşe Yılmaz", DeliveryDate "4 Haziran 2026", tutar 250₺, kod "ZBGC-7F3K-9M2P".

## JS etkileşim ipucu
- "Kopyala" → clipboard + Notyf toast.
- "PDF İndir" → link/endpoint.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

GiftCard/Created — Çek oluştu
- Ortalı card max-w-2xl py-16
- Yumuşak success ikonu
- H1 "Hediye Çekiniz Hazır"
- "{RecipientName} adına {DeliveryDate} tarihinde gönderilecek."
- **Büyük çek önizlemesi** card (brand themed gradient + tema görseli + alıcı + tutar + kod mono)
- Kod kutu: mono font + "Kopyala" + "PDF İndir" buttonlar
- 2 CTA: "Başka Çek Al" + "Anasayfa"
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/HediyeCekiOlusturuldu.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
