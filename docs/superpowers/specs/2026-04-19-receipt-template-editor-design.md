# Fiş Şablonu Editörü + Logo + A4 Desteği

**Tarih:** 2026-04-19
**Durum:** Onaylandı (tasarım)

## Problem

2026-04-19'da implement edilen POS fişi (`Print.cshtml`) **sabit şablon** — mağaza logosu, adı, iade kodu, toplamlar, teşekkür mesajı hep aynı sırada ve düzenlenemez. Çoklu müşteri kullanımında her tenant kendi görünümünü istiyor, ve büyük yazıcıda basmak için **A4 formatı** da gerekiyor. Ayrıca kasiyer/patron — teknik bilgi gerektirmeyen, **aptala yönelik** bir editör bekliyor: sürükle-bırak ile sıralama, bir kaç tıkla blok ekleme/çıkarma, logo upload.

## Amaç

- Mağaza sahibine tenant başına fiş şablonu tanımlama imkanı ver — drag-drop ile blok sıralama, blok aç/kapa, metin düzenleme, logo yükleme.
- İki çıktı formatı destekle: **Thermal (300px tek sütun)** ve **A4 (5-zone grid)**.
- İki mod: **Normal fiş** ve **Hediye fişi** — her blokta `showInNormal` / `showInGift` toggle.
- Mevcut `Print.cshtml` davranışı kesintisiz kalır — yeni `IReceiptRenderer` servisi arkada devreye girer.

Fiş **hala fatura değil** — mağaza-içi belge.

## Karar Özeti

| Başlık | Karar |
|---|---|
| Editör ambisyonu | **A** — blok listesi + SortableJS drag-drop + blok settings (metin, toggle). Gerçek pixel-canvas değil. |
| A4 layout | **B (5-zone)** — Header-Left, Header-Right, Body, Footer-Left, Footer-Right. Grid CSS. Bloklar zone'lar arası sürüklenebilir. |
| Kapsam | **Tenant-wide tek şablon** — `BranchOfficeId` kullanılmaz, tek satırlık `ReceiptTemplate` tablosu (Id=1). |
| Blok tipleri | **11 blok** — logo, store_info, text, divider, meta, items, totals, payments, vat_summary, return_code, spacer. QR/Barcode/Signature/MultiCol kapsam dışı. |
| Storage | Yeni entity `ReceiptTemplate` (JSON kolonları) — `ApplicationSetting` yerine ayrı tablo (loglama + future-proof). |
| Render | Yeni `IReceiptRenderer` servisi — template'ı parse edip HTML döndürür. `SaleController.Print` bunu `ViewBag.RenderedHtml` ile view'a geçirir. |
| Save modeli | Direkt save (draft/publish yok). |
| Preview | Canlı sağ panel — thermal/a4 + normal/gift toggle, sabit placeholder Sale verisi. |
| Seed | İlk migration + default template (mevcut `Print.cshtml` görünümüne yakın). |
| Logo | MinIO üzerinden (`IImageManager`), tenant başına tek logo. Max 500KB, png/jpg/webp, 400px max width. |

## Veri Modeli

### Entity `ReceiptTemplate`

```csharp
public sealed class ReceiptTemplate : BaseEntity
{
    public int Id { get; set; }                      // singleton (Id = 1)
    public string ThermalJson { get; set; } = "";    // JSON array of blocks
    public string A4Json { get; set; } = "";         // JSON: { hl, hr, body, fl, fr } each a block array
    public string? LogoUrl { get; set; }             // MinIO public URL
    public int LogoWidthPx { get; set; } = 120;      // 80-200 slider
    public string StoreName { get; set; } = "";      // denormalize
    public string StoreAddress { get; set; } = "";
    public string StorePhone { get; set; } = "";
}
```

- EF configuration: `HasIndex(x => x.Id)`, JSON kolonlar `jsonb` (PostgreSQL).
- Migration adı: `AddReceiptTemplateTable`.
- Ayrı seed migration: `SeedDefaultReceiptTemplate` (tek satır `Id=1`, default blok dizilimi).

### Blok JSON şeması

