---
title: Hediye Çeki Bakiye
target_view: Views/GiftCard/Balance.cshtml
controller_action: GiftCard/Balance
route: /hediye-ceki/bakiye
model: None (POST sonrası sonuç state'i)
status: pending
source_html: henüz tasarlanmadı
priority: medium
---

# Hediye Çeki Bakiye

**Bağlam (tek paragraf):** Hediye çeki kodu girilerek bakiye sorgulanan basit form sayfası. POST sonrası sonuç kartı gösterilir. Ortalı card max-w-md.

## Tasarım istekleri
- Ortalı `card max-w-md py-12`.
- H1 "Hediye Çeki Bakiyem".
- Input: çek kodu (auto-uppercase mask).
- "Sorgula" CTA.
- Sonuç state'i (POST sonrası): card `bg-success/10 rounded-2xl p-6` — "Mevcut Bakiye" etiket + "250₺" Fraunces `text-4xl` primary + "Son kullanma: 31 Aralık 2027".
- Yardım not: "Çek kodunuzu satın aldığınız e-postada bulabilirsiniz."

## Sample data ipucu
- Sorgu state ve sonuç state'i ayrı; tasarımda ikisini de göster. Örnek bakiye 250₺.

## JS etkileşim ipucu
- Kod input auto-uppercase mask (IMask lib mevcut).
- POST → PRG; sonuç TempData/ViewBag ile döner.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

GiftCard/Balance — Bakiye sorgula
- Ortalı card max-w-md py-12
- H1 "Hediye Çeki Bakiyem"
- Input: çek kodu (auto-uppercase mask)
- "Sorgula" CTA
- Sonuç state'i (POST sonrası):
  - Card bg-success/10 rounded-2xl p-6:
    - "Mevcut Bakiye" üst etiket
    - "250₺" Fraunces text-4xl primary
    - "Son kullanma: 31 Aralık 2027"
- Yardım not: "Çek kodunuzu satın aldığınız e-postada bulabilirsiniz."
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/HediyeCekiBakiye.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
