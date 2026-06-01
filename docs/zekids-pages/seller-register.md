---
title: Satıcı Kayıt
target_view: Views/Seller/Register.cshtml
controller_action: Seller/Register
route: /satici-kayit
model: None (form-heavy)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Kayıt

**Bağlam (tek paragraf):** Marketplace satıcı başvuru sayfası — form-heavy. Firma, iletişim, mağaza, banka, kategoriler, belgeler bölümleri. Storefront chrome aynı; küçük ekip admin paneli hissi: minimal, fonksiyonel, kid-brand uyumlu. Sample data: boş form.

## Tasarım istekleri
- Ortalı `max-w-3xl py-12`.
- Hero sade: H1 "Satıcı Olun, Türkiye'ye Ulaşın" Fraunces + alt açıklama.
- 3 fayda chip row (`bg-primary/10 text-primary pill`): "Kolay Liste · Düşük Komisyon · Pazarlama Desteği".
- Form gruplar (accordion veya kart):
  - **Firma Bilgileri:** Firma adı, vergi no/TC, vergi dairesi, MERSİS, Şahıs/Limited radio.
  - **İletişim:** Yetkili ad-soyad, e-posta, telefon (IMask), KEP.
  - **Mağaza:** Mağaza adı (slug otomatik altta), logo upload (drag-drop preview), kısa açıklama textarea (280 karakter).
  - **Banka:** IBAN (IMask), hesap sahibi.
  - **Kategoriler:** Tom Select multi (Kız giyim, Erkek giyim, Bebek, Aksesuar, Ayakkabı).
  - **Belgeler:** Vergi levhası upload, imza sirküleri upload, ticaret sicil upload.
  - "Satıcı Sözleşmesi'ni okudum" checkbox link → modal.
- "Başvuruyu Gönder" primary full.

## Sample data ipucu
- Boş form; model yok. Slug, mağaza adı input'undan canlı türetilir.

## JS etkileşim ipucu
- IMask (telefon/IBAN), Tom Select (kategori), drag-drop upload preview, char counter, slug live-generate, sözleşme modal.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı. Küçük bir ekibin küçük admin paneli — minimal, fonksiyonel, kid-brand'a uygun.

Seller/Register — Satıcı başvurusu (form-heavy)
Sample data: Boş form.
- Ortalı max-w-3xl py-12
- Hero sade: H1 "Satıcı Olun, Türkiye'ye Ulaşın" Fraunces + alt açıklama
- 3 fayda chip row (bg-primary/10 text-primary pill): "Kolay Liste · Düşük Komisyon · Pazarlama Desteği"
- Form gruplar (accordion veya kart):
  * **Firma Bilgileri**: Firma adı, vergi no/TC, vergi dairesi, MERSİS, Şahıs/Limited radio
  * **İletişim**: Yetkili ad-soyad, e-posta, telefon (IMask), KEP
  * **Mağaza**: Mağaza adı (slug otomatik altta gösterilir), logo upload (drag-drop preview), kısa açıklama textarea (280 karakter)
  * **Banka**: IBAN (IMask), hesap sahibi
  * **Kategoriler**: Tom Select multi (Kız giyim, Erkek giyim, Bebek, Aksesuar, Ayakkabı)
  * **Belgeler**: Vergi levhası upload, imza sirküleri upload, ticaret sicil upload
  * "Satıcı Sözleşmesi'ni okudum" checkbox link → modal
- "Başvuruyu Gönder" primary full
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiKayit.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
