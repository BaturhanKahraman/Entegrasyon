---
title: CMS Statik Sayfa
target_view: Views/Page/Show.cshtml
controller_action: Page/Show
route: /sayfa/{slug}
model: None (ViewBag.Page — Title, ContentHtml, slug)
status: pending
source_html: henüz tasarlanmadı
priority: high
---

# CMS Statik Sayfa

**Bağlam (tek paragraf):** CMS'ten gelen statik içerik sayfası (KVKK, Hakkımızda, İade Politikası vb.). Prose tipografi ile `@Html.Raw` render. Slug "sss" ise SSS accordion görünümü. Ortalı max-w-4xl.

## Tasarım istekleri
- Ortalı `max-w-4xl py-12`.
- Breadcrumb.
- H1 Fraunces: `Page.Title`. Son güncelleme muted küçük.
- **Prose içerik** `article class="prose prose-lg max-w-none"`: `@Html.Raw(Page.ContentHtml)` — Tailwind typography ile p, h2, h3, ul, ol, blockquote, code; renk override `prose-headings:text-charcoal prose-headings:font-heading prose-a:text-primary prose-strong:text-charcoal`.
- Footer alt link kart: "Yardım gerekirse iletişim" → /iletisim.
- **SSS özel görünüm** (slug "sss"): soru-cevap accordion liste; her `details/summary` açılınca cevap fade-in.

## Sample data ipucu
- `ViewBag.Page` null'a karşı fallback: Title "Hakkımızda", örnek ContentHtml (h2 + p + ul). SSS varyantı için 5-6 soru-cevap.

## JS etkileşim ipucu
- SSS accordion `details/summary` native; fade-in CSS.
- Ekstra JS gerekmez.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

Page/Show — CMS statik
Sample data: KVKK / Hakkımızda / İade Politikası gibi.
- Ortalı max-w-4xl py-12
- Breadcrumb
- H1 Fraunces: Page.Title
- Son güncelleme muted küçük
- **Prose içerik** article class="prose prose-lg max-w-none":
  - @Html.Raw(Page.ContentHtml) — Tailwind typography ile p, h2, h3, ul, ol, blockquote, code stilleri
  - Renk overrides: prose-headings:text-charcoal prose-headings:font-heading prose-a:text-primary prose-strong:text-charcoal
- Footer alt link kart: "Yardım gerekirse iletişim" → /iletisim

SSS özel görünüm (Page slug "sss" ise):
- Soru-cevap accordion liste
- Her details/summary açılınca cevap fade-in
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/Sayfa.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
