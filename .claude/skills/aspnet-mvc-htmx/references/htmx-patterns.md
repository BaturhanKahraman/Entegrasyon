# HTMX Patterns (Entegrasyon)

## Request Detection

| Helper | Header | Kullanim |
|---|---|---|
| `Request.IsHtmx()` | `HX-Request` | Tum HTMX istekleri |
| `Request.IsHtmxBoosted()` | `HX-Boosted` | `hx-boost="true"` ile gonderilen normal link/form |
| `Request.HtmxTarget()` | `HX-Target` | Hedef element id |
| `Request.HtmxTriggerName()` | `HX-Trigger-Name` | Tetikleyen input'un `name` veya `id`'si |

## Response Headers

```csharp
Response.HtmxRedirect("/products");         // Full-page nav (HX-Redirect)
Response.HtmxRefresh();                      // window.location.reload (HX-Refresh)
Response.HtmxTrigger("productSaved");        // DOM event
Response.HtmxTriggerWithData("productSaved", new { id = 5 }); // event + payload
Response.HtmxReswap("outerHTML");            // Default swap override
Response.HtmxRetarget("#main");              // Default target override
Response.HtmxPushUrl("/products/5");         // history.pushState
```

## Aynı Endpoint, İki Cevap

```csharp
[HttpGet("/products")]
public async Task<IActionResult> Index(...)
{
    var data = await service.GetPageable(...);

    if (Request.IsHtmx())
        return PartialView("Partials/_ProductTable", data);  // Sadece tablo

    return View(data);                                        // Full layout
}
```

## Inline Edit (Tablo Hücresi)

View:
```cshtml
<td hx-get="/products/@product.Id/edit-name" hx-trigger="dblclick" hx-swap="outerHTML">
    @product.Name
</td>
```

Controller `edit-name` action -> `<input name="Name" hx-post="/products/@id/save-name" hx-swap="outerHTML" />`. Save action gerekirse `Response.HtmxTrigger("notyf:success")` ile toast tetikler.

## Modal Aç/Kapat

```cshtml
<button class="btn btn-primary"
        hx-get="/products/@id/variant-add"
        hx-target="#modal-container"
        hx-swap="innerHTML">
    Varyant Ekle
</button>

<div id="modal-container"></div>
```

`variant-add` action -> `_VariantAddDialog.cshtml` partial (içinde Bootstrap modal). Form POST sonrası `Response.HtmxTriggerWithData("variantAdded", new { id })` + boş partial dön -> modal kendi kendine kapanır (JS listener), tablo `hx-trigger="variantAdded from:body"` ile yenilenir.

## Form Validation Akışı (AutoValidationFilter ile)

1. User submit -> `hx-post="/products/add"`, `hx-target="#form-container"`
2. ModelState invalid -> AutoValidationFilter `PartialView("Partials/_Form", vm)` döner (HX-Request true)
3. Form yerine yeni form swap edilir, hata mesajları input altında görünür
4. Geçerli ise controller normal akış -> `Response.HtmxRedirect("/products")` veya partial dön + toast trigger

## Wizard (Multi-Step)

`[SkipAutoValidation]` zorunlu — wizard kendi step validasyonunu yapar.

```csharp
[HttpPost("/products/add/step/{step:int}")]
[SkipAutoValidation]
public IActionResult Step(int step, CreateProductVm vm)
{
    if (step == 1 && string.IsNullOrEmpty(vm.Title))
    {
        ModelState.AddModelError(nameof(vm.Title), "Başlık zorunlu.");
        return PartialView($"Partials/_CreateStep{step}", vm);
    }
    return PartialView($"Partials/_CreateStep{step + 1}", vm);
}
```

## Antiforgery

`hx-boost` kullanan tüm POST formları + manuel HTMX form'lar:

```cshtml
<form method="post"> @* Form Tag Helper otomatik token ekler *@
    ...
</form>
```

Pure JS HTMX POST (no `<form>`): `<meta name="csrf-token" value="@antiforgery.GetAndStoreTokens(Context).RequestToken" />` + JS interceptor `htmx.on('htmx:configRequest', e => e.detail.headers['X-CSRF-TOKEN'] = ...)` ve `builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN")`.

## hx-trigger from:body (Global Event Bus)

```cshtml
<div id="product-table"
     hx-get="/products"
     hx-trigger="productSaved from:body, productDeleted from:body"
     hx-swap="outerHTML">
    @Html.Partial("Partials/_ProductTable", Model)
</div>
```

Herhangi bir endpoint `Response.HtmxTrigger("productSaved")` çağırınca tablo kendini yeniler.

## Boost vs Manual HTMX

- `hx-boost="true"` — `<a>`/`<form>` elemanlarını otomatik AJAX'a çevirir; tarayıcı history korunur, full layout swap edilir (HX-Boosted true).
- Manuel `hx-get`/`hx-post` — daha granular, target/swap kontrolü tam sende.

Storefront sayfalarında genelde root `<body hx-boost="true">`, partial swap'lerde manuel HTMX kullan.
