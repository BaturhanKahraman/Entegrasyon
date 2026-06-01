---
title: Satıcı Siparişleri
target_view: Views/Seller/Orders.cshtml
controller_action: Seller/Orders
route: /satici/siparis
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Siparişleri

**Bağlam (tek paragraf):** Satıcının siparişlerini yönettiği liste sayfası — Account/Orders'a benzer ama satıcı için inline aksiyon butonları ve durum filtreleri. Satıcı sidebar sabit.

## Tasarım istekleri
- Satıcı sidebar.
- Account/Orders'a benzer liste + satır inline button'lar: "Hazırlandı" / "Kargoya Ver" / "İptal Et".
- Filter chip: Bekleyen / Onaylandı / Hazırlanıyor / Kargoda / Tamamlandı / İptal / İade.

## Sample data ipucu
- 8-12 örnek sipariş, karışık durum; masked alıcı adları; model yok → ViewBag/inline.

## JS etkileşim ipucu
- Filter chip → liste filtrele; inline aksiyon butonları POST/HTMX; `siparisler.js`/`hesabim.js` pattern reuse.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

Seller/Orders
- Satıcı sidebar
- Account/Orders'a benzer ama satıcı için + satır inline button'lar: "Hazırlandı" / "Kargoya Ver" / "İptal Et"
- Filter chip: Bekleyen / Onaylandı / Hazırlanıyor / Kargoda / Tamamlandı / İptal / İade
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiSiparis.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
