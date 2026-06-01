---
title: Satıcı Sipariş Detay
target_view: Views/Seller/OrderDetail.cshtml
controller_action: Seller/OrderDetail
route: /satici/siparis/{id}
model: Entegrasyon.Entity.Orders.Order
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Sipariş Detay

**Bağlam (tek paragraf):** Satıcının tek bir siparişi yönettiği detay sayfası — Account/OrderDetail'e benzer ama satıcı aksiyon butonları + masked müşteri bilgisi. `@model Entegrasyon.Entity.Orders.Order`. Satıcı sidebar sabit.

## Tasarım istekleri
- Satıcı sidebar.
- Account/OrderDetail'e benzer + üstte aksiyon button row: "Hazırlandı Olarak İşaretle" / "Kargo Bilgisi Gir" (modal: kargo firma Tom Select + takip no) / "Faturayı Görüntüle" / "İade Talebi Aç".
- Müşteri bilgisi masked: "Ayşe Y." + masked telefon "+90 5XX XXX XX 67".

## Sample data ipucu
- `@model Order` null'a karşı fallback: 2-3 satır kalem, masked müşteri, durum chip.

## JS etkileşim ipucu
- Kargo bilgisi modalı (Tom Select firma + takip no); aksiyon butonları POST/HTMX.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

Seller/OrderDetail (@model Entegrasyon.Entity.Orders.Order)
- Satıcı sidebar
- Account/OrderDetail'a benzer ama satıcı için + üstte aksiyon button row:
  "Hazırlandı Olarak İşaretle" / "Kargo Bilgisi Gir" (modal: kargo firma Tom Select + takip no) / "Faturayı Görüntüle" / "İade Talebi Aç"
- Müşteri bilgisi masked: "Ayşe Y." + masked telefon "+90 5XX XXX XX 67"
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiSiparisDetay.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
