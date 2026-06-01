---
title: Kargo Takip Form
target_view: Views/Tracking/Index.cshtml
controller_action: Tracking/Index
route: /kargo-takip
model: None
status: pending
source_html: henüz tasarlanmadı
priority: high
---

# Kargo Takip Form

**Bağlam (tek paragraf):** Sipariş veya kargo takip numarasıyla sorgulama yapılan basit form sayfası. Ortalı card max-w-lg.

## Tasarım istekleri
- Ortalı `card max-w-lg py-12`.
- H1 "Kargonuzu Takip Edin".
- Açıklama: "Sipariş numaranız veya kargo takip kodunuzla anlık durumu öğrenin."
- Input pill: "Sipariş veya takip no".
- "Sorgula" primary CTA.
- Alt not muted: "Sipariş numarası satın alma e-postanızda yer alır."

## Sample data ipucu
- Form-only; model yok. Submit `/kargo-takip/{kod}` sonuç sayfasına yönlendirir.

## JS etkileşim ipucu
- Basit form submit; gerekirse `tracking.js` minimal validasyon.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

Tracking/Index — Kargo takip form
- Ortalı card max-w-lg py-12
- H1 "Kargonuzu Takip Edin"
- Açıklama: "Sipariş numaranız veya kargo takip kodunuzla anlık durumu öğrenin."
- Input pill: "Sipariş veya takip no"
- "Sorgula" primary CTA
- Alt not muted: "Sipariş numarası satın alma e-postanızda yer alır."
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/KargoTakip.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
