---
name: tabler-ui
description: Use when building or editing any UI in the Entegrasyon project (MVC esnaf paneli, Admin, Storefront) — adding/styling a Tabler component (badge, button, card, alert, avatar, modal, table, form, dropdown, ribbon, status, steps, timeline, toast, navbar, etc.), choosing color classes, or whenever you'd otherwise guess Tabler class names. Reference for exact class combinations from docs.tabler.io.
---

# Tabler UI Reference

Bu proje **Tabler UI** (Bootstrap 5 tabanlı) kullanır. Bu skill, doğru class kombinasyonlarını `docs.tabler.io`'dan birebir çıkarılmış halde tutar — **class isimlerini tahmin etme, buradan bak.**

## Çekirdek Prensip

**Tabler bileşenlerinin class kombinasyonları sezgisel değildir.** En sık yapılan hata renk class'larıdır: tek başına `bg-green` okunabilir metin vermez. Doğru kombinasyon her zaman çifttir.

## En Kritik Kural — Renk Kombinasyonları

| Stil | Kalıp | Örnek |
|---|---|---|
| **Solid (dolu)** | `bg-COLOR` + `text-COLOR-fg` | `badge bg-green text-green-fg` |
| **Light (açık ton)** | `bg-COLOR-lt` (+ `text-COLOR-lt-fg`) | `badge bg-yellow-lt text-yellow-lt-fg` |

- ❌ `badge bg-green` (tek başına — yanlış)
- ✅ `badge bg-green text-green-fg` (solid)
- ✅ `badge bg-green-lt` (light)

**Renk paleti:** `blue azure indigo purple pink red orange yellow lime green teal cyan`
**Semantik (alert/progress/btn için):** `primary secondary success danger warning info dark light`
> Semantik renkler (`success/danger/...`) alert ve progress gibi bileşenlerde alias olarak kullanılır; isimli palet (`green/red/...`) `-fg`/`-lt` sistemini kullanır.

## Ne Zaman Kullanılır

- Yeni bir Razor view/partial'a herhangi bir Tabler bileşeni eklerken
- Mevcut UI'da renk/badge/buton/kart stilini değiştirirken
- Bir bileşenin doğru class'ını veya data-attribute'unu hatırlamadığında (tahmin etme — bak)
- Form, tablo, modal, dropdown, navbar gibi yapıları kurarken

## Referans Dosyaları (detay için aç)

| Dosya | İçerik |
|---|---|
| `base-layout.md` | Colors, Typography, Navbars, Navs & Tabs, Page headers, Page layouts |
| `components-display.md` | Alerts, Avatars, Badges, Cards, Empty states, Modals, Offcanvas, Toasts, Tooltips, Popovers, Placeholder, Spinners, Progress, Ribbons, Statuses |
| `components-nav.md` | Breadcrumb, Buttons, Dropdowns, Pagination, Steps, Tabs, Timelines, Divider, Segmented control, Switch icon, Tables |
| `components-interactive.md` | Autosize, Carousel, Charts (ApexCharts), Countup, Data grid, Dropzone, Icons, Inline player (Plyr), Range slider (noUiSlider), Tracking, Vector maps, WYSIWYG |
| `forms.md` | Form elements, Color/Image check, Fieldset, Helpers, Selectgroup, Input mask (IMask), Validation states |
| `utilities-extras.md` | Borders, Cursors, Interactions, Margins/spacing, Vertical align, Visually hidden, Flags, Payments, Social icons, Tabler Icons kullanımı, Illustrations, Emails |

## Hızlı Referans — En Çok Kullanılanlar

**Badge:** `badge bg-COLOR text-COLOR-fg` (solid) / `badge bg-COLOR-lt` (light). Pill: `+badge-pill`.
**Button:** `btn btn-primary` (solid semantik), `btn btn-outline-primary` (çerçeve), `btn btn-ghost-primary`, boyut `btn-sm`/`btn-lg`, ikon-only `btn-icon`, grup `btn-list`.
**Card:** `card` > `card-header` (+`card-title`) / `card-body` / `card-footer`. `card-table` ile tablo gömülür. Renk şeridi: `card` + `card-status-top bg-COLOR` veya `ribbon`.
**Table:** `table table-vcenter` (+`card-table` kart içinde). `.table-striped`/`.table-hover`/`.table-bordered` Bootstrap'tan gelir. Responsive: `<div class="table-responsive">` sar.
**Status (nokta+metin):** `status status-COLOR` > `status-dot`. Animasyonlu: `status-dot-animated`.
**Alert:** `alert alert-success` (light) — dolu renk için `+alert-important`. Kapanabilir: `+alert-dismissible` + `btn-close[data-bs-dismiss="alert"]`.
**Icon:** `<i class="ti ti-NAME"></i>` (webfont) — bkz. `utilities-extras.md` icon bölümü.
**JS bileşenleri** (modal/dropdown/tab/toast/offcanvas/tooltip/popover): Bootstrap `data-bs-toggle` / `data-bs-dismiss` / `data-bs-target` attribute'ları ile.

## Sık Yapılan Hatalar

| Hata | Doğrusu |
|---|---|
| `badge bg-green` | `badge bg-green text-green-fg` (solid) ya da `badge bg-green-lt` (light) |
| Avatar'ı `<img>` ile koymak | `<span class="avatar" style="background-image:url(...)">` |
| Alert'i "dolu" sanmak | Standart alert light'tır; dolu için `alert-important` |
| Tabler class'ı tahmin etmek | İlgili referans dosyasını aç, doğrula |
| Custom CSS override yazmak | Önce Tabler'ın önerdiği class kombinasyonu var mı bak |

## Çalışma Akışı (Strict Rule — proje CLAUDE.md ile uyumlu)

Herhangi bir Tabler bileşeni kullanmadan **ÖNCE** bu skill'in ilgili referans dosyasına bak. Class isimlerini tahmin etme; doğru kombinasyonu (özellikle renk: `bg-COLOR` + `text-COLOR-fg`) doğrula. CSS'te global override eklemek yerine Tabler'ın önerdiği class kombinasyonunu kullan. Bilgi burada yoksa `https://docs.tabler.io/ui/<section>/<slug>` adresinden çek ve gerekiyorsa bu referansı güncelle.
