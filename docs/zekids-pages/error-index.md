---
title: Hata Sayfası (404/500)
target_view: Views/Error/Index.cshtml
controller_action: Error/Index
route: /hata?code=...
model: None (ViewBag.StatusCode — 404, 500, 503)
status: pending
source_html: henüz tasarlanmadı
priority: high
---

# Hata Sayfası (404/500)

**Bağlam (tek paragraf):** Durum koduna göre içerik değiştiren hata sayfası. `ViewBag.StatusCode` (404/500/503) ile çocuk-marka uyumlu SVG illustration + mesaj + CTA. Ortalı card.

## Tasarım istekleri
- Ortalı `card py-20 max-w-2xl`.
- StatusCode'a göre:
  - **404:** SVG illustration (kaybolmuş ayıcık balon peşinde, Fraunces çocuk vibe, abartısız); H1 "Buralarda kimse yok"; "Aradığınız sayfa taşınmış olabilir."; 2 CTA "Anasayfa" primary + "Kataloğa Git" outline.
  - **500/503:** SVG illustration (ufak makine ayıcık tamir ediyor); H1 "Bir şeyler ters gitti"; "Hata kaydedildi, en kısa sürede çözeceğiz."; 2 CTA "Tekrar Dene" primary + "Anasayfa" outline.
- Altta arama kutusu kart: "Belki şunu mu arıyordunuz?" + input.

## Sample data ipucu
- Her iki state'i (404 ve 500/503) tasarımda göster; Razor `@switch(ViewBag.StatusCode)` ile blok seçer.

## JS etkileşim ipucu
- "Tekrar Dene" → `history.back()`/reload; arama kutusu `/arama?q=` GET.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

Error/Index — Hata sayfası
@ViewBag.StatusCode (404, 500, 503)
- Ortalı card py-20 max-w-2xl
- StatusCode'a göre:
  * **404**:
    - SVG illustration: kaybolmuş bir ayıcık balonun peşinden uçuyor (Fraunces çocuk vibe, abartısız)
    - H1 "Buralarda kimse yok"
    - Açıklama: "Aradığınız sayfa taşınmış olabilir."
    - 2 CTA: "Anasayfa" primary + "Kataloğa Git" outline
  * **500/503**:
    - SVG illustration: ufak makine ayıcık tamir ediyor
    - H1 "Bir şeyler ters gitti"
    - "Hata kaydedildi, en kısa sürede çözeceğiz."
    - 2 CTA: "Tekrar Dene" primary + "Anasayfa" outline
- Altta arama kutusu kart: "Belki şunu mu arıyordunuz?" + input

(404 ve 500/503 state'lerini ayrı ayrı tasarımda göster.)
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/Hata.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
