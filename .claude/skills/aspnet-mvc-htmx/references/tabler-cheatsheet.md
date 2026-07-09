# Tabler UI Cheatsheet (Entegrasyon)

**Strict Rule:** Tabler bileşeni kullanmadan ÖNCE `https://tabler.io/docs/ui/<component>` aç. Class kombinasyonunu tahmin etme. Global CSS override yerine doğru kombinasyonu kullan.

## Badge

| İhtiyaç | Class |
|---|---|
| Solid renkli (yeşil yazı YEŞİL bg üstünde) | `badge bg-green text-green-fg` |
| Light variant (yeşil yazı açık yeşil bg üstünde) | `badge bg-green-lt` |
| Outline (border'lı) | `badge bg-green-lt text-green` |
| Notification dot | `badge bg-red badge-blink` |

Renkler: `red`, `pink`, `purple`, `indigo`, `blue`, `cyan`, `teal`, `green`, `lime`, `yellow`, `orange`, `gray`.

## Card

```cshtml
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Başlık</h3>
        <div class="card-actions">
            <button class="btn btn-primary">Eylem</button>
        </div>
    </div>
    <div class="card-body">
        İçerik
    </div>
    <div class="card-footer">
        Footer
    </div>
</div>
```

Variants: `card-stacked`, `card-active`, `card-borderless`, `card-status-start bg-red`.

## Alert

```cshtml
<div class="alert alert-warning alert-dismissible" role="alert">
    <h4 class="alert-title">Dikkat</h4>
    <div class="text-secondary">Mesaj burada.</div>
    <a class="btn-close" data-bs-dismiss="alert"></a>
</div>
```

Variants: `alert-success`, `alert-info`, `alert-warning`, `alert-danger`, `alert-important alert-warning` (sayfa üstü banner).

## Form Group

Standart input — `form-group` Tag Helper'ı kullan:
```cshtml
<form-group asp-for="Title" placeholder="Ürün adı" />
```

Manuel:
```cshtml
<div class="mb-3">
    <label class="form-label" asp-for="Title"></label>
    <input asp-for="Title" class="form-control" placeholder="..." />
    <span asp-validation-for="Title" class="text-danger"></span>
</div>
```

## Tabler Icons

```cshtml
<i class="ti ti-plus"></i>
<i class="ti ti-trash text-danger"></i>
```

Tüm ikonlar `ti ti-<name>`. Liste: https://tabler.io/icons

## Status Dot

```cshtml
<span class="status status-green">
    <span class="status-dot status-dot-animated"></span>
    Aktif
</span>
```

## Steps (Wizard)

```cshtml
<ul class="steps steps-counter my-4">
    <li class="step-item active">Bilgi</li>
    <li class="step-item">Özellikler</li>
    <li class="step-item">Varyantlar</li>
</ul>
```

`active` mevcut adım; `step-item` (önceki) tamamlandı görünür.

## Datagrid

```cshtml
<div class="datagrid">
    <div class="datagrid-item">
        <div class="datagrid-title">Stok Kodu</div>
        <div class="datagrid-content">@Model.StockCode</div>
    </div>
    <div class="datagrid-item">
        <div class="datagrid-title">Marka</div>
        <div class="datagrid-content">@Model.Brand</div>
    </div>
</div>
```

## Modal (Bootstrap 5)

```cshtml
<div class="modal modal-blur fade" id="my-modal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Başlık</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">İçerik</div>
            <div class="modal-footer">
                <button class="btn btn-link link-secondary" data-bs-dismiss="modal">Kapat</button>
                <button class="btn btn-primary">Kaydet</button>
            </div>
        </div>
    </div>
</div>
```

## Empty State

```cshtml
<div class="empty">
    <div class="empty-img">
        <img src="/static/illustrations/undraw_quitting_time_dm8t.svg" height="128" />
    </div>
    <p class="empty-title">Ürün bulunamadı</p>
    <p class="empty-subtitle text-secondary">Arama kriterlerini değiştirip tekrar deneyin.</p>
    <div class="empty-action">
        <a href="/products/add" class="btn btn-primary">Yeni Ürün</a>
    </div>
</div>
```

## Placeholder (Loading)

```cshtml
<div class="placeholder-glow">
    <div class="placeholder col-7"></div>
    <div class="placeholder col-4"></div>
</div>
```

HTMX `hx-indicator` ile birlikte kullan.

## Ribbon

```cshtml
<div class="card">
    <div class="ribbon bg-red">YENİ</div>
    <div class="card-body">...</div>
</div>
```

## Avatar

```cshtml
<span class="avatar avatar-rounded bg-blue-lt">BK</span>
<span class="avatar" style="background-image: url('/path.jpg')"></span>
```

## Color Tokens

Tabler 8 ana renk × 3 varyant (default, `-lt` light, `-fg` foreground). Custom renk eklemek yerine Tabler paletini kullan.

## Common Mistakes

| Yanlış | Doğru |
|---|---|
| `badge bg-green` (text görünmez) | `badge bg-green text-green-fg` (solid) veya `badge bg-green-lt` (light) |
| Custom CSS ile `.badge { color: white }` | Tabler doğru kombinasyonu kullan |
| `<button class="btn btn-primary btn-sm">` ikon yok | `<button class="btn btn-primary"><i class="ti ti-plus"></i> Ekle</button>` |
| `card` body olmadan | Mutlaka `card-body` veya `card-header` çocuk olmalı |
| Modal'da `data-dismiss` (Bootstrap 4) | `data-bs-dismiss` (Bootstrap 5) |

Daima docs: https://tabler.io/docs/ui/<component>