```jsonc
{
  "id": "block-uuid",
  "type": "logo|store_info|text|divider|meta|items|totals|payments|vat_summary|return_code|spacer",
  "showInNormal": true,
  "showInGift": true,
  "settings": { /* type-specific */ }
}
```

**Type-specific settings:**

| Type | Settings |
|---|---|
| `logo` | `{}` — logo kendisi template-level (LogoUrl, LogoWidthPx). Blok sadece konumu temsil eder. |
| `store_info` | `{ align: "left\|center\|right", size: "s\|m\|l" }` — content template.StoreName + Address + Phone'dan otomatik. |
| `text` | `{ content: string, align: "left\|center\|right", bold: bool, size: "s\|m\|l" }` |
| `divider` | `{ style: "solid\|dashed\|dotted", color: "black\|gray" }` |
| `meta` | `{ showSaleNumber: bool, showDate: bool, showCashier: bool, showCustomer: bool }` |
| `items` | `{ showBarcode: bool, showVatColumn: bool }` |
| `totals` | `{}` (hediye modunda otomatik gizlenir) |
| `payments` | `{}` (hediye modunda otomatik gizlenir) |
| `vat_summary` | `{}` (hediye modunda otomatik gizlenir) |
| `return_code` | `{ showLabel: bool }` (default true = "İade Kodu" etiketi) |
| `spacer` | `{ size: "s\|m\|l" }` |

**Hediye modu default'ları:** `totals`, `payments`, `vat_summary` blokları seed template'te `showInGift: false`. Kullanıcı istiyorsa aksini seçer.

### A4 JSON yapısı

```jsonc
{
  "hl":   [ /* header-left bloks */ ],
  "hr":   [ /* header-right blocks */ ],
  "body": [ /* body blocks (full width) */ ],
  "fl":   [ /* footer-left blocks */ ],
  "fr":   [ /* footer-right blocks */ ]
}
```

## Business Katmanı

### IReceiptTemplateManager

```csharp
Task<IDataResult<ReceiptTemplateDto>> GetAsync();
Task<IResult> UpdateAsync(UpdateReceiptTemplateDto dto);
Task<IDataResult<string>> UploadLogoAsync(IFormFile file);
Task<IResult> DeleteLogoAsync();
```

- `GetAsync` her zaman Id=1 satırını getirir (singleton).
- `UpdateAsync` JSON şeması validator'dan geçirir (geçersizse ErrorResult).
- `UploadLogoAsync` → `IImageManager` üzerinden MinIO'ya upload, `tenant-{id}/receipt-logo-{timestamp}.{ext}` path. Eski logo varsa delete. Döner: public URL.

### IReceiptRenderer

```csharp
public enum ReceiptMode { Normal, Gift }
public enum ReceiptSize { Thermal, A4 }

public interface IReceiptRenderer
{
    Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size);
}
```

**Akış:**
1. `IReceiptTemplateManager.GetAsync()` — template'ı al. Hot-path cache (`IMemoryCache`, 5 dk TTL) — template sık değişmez.
2. `size` → uygun JSON'ı seç (Thermal = block array, A4 = zones dict).
3. Bloklar üzerinde iterate:
   - `mode` toggle kontrolü (showInNormal / showInGift) — false ise skip.
   - Blok tipine göre `IBlockRenderer` dispatch (pattern match veya dict lookup).
