---
title: Satıcı Bakiye
target_view: Views/Seller/Balance.cshtml
controller_action: Seller/Balance
route: /satici/bakiye
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Bakiye

**Bağlam (tek paragraf):** Satıcının kazanç/bakiye/komisyon durumunu ve hareketlerini gördüğü sayfa — Wallet stiline benzer gradient bakiye kartı. Satıcı sidebar sabit.

## Tasarım istekleri
- Satıcı sidebar.
- Bakiye kartı (Wallet stiline benzer, primary gradient): "Kullanılabilir" 1.450₺ Fraunces; 2 sub-stat "Beklemede" 850₺ + "Toplam Kazanç" 12.450₺; "Çekim Talep Et" primary CTA.
- Komisyon kartı: "Komisyon Oranı: %12" + açıklama.
- Hareketler tablo: tarih, sipariş no, brüt, komisyon (−), net, durum chip.

## Sample data ipucu
- 1.450₺ kullanılabilir, 850₺ beklemede, 12.450₺ toplam; 8-12 hareket satırı; model yok → ViewBag/inline.

## JS etkileşim ipucu
- "Çekim Talep Et" → modal/POST; statik ağırlıklı.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

Seller/Balance
- Satıcı sidebar
- Bakiye kartı (Wallet stiline benzer, primary gradient):
  - "Kullanılabilir" 1.450₺ Fraunces
  - 2 sub-stat: "Beklemede" 850₺ + "Toplam Kazanç" 12.450₺
  - "Çekim Talep Et" primary CTA
- Komisyon kartı: "Komisyon Oranı: %12" + açıklama
- Hareketler tablo: tarih, sipariş no, brüt, komisyon (-), net, durum chip
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiBakiye.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
