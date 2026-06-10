# Ürün 360° — Tasarım Teslimi (Designer)

İlk teslim. Statik prototip ile doğrulandı (gerçek Tabler asset'leri, headless Chromium screenshot).
Onaydan sonra SWE-Ahmet `razor/` altındaki partial'ları `Features/Products/Views/Partials/`'e taşır.

## Görseller
- `shot-full.png` — e-ticaret aktif (4 pazaryeri kartı + Aktivite timeline + filtreler)
- `shot-empty.png` — **pasif/boş durum** (esnaf e-ticaret kullanmıyor → zarif boş durum)
- `shot-full-mobile.png` — responsive (390px, kartlar stack)

## Prototip (tarayıcıda aç)
- `product-360-full.html`, `product-360-empty.html`

## Razor partial referansları (`razor/`)
| Dosya | Karşılığı |
|---|---|
| `VIEWMODEL-CONTRACT.md` | SWE ile alan-adı sözleşmesi (gerçek entity'lerle hizalı) |
| `_Product360.cshtml` | Sekme iskeleti + **pasif boş durum** + HTMX lazy sekmeler |
| `_MarketplaceStatusCards.cshtml` | Bölüm A — pazaryeri durum kartları |
| `_ActivityFilterBar.cshtml` | Filtre barı (Tom Select + Flatpickr + status segmented) |
| `_ActivityTimeline.cshtml` | Bölüm C — gün-gruplu timeline + accordion + pagination |
| `_ProductOrders.cshtml` | Bölüm D — sipariş tablosu |
| `_ProductStockMovements.cshtml` | Bölüm E — variant-bazlı stok hareketleri |

> `_ProductGeneralInfo.cshtml` = mevcut `Detail.cshtml`'deki datagrid içeriği; SWE extract eder.

## site.css'e eklenecek (prototipte inline — global override değil, küçük etkileşim helper'ları)
```css
.mp-card{transition:transform .12s ease, box-shadow .12s ease;cursor:pointer}
.mp-card:hover{transform:translateY(-2px);box-shadow:0 .5rem 1rem rgba(0,0,0,.08)}
.mp-card.is-active{box-shadow:0 0 0 2px var(--tblr-primary)}
.tl-detail{display:none}
.tl-detail.show{display:block}
```

## Tabler strict-rule doğrulaması
Kullanılan tüm sınıflar yerel `tabler.min.css`'te grep ile doğrulandı:
`empty/empty-icon/empty-title/empty-subtitle/empty-action`, `timeline/timeline-event/timeline-event-icon/timeline-event-card`,
`card-status-start`, `status-dot/status-dot-animated`, `nav-tabs/card-header-tabs/tab-pane`, `bg-*-lt` badge'ler,
`hr-text`, `avatar avatar-sm/xs`, `datagrid`. Renkli badge'lerde `bg-X-lt` (light) varyantı kullanıldı.