4. Thermal = tek HTML string (bloklar ardı ardına).
5. A4 = 5-zone grid HTML (`<div class="receipt-a4 grid">` içinde `hl`, `hr`, `body`, `fl`, `fr` div'leri).
6. HTML string döndür (CSS dahil değil, view onu ekler).

**IBlockRenderer pattern:**
```csharp
internal static class BlockRenderers
{
    public static string Render(ReceiptBlock block, SaleDetailDto sale, ReceiptTemplate template, ReceiptMode mode)
        => block.Type switch
        {
            "logo" => RenderLogo(template),
            "text" => RenderText(block.Settings),
            "divider" => RenderDivider(block.Settings),
            "meta" => RenderMeta(block.Settings, sale),
            "items" => RenderItems(block.Settings, sale, mode),
            // ... vb.
            _ => ""
        };
}
```

Her renderer küçük string-build fonksiyonu — Razor partial değil (performance + kontrol).

## MVC Katmanı

### ReceiptTemplateController (`/settings/receipt-template`)

| Route | Method | Açıklama |
|---|---|---|
| `/settings/receipt-template` | GET | Editör sayfasını aç. Model = `ReceiptTemplateDto` + placeholder `SaleDetailDto`. |
| `/settings/receipt-template` | POST | Template kaydet. Form: `thermalJson`, `a4Json`, `logoWidthPx`, `storeName`, `storeAddress`, `storePhone`. |
| `/settings/receipt-template/upload-logo` | POST | `IFormFile file` — MinIO'ya upload, URL döner. |
| `/settings/receipt-template/logo` | DELETE | Logo sil. |
| `/settings/receipt-template/preview` | POST | `{ size, mode, json }` → placeholder Sale ile render edilmiş HTML partial döner. Live preview için. |

Auth: `[Authorize(Roles = "Admin")]`.

### SaleController.Print güncelleme

Mevcut:
```csharp
public async Task<IActionResult> Print(Guid id, string mode = "normal")
{
    var result = await saleManager.GetSaleDetailAsync(id);
    ViewBag.GiftMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase);
    return View(result.Data);
}
```

Yeni:
```csharp
public async Task<IActionResult> Print(Guid id, string mode = "normal", string size = "thermal")
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success || result.Data is null) return NotFound();

    var rMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase) ? ReceiptMode.Gift : ReceiptMode.Normal;
    var rSize = string.Equals(size, "a4", StringComparison.OrdinalIgnoreCase) ? ReceiptSize.A4 : ReceiptSize.Thermal;

    ViewBag.RenderedHtml = await receiptRenderer.RenderAsync(result.Data, rMode, rSize);
    ViewBag.ReceiptSize = rSize;
    return View(result.Data);
}
```

`Print.cshtml` basitleşir — sadece `<style>` (thermal.css veya a4.css) + `@Html.Raw(ViewBag.RenderedHtml)` + auto-print script.

### Satış detay + POS modal güncellemeleri

- `SaleDetail.cshtml` — mevcut "Fişi Yazdır"/"Hediye Fişi" butonları **dropdown**'a dönüşür: her biri `Thermal` ve `A4` alt seçeneği sunar. 4 link: normal-thermal, normal-a4, gift-thermal, gift-a4.
- `_POSLastSaleModal.cshtml` — "Fişi Yazdır" butonu varsayılan thermal'da; yanına küçük `[A4]` toggle.

## Editör UI

### Sayfa yapısı (`/settings/receipt-template`)

Tabler layout, üç kolon:

```
┌─────────────────────────────────────────────────────────────┐
│ Page header: "Fiş Şablonu"  [Sakla] butonu                  │
│ Size tabs: [Thermal] [A4]                                   │
│ Mode toggle: ○ Normal ● Hediye (sadece preview için)        │
├──────────┬──────────────────────────┬───────────────────────┤
│ Palette  │  Editor Zone(s)          │  Preview              │
│ (3 col)  │  (5 col)                 │  (4 col)              │
├──────────┼──────────────────────────┼───────────────────────┤
│ [+Logo]  │  Thermal: tek liste      │  [Thermal frame]      │
│ [+Metin] │  A4: 5-zone grid         │  Live HTML render     │
│ [+Ayır.] │                          │                       │
│ ...      │  Bloklar SortableJS ile  │                       │
│          │  dikey sıralanır.        │                       │
│ Logo     │                          │                       │
│ upload   │  Her blokta:             │                       │
│ card     │  - Tip adı + özet        │                       │
│          │  - N/H toggle ikonları   │                       │
│          │  - [⚙] düzenle (offcan.) │                       │
│ Store    │  - [✕] kaldır            │                       │
│ info     │                          │                       │
│ form     │                          │                       │
└──────────┴──────────────────────────┴───────────────────────┘
```

### Blok ekleme

Sol palettedeki buton tıklanınca JS:
1. Default settings ile yeni blok objesi oluştur (`crypto.randomUUID()` id).
2. Aktif zone'un (Thermal'da: body; A4'te: kullanıcı son tıkladığı zone veya "body") bloklar dizisine push.
3. Editörü ve preview'i re-render.
4. Yeni bloğu animasyonla vurgula + scroll.

### Blok düzenleme

Bloğun `[⚙]` ikonuna tıklayınca Tabler offcanvas sağdan açılır. İçerik blok tipine göre form:
- `text` → content (textarea), align radio, bold checkbox, size radio (S/M/L).
- `meta` → 4 checkbox.
- `items` → 2 checkbox.
- `divider` → style radio, color radio.
- `return_code` → showLabel checkbox.
- `spacer` → size radio.
- `logo` / `totals` / `payments` / `vat_summary` → sadece "bu blokta ek ayar yok" mesajı + N/H toggle.

Offcanvas her alan değişiminde JS state'i günceller; "Kapat" tıklayınca preview refresh.

### Drag-drop

**SortableJS** her zone'a bağlanır:
- Thermal: tek `<div id="zone-body">` — tüm bloklar burada.
- A4: 5 farklı `<div id="zone-hl/hr/body/fl/fr">` — hepsi `group: 'a4-blocks'` ile aynı gruba bağlı, bloklar zone'lar arası taşınabilir.

`onEnd` callback → state güncelle → preview refresh.

### Logo upload

Sol palettede "Logo" card'ı:
- Drag-drop alanı veya `<input type="file" accept="image/png,image/jpeg,image/webp">`.
- Seçim sonrası: anlık POST `/settings/receipt-template/upload-logo` — progress bar, başarı ikonu.
- Response'taki URL'i state'e yaz, preview'daki logo bloğunu yeni URL ile refresh et.
- Logo varsa "Logoyu Sil" butonu altta.
- Logo genişlik slider (80-200px, default 120) — değişince preview canlı güncellenir.

### Mağaza bilgileri formu

Sol palettede en altta:
- Input: Mağaza Adı (max 100).
- Textarea: Adres (max 200).
- Input: Telefon (max 20).

Bu üç değer state'te yaşar; "Sakla" ile birlikte kaydedilir. **Template'te "StoreName text bloğu" YOK** — bu 3 alan logo/header bölgesinde "store info" slot'u olarak render edilir (A4'te header-right default, thermal'da logo'nun altında).

