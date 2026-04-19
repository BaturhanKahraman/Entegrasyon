# POS Fişi ve İade Kodu Sistemi

**Tarih:** 2026-04-19
**Durum:** Onaylandı (tasarım)

## Problem

POS/Kasa satışı tamamlandığında şu an müşteriye verilebilecek fiziksel bir döküm otomatik olarak üretilmiyor. Mevcut `Print.cshtml` endpoint'i (`/sales/sale/{id}/print`) var ama POS akışı bitince açılmıyor — kasiyer manuel olarak satış detayına gidip basmak zorunda. Ayrıca:

- Satışın tek kimliği olan `Sale.SaleNumber` günlük sıralı (`S20260419NNNN`) → tahmin edilebilir. Hediye iade senaryosunda (fiyat görünmez, kod eşleşmesi ile iade) bu bir risk.
- Hediye satışı için fiyatı gizleyen bir fiş formatı yok.
- `/returns` ekranında `search` parametresi tanımlı ama fiilen kullanılmıyor (ReturnController.cs:52). Müşteri elinde fişle geldiğinde kasiyerin doğrudan o satıştan iade başlatabileceği bir akış yok — önce satışı bulup `/sales/sale/{id}` detayına gitmesi gerekiyor.

## Amaç

- POS satışı sonrası kasiyere fiş basma akışı sun (normal / hediye / atla).
- Her satışta, `SaleNumber`'dan bağımsız, tahmin edilemez bir `ReturnCode` üret — müşteriye bu gösterilir.
- Hediye fişi modunda fiyat bilgilerini gizle, iade kodunu koru.
- İade ekranında iki ayrı akış: "mevcut iadelerde ara" ve "fiş kodundan yeni iade başlat".

Bu belge bir **fatura/e-arşiv değildir** — mağaza içi, kullanıcının dükkanı için geçerli, iade/takip amaçlı iç belgedir.

## Karar Özeti

| Başlık | Karar |
|---|---|
| İade kodu | Mevcut `SaleNumber` korunur, ayrıca yeni `Sale.ReturnCode` kolonu eklenir. Crockford Base32, 13 karakter, `R-` prefix'li. |
| Hediye modu | Sale entity'sinde `IsGift` **yok**. Print view `?mode=gift` query parametresi alır, fiyat bloklarını gizler. |
| Satış sonrası akış | `TempData` üzerinden `/pos` sayfasına "fiş modu seç" modal'ı aç; [Normal] / [Hediye] / [Atla]. |
| İade araması | `/returns` Index üstünde iki input: "Mevcut iadelerde ara" ve "Yeni iade başlat" (kod ile). |
| Eski satış backfill | Yapılmaz. `ReturnCode` nullable, yeni satışlar doldurur, arama her iki kodu da dener. |

## Veri Modeli

### Sale entity güncellemesi

```csharp
public sealed class Sale : BaseEntity
{
    // ... mevcut alanlar ...

    [StringLength(16)]
    public string? ReturnCode { get; set; }
}
```

- Nullable (eski satışlar null kalır).
- EF configuration: **filtered unique index** (`WHERE "ReturnCode" IS NOT NULL`).
- Migration adı: `AddSaleReturnCode`.

### ReturnCode üretimi

- `System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)` → 64 bit.
- Crockford Base32 alfabe (`0123456789ABCDEFGHJKMNPQRSTVWXYZ` — `I`, `L`, `O`, `U` hariç) ile encode → 13 karakter.
- Format: `R-XXXXXXXXXXXXX` (toplam 15 karakter, kolon 16 için yeterli).
- Collision: `GenerateReturnCodeAsync` üretir, DB'de `AnyAsync(s => s.ReturnCode == code)` kontrolü, çakışırsa retry (max 5). Pratik olasılık 64-bit'te sıfıra yakın ama defensive.

Utility konumu: `Application/Entegrasyon.Business/Utilities/ReturnCodeGenerator.cs` — `ICollisionChecker` bağımlılığı yok, `Func<string, Task<bool>>` callback ile çağrılır (test edilebilir, utility saflığı korunur, feedback kurallarıyla uyumlu).

## Business Katmanı

### SaleManager.MakeSale

```csharp
sale.SaleNumber = await GenerateSaleNumberAsync(dbContext);
sale.ReturnCode = await GenerateReturnCodeAsync(dbContext);
sale.SaleDate = DateTimeOffset.UtcNow;
```

`GenerateReturnCodeAsync` private method; `ReturnCodeGenerator.Generate()` ile üret + DB lookup callback ile çakışma kontrolü.

### ISaleManager.GetSaleByCodeAsync

Yeni method:

```csharp
Task<IDataResult<SaleDetailDto>> GetSaleByCodeAsync(string code);
```

