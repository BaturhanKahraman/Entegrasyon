---
title: Satıcı Profili
target_view: Views/Seller/Profile.cshtml
controller_action: Seller/Profile
route: /satici/profil
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Profili

**Bağlam (tek paragraf):** Satıcının mağaza profilini düzenlediği form sayfası — satıcı sidebar + 2 kolon form. Sample data: mağaza adı, logo, banner, açıklama, kategori, kargo/iade politikası.

## Tasarım istekleri
- Satıcı sidebar + sağ form.
- H1 "Mağaza Profili".
- 2-col form: Banner upload (drag-drop, `aspect-[16/5]`); Logo upload (yuvarlak preview); Mağaza adı, slug; Kısa açıklama (280 char counter); Uzun açıklama (rich text basic); Kategoriler (Tom Select multi); Kargo politikası textarea; İade politikası textarea; İletişim (telefon, email, WhatsApp); Sosyal medya (Instagram, Facebook URL'leri).
- "Kaydet" primary.

## Sample data ipucu
- Mağaza "ZekidsBebe", örnek açıklama + politikalar; model yok → ViewBag/inline.

## JS etkileşim ipucu
- Drag-drop upload preview, Tom Select multi, char counter, basit rich text (bold/italic/list).

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı, sol SATICI SIDEBAR (Panel sayfasındaki ile aynı).

Seller/Profile
Sample data: mağaza adı, logo, banner, açıklama, kategori, kargo/iade politikası.
- Satıcı sidebar + sağ form
- H1 "Mağaza Profili"
- 2-col form:
  * Banner upload (drag-drop, aspect-[16/5])
  * Logo upload (yuvarlak preview)
  * Mağaza adı, slug
  * Kısa açıklama (280 char counter)
  * Uzun açıklama (rich text basic)
  * Kategoriler (Tom Select multi)
  * Kargo politikası textarea
  * İade politikası textarea
  * İletişim (telefon, email, WhatsApp)
  * Sosyal medya (Instagram, Facebook URL'leri)
- "Kaydet" primary
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiProfil.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