**İsterse kullanıcı ayrı `text` blokları ekleyip daha fazla özelleştirme yapabilir** (örn. "Web sitemiz: ..."). Bunlar yukarıdaki 3-field'dan bağımsız.

### Canlı preview

Sağ panelde `<iframe>` yok — `<div class="receipt-preview">` doğrudan. Scoped CSS için wrapper class (`.receipt-preview-thermal`, `.receipt-preview-a4`).

Preview 500ms debounce ile update:
- `POST /settings/receipt-template/preview` → `{ size, mode, json }`.
- Server response = HTML partial (aynı `IReceiptRenderer` + placeholder Sale).
- JS `innerHTML` ile swap.

Placeholder Sale (server-side fix):
```csharp
new SaleDetailDto
{
    Id = Guid.Parse("11111111-..."),
    SaleNumber = "S20260419-0042",
    ReturnCode = "R-7K4QN9XM2P9",
    SaleDate = DateTimeOffset.Now,
    CustomerName = "Ahmet Yılmaz",
    SalePersonName = "Kasiyer Demo",
    BranchOfficeName = "Merkez Şube",
    SubTotal = 250m, VatTotal = 50m, GrandTotal = 300m,
    Items = [ ... 2-3 örnek satır ... ],
    Payments = [ ... nakit + kart örneği ... ],
    VatSummary = [ ... ],
    Returns = []
}
```

### Save

Alt bar'da tek [Sakla] butonu. Tıklama:
1. JS state'i JSON'a serialize et.
2. POST form:
   ```
   thermalJson=<stringify>
   a4Json=<stringify>
   logoWidthPx=<int>
   storeName=<str>
   storeAddress=<str>
   storePhone=<str>
   ```