Davranış:
- `code` trim edilir, upper-case'e çevrilir.
- Önce `ReturnCode == code` ile ara (kod `R-` ile başlıyorsa bu kesin yol).
- Bulunamazsa `SaleNumber == code` ile ara (kod `S` ile başlıyorsa bu kesin yol).
- Bulunamazsa `ErrorDataResult("Satış bulunamadı.")`.
- Bulundu: `GetSaleDetailAsync` ile aynı DTO'yu döndür.

## POS Akışı (Fiş Teslimi)

### CompleteSale sonrası TempData

`POSController.CompleteSale` — satış başarılı olduktan sonra (`AddTransactionRecordAsync`'ten sonra, sepet temizliğinden önce):

```csharp
TempData["pos_last_sale"] = System.Text.Json.JsonSerializer.Serialize(new
{
    saleId = saleResult.Data,
    saleNumber = saleDetailForToast.SaleNumber,
    returnCode = saleDetailForToast.ReturnCode
});
```

`saleDetailForToast` için `saleManager.GetSaleDetailAsync(saleResult.Data)` çağrısı gerekir — veya daha hafif: `MakeSale` dönüş tipi genişletilebilir (`IDataResult<Guid>` yerine `IDataResult<SaleCreatedDto>` — `{ SaleId, SaleNumber, ReturnCode }`). İkinci yol daha temiz, ek roundtrip yok.

Karar: `MakeSale` dönüş tipini `IDataResult<SaleCreatedDto>`'ya güncelle (recent commit `CreateReturnAsync`'in aynı pattern'i izlediği görülüyor — proje yönü bu).

### /pos sayfası

`Views/Index.cshtml` — sayfanın altına:

```razor
@{
    var lastSaleJson = TempData["pos_last_sale"] as string;
}
@if (!string.IsNullOrEmpty(lastSaleJson))
{
    <div id="pos-last-sale-data" data-last-sale='@lastSaleJson' class="d-none"></div>
    @await Html.PartialAsync("Partials/_POSLastSaleModal")
}
```

`_POSLastSaleModal.cshtml` — Tabler modal şablonu, boş data-bound iskelet. JS modal'a data-last-sale'ı okur, DOM'a yazar, açar.

`pos.js` (mevcut JS dosyası veya yeni eklenen script bloğu):

```javascript
document.addEventListener('DOMContentLoaded', () => {
    const dataEl = document.getElementById('pos-last-sale-data');
    if (!dataEl) return;
    const { saleId, saleNumber, returnCode } = JSON.parse(dataEl.dataset.lastSale);
    // modal içini doldur, aç
    // [Normal Fiş] -> window.open(`/sales/sale/${saleId}/print?mode=normal`, '_blank')
    // [Hediye Fişi] -> window.open(`/sales/sale/${saleId}/print?mode=gift`, '_blank')
    // [Atla] -> modal.close()
});
```

### Modal içeriği

Tabler modal (`modal-md`):
- Header: "Satış Tamamlandı"
- Body:
  - Büyük yeşil ikonlu başarı ikonu
  - `Fiş No: <saleNumber>` (küçük puntoyla)
  - `İade Kodu: <returnCode>` (büyük, kalın, monospace)
  - "Müşteriye fiş teslim edecek misiniz?" açıklaması
- Footer: `[Normal Fiş]`, `[Hediye Fişi]`, `[Atla]` (üç buton).

## Print View

### SaleController.Print

```csharp
[HttpGet("/sales/sale/{id:guid}/print")]
public async Task<IActionResult> Print(Guid id, string mode = "normal")
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success || result.Data is null) return NotFound();
    ViewBag.GiftMode = (mode == "gift");
    return View(result.Data);
}
```

### Print.cshtml güncellemesi

- Başlık: `@(ViewBag.GiftMode ? "HEDİYE FİŞİ" : "FİŞ")`.
- `@Model.ReturnCode` — her iki modda büyük/kalın/monospace, `SaleNumber` küçük puntoyla.
- Fiyat blokları `@if (ViewBag.GiftMode != true) { ... }` wrap:
  - Ürün satırındaki `@item.Quantity x @item.UnitPriceWithVat ... @item.LineTotalWithVat`
  - Alt toplam, KDV, TOPLAM satırları
  - Ödeme bloğu
- Hediye modunda ürün satırı: sadece `@item.ProductTitle` + `Adet: @item.Quantity`.
- Adet bilgisi iade için gerekli — müşteri "bunlardan birini iade edeceğim" derken kasiyer eşleşmeyi doğrular.

### SaleDetailDto

`ReturnCode` alanı DTO'ya eklenir:

```csharp
public string? ReturnCode { get; set; }
```

`SaleMapper` / `GetSaleDetailAsync` denormalize eder.

## Satış Detay Sayfasında Tekrar Baskı

`/sales/sale/{id}` → `SaleDetail.cshtml` → butonlar alanına:

```razor
<a href="/sales/sale/@Model.Id/print?mode=normal" target="_blank"
   class="btn btn-outline-secondary">
    <i class="ti ti-printer"></i> Fişi Yazdır
</a>
<a href="/sales/sale/@Model.Id/print?mode=gift" target="_blank"
   class="btn btn-outline-secondary">
    <i class="ti ti-gift"></i> Hediye Fişi Yazdır
</a>
```

## İade Ekranı

### /returns Index layout

Tablonun üstünde iki kolon (`row row-cards`):

**Sol kart — "Mevcut iadelerde ara":**
- Input: `search` (text). Submit (veya debounce) → HTMX `hx-get="/returns"` `hx-trigger="keyup changed delay:300ms"` `hx-target="#returns-table"`.
- Filtre: `SaleReturn.ReturnCode LIKE %q%` OR `Sale.SaleNumber LIKE %q%` OR `Customer.NameSurname LIKE %q%`.
- ReturnController.cs:52'deki tanımlı ama kullanılmayan `search` parametresi **aktifleştirilir**.

**Sağ kart — "Yeni iade başlat":**
- Input: `code` (text, `R-` veya `S` ile başlaması beklenir).
- Buton: "Satışı Getir" → HTMX `hx-post="/returns/lookup"` `hx-target="#modal-container"`.
- `POST /returns/lookup`:
  ```csharp
  [HttpPost("/returns/lookup")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Lookup([FromForm] string code)
  {
      var result = await saleManager.GetSaleByCodeAsync(code);
      if (!result.Success || result.Data is null)
      {
          Response.HtmxReswap("none");
          Response.HtmxTriggerWithData("showToast",
              new { message = "Satış bulunamadı.", level = "error" });
          return NoContent();
      }
      return PartialView("~/Features/Sales/Views/Partials/_SaleReturnDialog.cshtml", result.Data);
  }
  ```
- Mevcut `_SaleReturnDialog` partial'ı satış detay sayfasında da kullanılıyor — reuse.

## Kapsam Dışı

- Satış entity'sinde `IsGift` flag'i.
- İade tamamlandıktan sonra refund belgesi basma.
- QR kod / barkod basma.
- Eski satışlara `ReturnCode` backfill.
- Çoklu tenant için `ReturnCode` namespace'i — şu an tenant izolasyonu DB seviyesinde, unique index tek tenant DB'sinde yeterli.

## Test Planı

### Unit (`Test/Entegrasyon.Test/`)

- `ReturnCodeGeneratorTests`:
  - `Generate_ReturnsRFormatWith13Chars()` — format `R-` + 13 char.
  - `Generate_UsesCrockfordAlphabet_NoIluo()` — 1000 iterasyonda hiç I/L/O/U geçmemeli.
  - `GenerateWithCollisionCheck_RetriesOnDuplicate()` — fake callback ilk çağrıda true döner, ikincide false → iki kod üretmeli.

- `SaleManagerGetSaleByCodeTests`:
  - `GetSaleByCodeAsync_WithReturnCode_FindsSale()`.
  - `GetSaleByCodeAsync_WithSaleNumber_FindsSale()` (geriye uyumluluk).
  - `GetSaleByCodeAsync_NotFound_ReturnsError()`.
  - `GetSaleByCodeAsync_Lowercase_NormalizesToUpper()`.

### Integration (`Test/Entegrasyon.IntegrationTest/`)

- `MakeSaleReturnCodeTests`:
  - `MakeSale_AssignsReturnCode()` — satış sonrası `Sale.ReturnCode` dolu.
  - `MakeSale_ReturnCodeIsUnique()` — 10 satış sonrası 10 unique kod.
- `ReturnLookupEndpointTests`:
  - `Lookup_WithReturnCode_RendersDialog()`.
  - `Lookup_WithSaleNumber_RendersDialog()`.
  - `Lookup_Unknown_Returns204WithToast()`.
- `SalePrintGiftModeTests`:
  - `Print_GiftMode_HidesPriceBlocks()` — HTML çıktısında "TOPLAM" ve "KDV" yok; "İade Kodu" var.

### MVC (`Test/Entegrasyon.MVC.Test/`)

- `POSControllerCompleteSaleTests`:
  - `CompleteSale_OnSuccess_SetsPosLastSaleTempData()`.
- `ReturnControllerSearchTests`:
  - `Index_WithSearch_FiltersBySaleNumber()`.
  - `Index_WithSearch_FiltersByReturnCode()`.

## Migration Adımları

1. `Sale` entity'sine `ReturnCode` property ekle.
2. `SaleEntityConfiguration` → `HasIndex(s => s.ReturnCode).IsUnique().HasFilter("\"ReturnCode\" IS NOT NULL")`.
3. `dotnet ef migrations add AddSaleReturnCode -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`.
4. Migration dosyasını incele, gereksiz change olmadığını doğrula.
5. `dotnet ef database update`.
6. `dotnet ef migrations has-pending-model-changes` ile snapshot doğrula.
