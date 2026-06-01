---
title: İletişim
target_view: Views/Contact/Index.cshtml
controller_action: Contact/Index
route: /iletisim
model: None (Tenant'tan ContactPhone, WhatsApp, Email, Address, City, MapEmbedUrl)
status: pending
source_html: henüz tasarlanmadı
priority: high
---

# İletişim

**Bağlam (tek paragraf):** İletişim sayfası — sol tarafta iletişim formu, sağ tarafta telefon/WhatsApp/e-posta/adres bilgi kartları + Google Maps embed. Sample data tenant'tan: ContactPhone, WhatsApp, Email, Address, City, MapEmbedUrl.

## Tasarım istekleri
- Breadcrumb + H1 "Bize Ulaşın".
- 2 col grid (`md:grid-cols-2 gap-12`):
  - **Sol form** `card bg-white border rounded-2xl p-8`: ad-soyad, e-posta, telefon (IMask) `sm:grid-cols-2`; Konu Tom Select (Sipariş Sorusu / İade / Ürün Önerisi / Kurumsal / Diğer); Mesaj textarea (1000 char sayaç); KVKK onay checkbox + link; "Gönder" primary full.
  - **Sağ bilgi** stack `space-y-4`: Telefon kart (ikon + "0850 000 00 00" + "Hafta içi 09:00-18:00" muted); WhatsApp kart `bg-success/10` (ikon + "Aynı saatlerde anlık" + "WhatsApp'ta yaz" link); E-posta kart; Adres kart (ikon + adres + Google Maps embed `aspect-[16/9] rounded-2xl iframe`).

## Sample data ipucu
- Tenant alanları null'a karşı fallback: telefon "0850 000 00 00", WhatsApp aynı, email "destek@zekids.com", adres + örnek MapEmbedUrl.

## JS etkileşim ipucu
- Konu Tom Select (lib mevcut), telefon IMask, char counter.
- POST → PRG → TempData.SetSuccess; `auth.js`/form handler pattern reuse.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

Contact/Index — İletişim
Sample data: Tenant'tan ContactPhone, WhatsApp, Email, Address, City, MapEmbedUrl.
- Breadcrumb + H1 "Bize Ulaşın"
- 2 col grid (md:grid-cols-2 gap-12):
  - **Sol form** card bg-white border rounded-2xl p-8:
    * Ad-soyad, e-posta, telefon (IMask) sm:grid-cols-2
    * Konu Tom Select: "Sipariş Sorusu / İade / Ürün Önerisi / Kurumsal / Diğer"
    * Mesaj textarea (1000 char sayaç)
    * KVKK onay checkbox + link
    * "Gönder" primary full CTA
  - **Sağ bilgi** stack space-y-4:
    * Telefon kart: ikon + "0850 000 00 00" + "Hafta içi 09:00-18:00" muted
    * WhatsApp kart bg-success/10: ikon + "Aynı saatlerde anlık" + "WhatsApp'ta yaz" link
    * E-posta kart: ikon + email
    * Adres kart: ikon + adres + Google Maps embed altta (aspect-[16/9] rounded-2xl iframe)
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/Iletisim.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