3. Başarı → toast "Şablon kaydedildi."
4. Cache invalidate (server tarafı `IMemoryCache.Remove("receipt_template")`).

## CSS

İki ayrı dosya:
- `wwwroot/css/receipt-thermal.css` — 300px max-width, monospace, 12px font, mevcut Print.cshtml stilinin rafine hali.
- `wwwroot/css/receipt-a4.css` — A4 page size (`@page { size: A4; margin: 20mm; }`), 11pt serif font, 5-zone grid (`grid-template-areas: 'hl hr' 'body body' 'fl fr'`; `grid-template-columns: 1fr 1fr`).

Print.cshtml'de `size` parametresine göre uygun `<link rel="stylesheet">` yüklenir.

Editör preview sağ paneli aynı CSS'leri kullanır ama parent selector ile scope'lanır (`.receipt-preview-wrapper .receipt-thermal { ... }`).

## Default Seed

Migration `SeedDefaultReceiptTemplate` satırı (Id=1):

**Thermal default blokları (sırayla):**
1. logo
2. store_info (align: center, size: m)
3. divider (dashed)
4. meta (Sale No + Tarih + Kasiyer + Müşteri)
5. divider
6. items
7. divider
8. totals (showInGift: false)
9. payments (showInGift: false)
10. vat_summary (showInGift: false)
11. divider
12. return_code
13. text — `{ content: "Teşekkür ederiz!", align: center, size: "m" }`

**A4 (5-zone):**
- **hl:** logo
- **hr:** store_info (align: left, size: m)
- **body:** divider, meta, items, totals(normal), vat_summary(normal)
- **fl:** payments(normal), divider, return_code
- **fr:** text "Teşekkür ederiz!", text "Müşteri imzası: _______________"

**`store_info` bloğu:**
- Content otomatik: `template.StoreName + template.StoreAddress + template.StorePhone` dikey metin.
- Kullanıcı `align` ve `size` ayarlayabilir; konumunu da drag-drop ile değiştirebilir.
- StoreName/Address/Phone hepsi boşsa blok render edilmez.
- Ek özelleştirme isterse kullanıcı ayrı `text` blokları ekleyip daha fazla satır yazar (örn. "Web sitemiz: ...").

## Kapsam Dışı

- Şube başına override (Q4 — A seçildi).
- QR / Barkod / İmza (kapsam dışı, ileride).
- Çoklu sütun / iç içe grid (B yerine sabit 5-zone).
- Draft → Publish workflow.
- Template versioning / undo stack.
- Birden fazla preview sample.
- PDF export.
- Tarihçe: kim ne zaman değiştirdi.
- Logo crop/rotate editor — upload edildiği gibi kullanılır.

## Test Planı

### Unit (`Test/Entegrasyon.Test/`)

- `ReceiptTemplateJsonValidatorTests` — 10+ blok tipi için valid/invalid cases; bilinmeyen tip reddi; required fields eksik reddi.
- `BlockRendererTests` — her blok tipi için snapshot HTML (örneğin `RenderText_WithBold_ProducesStrongTag`).
- `ReceiptRendererTests` — mode toggle (totals gift'te gizlenir); size ayrımı (thermal tek sütun, a4 grid).

### Integration (`Test/Entegrasyon.IntegrationTest/`)

- `ReceiptTemplateEndpointTests` — GET/POST roundtrip; logo upload; preview endpoint (placeholder sale ile render).
- `PrintWithCustomTemplateTests` — template değiştir → Print output değişir.

### MVC (`Test/Entegrasyon.MVC.Test/`)

- `SaleControllerPrintSizeTests` — `?size=a4` → A4 CSS yüklenir.

## Migration Sırası

1. `AddReceiptTemplateTable` — entity + index.
2. `SeedDefaultReceiptTemplate` — tek satır Id=1 default JSON.
3. Mevcut Print.cshtml silinmiyor — yeni renderer devreye girdikten sonra test edilip confirmation ile temizlenir (son task).

## Sonraki Plan

Plan dokümanı bu spec üzerinden oluşturulur — tahmini 16 task, 5-8 gün effort. Detaylar `writing-plans` skill'i ile üretilecek.
