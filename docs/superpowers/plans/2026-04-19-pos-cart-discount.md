# POS Sepet (Genel) İndirimi — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** POS ekranında sepet toplamına yüzde/TL genel indirim uygulayabilmek; indirim her kaleme pro-rata dağıtılıp KDV/iade uyumu korunur.

**Architecture:** Kalem indirimli gross üzerinden pro-rata hesaplanır, rounding remainder son kaleme yazılır; indirim `SaleItem.DiscountAmount`'a birleşik yazılır; `Sale.GeneralDiscount` bilgi amaçlı tutulur. UI: sepet altında "İndirim" butonu ve ödeme dialog'unda "Düzenle" butonu aynı modalı açar.

**Tech Stack:** ASP.NET Core 8 MVC + HTMX + Tabler UI, EF Core 10 / PostgreSQL, Mapperly, FluentValidation, xUnit + Moq + FluentAssertions, Testcontainers, Playwright.

**Spec:** `docs/superpowers/specs/2026-04-19-pos-cart-discount-design.md`

---

## Task 1: Pro-rata dağıtım pure function + unit testleri

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/POS/CartDiscountDistributor.cs`
- Test: `Test/Entegrasyon.Test/Business/POS/CartDiscountDistributorTests.cs`

- [ ] **Step 1: Unit test dosyasını oluştur (RED)**

```csharp
// Test/Entegrasyon.Test/Business/POS/CartDiscountDistributorTests.cs
using Entegrasyon.MVC.Features.POS;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business.POS;

public class CartDiscountDistributorTests
{
    private static CartLine Line(decimal unitPrice, int qty, decimal vatRate, decimal existingDiscountAmount = 0)
        => new(UnitPrice: unitPrice, Quantity: qty, VatRate: vatRate, ExistingLineDiscountAmount: existingDiscountAmount);

    [Fact]
    public void Distribute_ReturnsZeroShares_WhenGeneralDiscountIsZero()
    {
        var lines = new[] { Line(100m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 0m);
        result.PerLineNetShare.Should().AllSatisfy(s => s.Should().Be(0m));
        result.AppliedGrossTotal.Should().Be(0m);
    }

    [Fact]
    public void Distribute_SingleLine_AllDiscountGoesToThatLine()
    {
        var lines = new[] { Line(1000m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 250m);
        result.AppliedGrossTotal.Should().Be(250m);
        // Net pay = 250 / (1 + 20/100) = 208.33
        result.PerLineNetShare[0].Should().Be(208.33m);
    }

    [Fact]
    public void Distribute_TwoEqualLines_DividesEvenly()
    {
        var lines = new[] { Line(100m, 1, 20m), Line(100m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 40m);
        // Her biri 20 TL gross düşer → net 16.67
        result.PerLineNetShare[0].Should().Be(16.67m);
        result.PerLineNetShare[1].Should().Be(16.67m);
    }

    [Fact]
    public void Distribute_RoundingRemainder_GoesToLastLine()
    {
        // 3 eşit kalem, 10 TL gross indirim → 3.33 + 3.33 + 3.34
        var lines = new[] { Line(100m, 1, 0m), Line(100m, 1, 0m), Line(100m, 1, 0m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 10m);
        result.PerLineGrossShare.Sum().Should().Be(10m);
        result.PerLineGrossShare[0].Should().Be(3.33m);
        result.PerLineGrossShare[1].Should().Be(3.33m);
        result.PerLineGrossShare[2].Should().Be(3.34m);
    }

    [Fact]
    public void Distribute_MixedVatRates_KeepsPerLineVatCorrect()
    {
        // A: 1000 TL gross (net 833.33 + %20 KDV)
        // B: 500 TL gross (net 495.05 + %1 KDV)
        // Toplam gross = 1500, indirim 250
        // A payı: 1000/1500 * 250 = 166.67 gross
        // B payı: 500/1500 * 250 = 83.33 gross (+remainder)
        var lines = new[]
        {
            Line(833.3333m, 1, 20m),  // gross ≈ 1000
            Line(495.0495m, 1, 1m)    // gross ≈ 500
        };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 250m);
        result.PerLineGrossShare.Sum().Should().Be(250m);
        result.PerLineGrossShare[0].Should().Be(166.67m);
        result.PerLineGrossShare[1].Should().Be(83.33m);
        // Net paylar = gross / (1 + vatRate/100)
        result.PerLineNetShare[0].Should().Be(Math.Round(166.67m / 1.20m, 2));
        result.PerLineNetShare[1].Should().Be(Math.Round(83.33m / 1.01m, 2));
    }

    [Fact]
    public void Distribute_InvariantHolds_GrandTotalMatchesTarget()
    {
        var lines = new[] { Line(1000m, 1, 20m), Line(500m, 1, 10m) };
        var subtotalGross = 1000m * 1.20m + 500m * 1.10m; // 1750
        var target = 1500m;
        var discount = subtotalGross - target; // 250

        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: discount);

        var finalGross = subtotalGross - result.AppliedGrossTotal;
        finalGross.Should().Be(target);
    }

    [Fact]
    public void Distribute_ClampsWhenDiscountExceedsSubtotal()
    {
        var lines = new[] { Line(100m, 1, 0m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 150m);
        result.AppliedGrossTotal.Should().Be(100m);
        result.PerLineGrossShare[0].Should().Be(100m);
    }
}
```

- [ ] **Step 2: Run test to verify fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CartDiscountDistributor" --nologo`
Expected: Compile error — `CartDiscountDistributor` yok.

- [ ] **Step 3: Pure function'ı yaz**

```csharp
// Application/Entegrasyon.MVC/Features/POS/CartDiscountDistributor.cs
namespace Entegrasyon.MVC.Features.POS;

public readonly record struct CartLine(
    decimal UnitPrice,
    int Quantity,
    decimal VatRate,
    decimal ExistingLineDiscountAmount);

public sealed record CartDiscountDistribution(
    decimal[] PerLineGrossShare,
    decimal[] PerLineNetShare,
    decimal AppliedGrossTotal);

public static class CartDiscountDistributor
{
    /// <summary>
    /// Sepet (genel) indirimini kalemlere pro-rata dağıtır.
    /// Girdi: her kalemin (kalem indirimi uygulanmış) gross payı baz alınır.
    /// Çıktı: her kalem için gross ve net indirim payı, uygulanan toplam gross.
    /// Yuvarlama kalanı son kaleme eklenir → toplam tam hedef tutara eşitlenir.
    /// </summary>
    public static CartDiscountDistribution Distribute(
        IReadOnlyList<CartLine> lines,
        decimal generalDiscountGross)
    {
        var count = lines.Count;
        var grossShares = new decimal[count];
        var netShares = new decimal[count];

        if (count == 0 || generalDiscountGross <= 0m)
            return new CartDiscountDistribution(grossShares, netShares, 0m);

        // Kalem indirimi uygulanmış gross = (UnitPrice*Qty − ExistingDiscount) * (1 + VatRate/100)
        var lineGross = new decimal[count];
        for (int i = 0; i < count; i++)
        {
            var net = lines[i].UnitPrice * lines[i].Quantity - lines[i].ExistingLineDiscountAmount;
            if (net < 0m) net = 0m;
            lineGross[i] = Math.Round(net * (1m + lines[i].VatRate / 100m), 2);
        }

        var subtotalGross = lineGross.Sum();
        if (subtotalGross <= 0m)
            return new CartDiscountDistribution(grossShares, netShares, 0m);

        // Clamp: indirim alt toplamdan büyük olamaz
        var applied = Math.Min(generalDiscountGross, subtotalGross);

        for (int i = 0; i < count; i++)
            grossShares[i] = Math.Round(lineGross[i] / subtotalGross * applied, 2);

        // Rounding remainder son kaleme yazılır
        var distributed = grossShares.Sum();
        grossShares[count - 1] += (applied - distributed);

        for (int i = 0; i < count; i++)
        {
            if (grossShares[i] > 0m)
                netShares[i] = Math.Round(grossShares[i] / (1m + lines[i].VatRate / 100m), 2);
        }

        return new CartDiscountDistribution(grossShares, netShares, applied);
    }
}
```

- [ ] **Step 4: Run test to verify PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CartDiscountDistributor" --nologo`
Expected: 7 passing.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/CartDiscountDistributor.cs \
        Test/Entegrasyon.Test/Business/POS/CartDiscountDistributorTests.cs
git commit -m "feat(pos): sepet indirimi pro-rata dağıtım fonksiyonu + testler"
```

---

## Task 2: Sale entity + MakeSaleDto genişletme

**Files:**
- Modify: `Application/Entegrasyon.Entity/Sales/Sale.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs`

- [ ] **Step 1: Sale entity'sine genel indirim sebep alanlarını ekle + GeneralDiscount tipini decimal'e çek**

Edit `Application/Entegrasyon.Entity/Sales/Sale.cs` — satır 18 olan `public double GeneralDiscount { get; set; }` satırını decimal yap ve yeni alanlar ekle:

```csharp
public decimal GeneralDiscount { get; set; }

public int? GeneralDiscountReasonId { get; set; }
public DiscountReason? GeneralDiscountReason { get; set; }

[StringLength(200)]
public string? GeneralDiscountReasonNote { get; set; }
```

- [ ] **Step 2: MakeSaleDto'ya yeni alanları ekle**

Edit `Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs` — `double GeneralDiscount` alanını decimal'e çek ve iki yeni opsiyonel alan ekle:

```csharp
public sealed record MakeSaleDto(
    Guid SalePersonId,
    int? CustomerId,
    decimal GeneralDiscount,
    int BranchOfficeId,
    SaleSource SaleSource,
    string? Note,
    IEnumerable<SaleItemDto> SaleItems,
    List<SalePaymentDto> Payments,
    int? GeneralDiscountReasonId = null,
    string? GeneralDiscountReasonNote = null);
```

- [ ] **Step 3: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj --nologo`
Expected: success.

- [ ] **Step 4: Bağımlı projelerde break noktalarını düzelt**

Run: `dotnet build Entegrasyon.sln --nologo 2>&1 | grep -E "error|Error" | head -40`

Beklenen hatalar iki noktada:
1. `SaleDetailDto.GeneralDiscount` alanı `double` — hala double olabilir (view tarafı string format için), ama tutarsızlık olmasın. Bu alanı da decimal yap:

Edit `Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs`:
```csharp
public decimal GeneralDiscount { get; init; }
```

2. `SaleManager.GetSaleDetailAsync` içinde `GeneralDiscount = sale.GeneralDiscount` satırında artık tip uyumu var (ikisi de decimal).

3. `POSController.CompleteSale`'de `GeneralDiscount: 0` argümanı → `GeneralDiscount: 0m` olsun.

Edit `Application/Entegrasyon.MVC/Features/POS/POSController.cs` CompleteSale metodunda:
```csharp
var makeSaleDto = new MakeSaleDto(
    SalePersonId: GetCurrentUserId(),
    CustomerId: GetCustomerIdFromSession(),
    GeneralDiscount: 0m,
    BranchOfficeId: DefaultBranchOfficeId,
    SaleSource: SaleSource.POS,
    Note: null,
    SaleItems: saleItems,
    Payments: payments);
```

- [ ] **Step 5: Solution build**

Run: `dotnet build Entegrasyon.sln --nologo`
Expected: success (tüm projeler).

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/Sale.cs \
        Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs \
        Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs \
        Application/Entegrasyon.MVC/Features/POS/POSController.cs
git commit -m "refactor(sales): Sale.GeneralDiscount double→decimal + sebep alanları"
```

---

## Task 3: EF Core migration

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Migrations/<timestamp>_AddSaleGeneralDiscountAndReason.cs` (EF üretir)

- [ ] **Step 1: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddSaleGeneralDiscountAndReason \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 2: Migration dosyasını incele**

Open: `Application/Entegrasyon.DataAccess/Migrations/<timestamp>_AddSaleGeneralDiscountAndReason.cs`

Beklenen değişiklikler:
- `Sales.GeneralDiscount` column type: `double precision` → `numeric(18,2)`
- `Sales.GeneralDiscountReasonId` column: `integer` nullable, FK → `DiscountReasons.Id`
- `Sales.GeneralDiscountReasonNote` column: `character varying(200)` nullable
- İlgili index `Sales.GeneralDiscountReasonId` üzerinde

Yanlış ek değişiklik (gereksiz alter) varsa elle temizle.

- [ ] **Step 3: Dev DB'ye uygula**

Run:
```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: "Done."

- [ ] **Step 4: Model-snapshot doğrulaması**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: "No pending model changes."

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Migrations/
git commit -m "feat(db): Sale genel indirim + sebep alanları migration"
```

---

## Task 4: FluentValidation — MakeSale genel indirim kuralları

**Files:**
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/MakeSaleValidator.cs`
- Test: `Test/Entegrasyon.Test/Business/MakeSaleValidatorGeneralDiscountTests.cs` (CREATE)

- [ ] **Step 1: Failing test ekle**

```csharp
// Test/Entegrasyon.Test/Business/MakeSaleValidatorGeneralDiscountTests.cs
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class MakeSaleValidatorGeneralDiscountTests
{
    private readonly MakeSaleValidator _sut = new();

    private static MakeSaleDto Valid(decimal generalDiscount)
        => new(
            SalePersonId: Guid.NewGuid(),
            CustomerId: null,
            GeneralDiscount: generalDiscount,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: [new SaleItemDto(
                ProductVariantId: Guid.NewGuid(),
                TaxPercentage: 20,
                DiscountPercent: 0,
                UnitPrice: 100m,
                Quantity: 1,
                DiscountVoucherCode: "")],
            Payments: [new SalePaymentDto(PaymentMethodId: 1, Amount: 100m, null, null)]);

    [Fact]
    public void Validator_RejectsNegativeGeneralDiscount()
    {
        var dto = Valid(-1m);
        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_RejectsGeneralDiscountExceedingSubtotalGross()
    {
        // Subtotal gross = 100 * 1.20 = 120
        var dto = Valid(500m);
        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_AcceptsZeroGeneralDiscount()
    {
        var dto = Valid(0m);
        var result = _sut.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_AcceptsDiscountWithinSubtotal()
    {
        var dto = Valid(50m);
        var result = _sut.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.GeneralDiscount);
    }
}
```

Çalıştır: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MakeSaleValidatorGeneralDiscount" --nologo`
Expected: 3 fail (negative, exceeding, accepted should fail because no rule yet; "zero" muhtemelen geçer).

- [ ] **Step 2: Validator'a kurallar ekle**

Edit `Application/Entegrasyon.Business/Validation/FluentValidation/MakeSaleValidator.cs` — mevcut constructor'a ekle:

```csharp
RuleFor(x => x.GeneralDiscount)
    .GreaterThanOrEqualTo(0m)
    .WithMessage("Genel indirim negatif olamaz.");

RuleFor(x => x)
    .Must(HaveDiscountWithinSubtotal)
    .WithName(nameof(MakeSaleDto.GeneralDiscount))
    .WithMessage("Genel indirim sepet alt toplamından büyük olamaz.");

static bool HaveDiscountWithinSubtotal(MakeSaleDto dto)
{
    if (dto.GeneralDiscount <= 0m) return true;
    var subtotalGross = dto.SaleItems
        .Sum(i => i.UnitPrice * i.Quantity * (1m + (decimal)i.TaxPercentage / 100m));
    return dto.GeneralDiscount <= subtotalGross;
}
```

- [ ] **Step 3: Testi çalıştır**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MakeSaleValidatorGeneralDiscount" --nologo`
Expected: 4 passing.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Validation/FluentValidation/MakeSaleValidator.cs \
        Test/Entegrasyon.Test/Business/MakeSaleValidatorGeneralDiscountTests.cs
git commit -m "feat(validation): MakeSale genel indirim kuralları"
```

---

## Task 5: POSCartVm refactor — sepet indirim alanları

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs`

- [ ] **Step 1: POSCartVm'e genel indirim alanlarını ekle**

Edit `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs` — `POSCartVm` class'ını değiştir:

```csharp
public class POSCartVm
{
    public List<POSCartItemVm> Items { get; set; } = [];

    // Genel (sepet) indirimi state
    public string? GeneralDiscountType { get; set; }   // "percent" | "amount" | null
    public decimal GeneralDiscountValue { get; set; }
    public int? GeneralDiscountReasonId { get; set; }
    public string? GeneralDiscountReasonName { get; set; }
    public string? GeneralDiscountReasonNote { get; set; }

    // Kalem indirimi UYGULANMIŞ alt toplam (KDV-dahil)
    public decimal SubtotalAfterLineDiscount => Items.Sum(i => i.LineTotalWithVat);

    // Eski arayüzler için geriye dönük - ara toplam (KDV hariç, kalem indirimi uygulanmış)
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal VatTotal => Items.Sum(i => i.VatAmount);

    // Genel indirim uygulanmamış grand total (kalem indirimli, KDV-dahil)
    public decimal GrandTotalBeforeGeneralDiscount => SubtotalAfterLineDiscount;

    public decimal GeneralDiscountAmount
    {
        get
        {
            if (string.IsNullOrEmpty(GeneralDiscountType) || GeneralDiscountValue <= 0m)
                return 0m;
            if (SubtotalAfterLineDiscount <= 0m) return 0m;

            return GeneralDiscountType switch
            {
                "percent" => Math.Round(
                    SubtotalAfterLineDiscount * Math.Min(GeneralDiscountValue, 100m) / 100m, 2),
                "amount"  => Math.Min(GeneralDiscountValue, SubtotalAfterLineDiscount),
                _         => 0m
            };
        }
    }

    public decimal GrandTotal => GrandTotalBeforeGeneralDiscount - GeneralDiscountAmount;
    public int TotalItems => Items.Sum(i => i.Quantity);
    public bool HasGeneralDiscount => GeneralDiscountAmount > 0m;
}
```

Not: eski `Subtotal`, `VatTotal`, `GrandTotal` propertylerinin davranışı değişiyor. `GrandTotal` artık genel indirim uygulanmış tutarı döndürür. Bu, sepet footer'ını ve payment dialog'unu doğrudan etkiler — bir sonraki task'larda bu alanlar bilinçli kullanılır.

- [ ] **Step 2: Yeni ViewModel sınıfı ekle — POSCartDiscountDialogVm**

Aynı dosyanın sonuna:

```csharp
public class POSCartDiscountDialogVm
{
    public decimal SubtotalAfterLineDiscount { get; set; }
    public string? CurrentType { get; set; }
    public decimal CurrentValue { get; set; }
    public int? CurrentReasonId { get; set; }
    public string? CurrentNote { get; set; }
    public List<Entegrasyon.Entity.Sales.DiscountReason> Reasons { get; set; } = [];
}
```

- [ ] **Step 3: POSPaymentDialogVm'e yeni alanlar ekle**

Aynı dosyadaki `POSPaymentDialogVm`'i güncelle:

```csharp
public class POSPaymentDialogVm
{
    public long SessionId { get; set; }
    public decimal SubtotalBeforeGeneralDiscount { get; set; }  // kalem indirimli, KDV-dahil
    public decimal GeneralDiscountAmount { get; set; }
    public string? GeneralDiscountReasonName { get; set; }
    public decimal Subtotal { get; set; }   // KDV hariç (mevcut davranış)
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public int ItemCount { get; set; }
    public List<PaymentMethodDefinition> PaymentMethods { get; set; } = [];
    public string SubmitToken { get; set; } = "";
    public bool HasGeneralDiscount => GeneralDiscountAmount > 0m;
}
```

- [ ] **Step 4: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success (henüz controller kullanmıyor).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs
git commit -m "feat(pos): POSCartVm genel indirim alanları + dialog VM'leri"
```

---

## Task 6: Controller — session refactor (cart artık POSCartVm serialize edilir)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs` (sadece session helper'ları)

- [ ] **Step 1: Session helper'larını güncelle**

Edit `Application/Entegrasyon.MVC/Features/POS/POSController.cs` — `GetCartFromSession` ve `SaveCartToSession` metotlarını değiştir (dosya sonunda):

```csharp
private POSCartVm GetCartFromSession()
{
    var json = HttpContext.Session.GetString("pos_cart");
    if (string.IsNullOrEmpty(json)) return new POSCartVm();

    try
    {
        var vm = System.Text.Json.JsonSerializer.Deserialize<POSCartVm>(json);
        if (vm is not null) return vm;
    }
    catch
    {
        // Eski şema (liste) — sessizce boş cart'a düş
    }

    // Eski şema desteği: list ise items olarak al
    try
    {
        var items = System.Text.Json.JsonSerializer.Deserialize<List<POSCartItemVm>>(json);
        if (items is not null) return new POSCartVm { Items = items };
    }
    catch { }

    return new POSCartVm();
}

private void SaveCartToSession(POSCartVm cart)
{
    var json = System.Text.Json.JsonSerializer.Serialize(cart);
    HttpContext.Session.SetString("pos_cart", json);
}
```

- [ ] **Step 2: Tüm mevcut action'larda cart kullanımını uyarla**

Aynı dosyada aşağıdaki yerlerde değişim gerekir. Her yerde `cart` artık `POSCartVm` ve `cart.Items` ile kalemlere erişilir:

**AddItem:**
```csharp
var cart = GetCartFromSession();
...
var existingItem = cart.Items.FirstOrDefault(c => c.ProductId == productId);
...
cart.Items.Add(new POSCartItemVm { ... });
...
SaveCartToSession(cart);
return PartialView("Partials/_POSCart", cart);
```

**UpdateQuantity:**
```csharp
var cart = GetCartFromSession();
var item = cart.Items.FirstOrDefault(c => c.ProductId == productId);
...
if (quantity <= 0) cart.Items.Remove(item);
...
SaveCartToSession(cart);
return PartialView("Partials/_POSCart", cart);
```

**RemoveItem:**
```csharp
var cart = GetCartFromSession();
cart.Items.RemoveAll(c => c.ProductId == productId);
SaveCartToSession(cart);
return PartialView("Partials/_POSCart", cart);
```

**ClearCart:**
```csharp
SaveCartToSession(new POSCartVm());   // her şey sıfırlanır (kalemler + genel indirim)
HttpContext.Session.Remove("pos_customer_id");
HttpContext.Session.Remove("pos_customer_name");
return PartialView("Partials/_POSCart", new POSCartVm());
```

**ItemDiscountDialog:**
```csharp
var cart = GetCartFromSession();
var item = cart.Items.FirstOrDefault(c => c.VariantId == variantId);
```

**ApplyItemDiscount:**
```csharp
var cart = GetCartFromSession();
var item = cart.Items.FirstOrDefault(c => c.VariantId == variantId);
...
// reset, set, save
SaveCartToSession(cart);
return PartialView("Partials/_POSCart", cart);
```

**PaymentDialog:**
```csharp
var cart = GetCartFromSession();
if (cart.Items.Count == 0) { ... return NoContent(); }
...
var vm = new POSPaymentDialogVm
{
    SessionId = sessionResult.Data.Id,
    SubtotalBeforeGeneralDiscount = cart.GrandTotalBeforeGeneralDiscount,
    GeneralDiscountAmount = cart.GeneralDiscountAmount,
    GeneralDiscountReasonName = cart.GeneralDiscountReasonName,
    Subtotal = cart.Subtotal,
    VatTotal = cart.VatTotal,
    GrandTotal = cart.GrandTotal,    // artık genel indirim uygulanmış
    ItemCount = cart.TotalItems,
    PaymentMethods = paymentMethodsResult.Data ?? [],
    SubmitToken = submitToken
};
return PartialView("Partials/_POSPaymentDialog", vm);
```

**CompleteSale:**
```csharp
var cart = GetCartFromSession();
if (cart.Items.Count == 0) { ... }
...
var saleItems = cart.Items.Select(c => ...).ToList();
```

(Task 11'de `CompleteSale` içine pro-rata dağıtım eklenir.)

- [ ] **Step 3: Partial view Model typei değişimi**

Edit `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml` — ilk satır:

```cshtml
@model Entegrasyon.MVC.Features.POS.ViewModels.POSCartVm
```

Zaten `POSCartVm` → değişiklik yok. Ama view içinde kullanılan `Model.Items.Count`, `Model.TotalItems`, `Model.Subtotal`, `Model.VatTotal`, `Model.GrandTotal` referansları hâlâ geçerli (VM'de property isimleri aynı). Değişim sadece anlamda: `GrandTotal` artık genel indirim uygulanmış değer. Task 9'da footer içinde genel indirim satırı eklenir.

- [ ] **Step 4: Build + smoke run**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success.

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --nologo`
Expected: mevcut MVC testleri pass.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/POSController.cs
git commit -m "refactor(pos): cart session şeması POSCartVm'e geçti"
```

---

## Task 7: Yeni controller action'ları — CartDiscountDialog, ApplyCartDiscount, ClearCartDiscount

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs`

- [ ] **Step 1: "Kalem İndirimi" bölümünden sonra yeni bölüm ekle**

Edit `Application/Entegrasyon.MVC/Features/POS/POSController.cs` — `ApplyItemDiscount` metodunun bitişinden sonra, `CompleteSale`'den önce ekle:

```csharp
// ── Sepet (Genel) İndirimi (HTMX) ───────────────────────────────────

[HttpGet("/pos/cart-discount-dialog")]
public async Task<IActionResult> CartDiscountDialog()
{
    var cart = GetCartFromSession();
    if (cart.Items.Count == 0)
    {
        Response.HtmxReswap("none");
        Response.HtmxTriggerWithData("showToast",
            new { message = "Sepet boş. İndirim uygulanamaz.", level = "error" });
        return NoContent();
    }

    var reasons = await discountReasonManager.GetActiveAsync();

    var vm = new POSCartDiscountDialogVm
    {
        SubtotalAfterLineDiscount = cart.SubtotalAfterLineDiscount,
        CurrentType = cart.GeneralDiscountType,
        CurrentValue = cart.GeneralDiscountValue,
        CurrentReasonId = cart.GeneralDiscountReasonId,
        CurrentNote = cart.GeneralDiscountReasonNote,
        Reasons = reasons.ToList()
    };

    return PartialView("Partials/_POSCartDiscountModal", vm);
}

[HttpPost("/pos/apply-cart-discount")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApplyCartDiscount(
    [FromForm] string discountType,
    [FromForm] decimal? percent,
    [FromForm] decimal? amount,
    [FromForm] int? reasonId,
    [FromForm] string? note)
{
    var cart = GetCartFromSession();
    if (cart.Items.Count == 0)
    {
        Response.HtmxTriggerWithData("showToast",
            new { message = "Sepet boş.", level = "error" });
        return PartialView("Partials/_POSCart", cart);
    }

    // Reset
    cart.GeneralDiscountType = null;
    cart.GeneralDiscountValue = 0m;

    if (discountType == "percent")
    {
        if (percent is not > 0 || percent > 100m)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Yüzde 0-100 arasında olmalı.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }
        cart.GeneralDiscountType = "percent";
        cart.GeneralDiscountValue = percent.Value;
    }
    else if (discountType == "amount")
    {
        if (amount is not > 0m)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "İndirim 0'dan büyük olmalı.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }
        if (amount.Value > cart.SubtotalAfterLineDiscount)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "İndirim sepet toplamından büyük olamaz.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }
        cart.GeneralDiscountType = "amount";
        cart.GeneralDiscountValue = amount.Value;
    }
    else
    {
        Response.HtmxTriggerWithData("showToast",
            new { message = "Geçersiz indirim tipi.", level = "error" });
        return PartialView("Partials/_POSCart", cart);
    }

    cart.GeneralDiscountReasonId = reasonId;
    cart.GeneralDiscountReasonNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    // Reason name denormalize
    if (reasonId.HasValue)
    {
        var reasons = await discountReasonManager.GetActiveAsync();
        cart.GeneralDiscountReasonName = reasons.FirstOrDefault(r => r.Id == reasonId.Value)?.Name;
    }
    else
    {
        cart.GeneralDiscountReasonName = null;
    }

    SaveCartToSession(cart);

    Response.HtmxTrigger("closePosCartDiscountModal");
    Response.HtmxTriggerWithData("showToast",
        new { message = "Sepet indirimi uygulandı.", level = "success" });

    return PartialView("Partials/_POSCart", cart);
}

[HttpPost("/pos/clear-cart-discount")]
[ValidateAntiForgeryToken]
public IActionResult ClearCartDiscount()
{
    var cart = GetCartFromSession();
    cart.GeneralDiscountType = null;
    cart.GeneralDiscountValue = 0m;
    cart.GeneralDiscountReasonId = null;
    cart.GeneralDiscountReasonName = null;
    cart.GeneralDiscountReasonNote = null;
    SaveCartToSession(cart);

    Response.HtmxTriggerWithData("showToast",
        new { message = "Sepet indirimi kaldırıldı.", level = "success" });

    return PartialView("Partials/_POSCart", cart);
}
```

- [ ] **Step 2: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/POSController.cs
git commit -m "feat(pos): sepet indirimi controller action'ları (uygula/kaldır/dialog)"
```

---

## Task 8: Modal partial view — _POSCartDiscountModal.cshtml

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCartDiscountModal.cshtml`

- [ ] **Step 1: Kalem indirimi modal'ını referans olarak oku**

Read: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLineDiscountModal.cshtml` — aynı UX pattern'i korumak için.

- [ ] **Step 2: Partial'ı oluştur**

```cshtml
@* _POSCartDiscountModal.cshtml *@
@model Entegrasyon.MVC.Features.POS.ViewModels.POSCartDiscountDialogVm

<div class="modal modal-blur fade show" id="posCartDiscountModal" tabindex="-1"
     style="display:block" aria-modal="true" role="dialog"
     hx-on::after-swap="this.remove()"
     @@close-pos-cart-discount-modal.window="this.remove()">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Sepet İndirimi</h5>
                <button type="button" class="btn-close"
                        onclick="document.getElementById('posCartDiscountModal').remove()"></button>
            </div>

            <form hx-post="/pos/apply-cart-discount"
                  hx-target="#pos-cart"
                  hx-swap="innerHTML">
                @Html.AntiForgeryToken()

                <div class="modal-body">
                    <div class="mb-3">
                        <div class="text-secondary">Sepet alt toplamı (kalem indirimleri sonrası)</div>
                        <div class="fs-2 fw-bold">@Model.SubtotalAfterLineDiscount.ToString("N2") TL</div>
                    </div>

                    <div class="btn-group w-100 mb-3" role="group" aria-label="İndirim tipi">
                        <input type="radio" class="btn-check" name="discountType" id="cd-type-percent"
                               value="percent" autocomplete="off"
                               @(Model.CurrentType == "percent" || Model.CurrentType is null ? "checked" : "")>
                        <label class="btn btn-outline-primary" for="cd-type-percent">Yüzde (%)</label>

                        <input type="radio" class="btn-check" name="discountType" id="cd-type-amount"
                               value="amount" autocomplete="off"
                               @(Model.CurrentType == "amount" ? "checked" : "")>
                        <label class="btn btn-outline-primary" for="cd-type-amount">TL</label>
                    </div>

                    <div class="mb-3">
                        <label class="form-label">Yüzde</label>
                        <div class="input-group">
                            <input type="number" class="form-control" name="percent"
                                   min="0" max="100" step="0.01"
                                   value="@(Model.CurrentType == "percent" ? Model.CurrentValue.ToString("0.##") : "")"
                                   data-discount-input="percent" />
                            <span class="input-group-text">%</span>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label">TL</label>
                        <div class="input-group">
                            <input type="number" class="form-control" name="amount"
                                   min="0" step="0.01"
                                   value="@(Model.CurrentType == "amount" ? Model.CurrentValue.ToString("0.##") : "")"
                                   data-discount-input="amount" />
                            <span class="input-group-text">TL</span>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label">İndirim Nedeni (opsiyonel)</label>
                        <select class="form-select" name="reasonId">
                            <option value="">— Seçiniz —</option>
                            @foreach (var r in Model.Reasons)
                            {
                                <option value="@r.Id" selected="@(Model.CurrentReasonId == r.Id)">@r.Name</option>
                            }
                        </select>
                    </div>

                    <div class="mb-3">
                        <label class="form-label">Not (opsiyonel)</label>
                        <textarea class="form-control" name="note" maxlength="200" rows="2">@Model.CurrentNote</textarea>
                    </div>
                </div>

                <div class="modal-footer">
                    @if (Model.CurrentType is not null && Model.CurrentValue > 0)
                    {
                        <button type="button" class="btn btn-outline-danger me-auto"
                                hx-post="/pos/clear-cart-discount"
                                hx-target="#pos-cart"
                                hx-swap="innerHTML"
                                onclick="document.getElementById('posCartDiscountModal').remove()">
                            Kaldır
                        </button>
                    }
                    <button type="button" class="btn"
                            onclick="document.getElementById('posCartDiscountModal').remove()">
                        Vazgeç
                    </button>
                    <button type="submit" class="btn btn-primary">Uygula</button>
                </div>
            </form>
        </div>
    </div>
</div>
<div class="modal-backdrop fade show"
     onclick="document.getElementById('posCartDiscountModal')?.remove(); this.remove();"></div>

<script>
(function() {
    document.addEventListener('closePosCartDiscountModal', function() {
        document.getElementById('posCartDiscountModal')?.remove();
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
    }, { once: true });
})();
</script>
```

- [ ] **Step 3: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success (Razor compile OK).

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCartDiscountModal.cshtml
git commit -m "feat(pos): sepet indirimi modal partial'ı"
```

---

## Task 9: Cart footer güncellemesi — sepet indirimi satırı + "İndirim" butonu

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml` (footer bölümü)

- [ ] **Step 1: Footer'daki `<tfoot>` bloğunu değiştir**

Read `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml` satır 128-145 (mevcut tfoot).

Değiştir:

```cshtml
<tfoot>
    <tr class="bg-light">
        <td colspan="2" class="fw-bold">Ara Toplam</td>
        <td class="text-end">@Model.TotalItems adet</td>
        <td class="text-end fw-bold">@Model.SubtotalAfterLineDiscount.ToString("N2") TL</td>
        <td></td>
    </tr>
    @if (Model.HasGeneralDiscount)
    {
        <tr class="bg-light">
            <td colspan="3" class="text-success">
                Sepet İndirimi
                @if (Model.GeneralDiscountType == "percent")
                {
                    <span class="badge bg-green-lt ms-2">%@Model.GeneralDiscountValue.ToString("0.##")</span>
                }
                @if (!string.IsNullOrEmpty(Model.GeneralDiscountReasonName))
                {
                    <span class="text-secondary small ms-2">(@Model.GeneralDiscountReasonName)</span>
                }
            </td>
            <td class="text-end text-success">-@Model.GeneralDiscountAmount.ToString("N2") TL</td>
            <td class="text-end">
                <form method="post" action="/pos/clear-cart-discount"
                      hx-post="/pos/clear-cart-discount"
                      hx-target="#pos-cart"
                      hx-swap="innerHTML"
                      style="display:inline">
                    @Html.AntiForgeryToken()
                    <button type="submit" class="btn btn-ghost-danger btn-sm btn-icon"
                            title="İndirimi kaldır">
                        <i class="ti ti-x icon"></i>
                    </button>
                </form>
            </td>
        </tr>
    }
    <tr class="bg-light">
        <td colspan="3" class="fw-bold fs-4">TOPLAM</td>
        <td class="text-end fw-bold fs-4 text-primary">@Model.GrandTotal.ToString("N2") TL</td>
        <td class="text-end">
            <button type="button"
                    class="btn btn-outline-primary btn-sm"
                    title="@(Model.HasGeneralDiscount ? "Sepet indirimini düzenle" : "Sepet indirimi uygula")"
                    hx-get="/pos/cart-discount-dialog"
                    hx-target="#modal-container"
                    hx-swap="innerHTML">
                <i class="ti ti-discount icon"></i>
                @(Model.HasGeneralDiscount ? "Düzenle" : "İndirim")
            </button>
        </td>
    </tr>
</tfoot>
```

Not: Eski footer'da `Model.Subtotal` (KDV hariç) ve `Model.VatTotal` satırları vardı. Bunları çıkardım — kasiyere KDV dahil akış daha anlaşılır ve ekran daha temiz. İstenirse geri eklenebilir.

- [ ] **Step 2: Build + manuel test**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success.

Manuel kontrol: `cd Application/Entegrasyon.MVC && dotnet run` → `http://localhost:5100/pos` → sepete ürün ekle → footer'da "İndirim" butonu görünür → tıkla → modal açılır.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml
git commit -m "feat(pos): cart footer sepet indirimi satırı + İndirim butonu"
```

---

## Task 10: Payment dialog — "İndirim düzenle" butonu + özet satırları

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml`

- [ ] **Step 1: Özet bölümüne sepet indirimi satırı ekle**

Read `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml`. Özet (Subtotal/VatTotal/GrandTotal gösteren) bölümüne, GrandTotal satırından önce ekle:

```cshtml
@if (Model.HasGeneralDiscount)
{
    <div class="row mb-2">
        <div class="col text-secondary">
            Sepet İndirimi
            @if (!string.IsNullOrEmpty(Model.GeneralDiscountReasonName))
            {
                <span class="small">— @Model.GeneralDiscountReasonName</span>
            }
        </div>
        <div class="col-auto text-end text-success">-@Model.GeneralDiscountAmount.ToString("N2") TL</div>
    </div>
}
```

GrandTotal satırı ve/veya "Ödenecek tutar" bölümünün altına (ödeme form'u başlamadan önce) "Düzenle" butonu ekle:

```cshtml
<div class="mb-3 text-end">
    <button type="button"
            class="btn btn-outline-primary btn-sm"
            hx-get="/pos/cart-discount-dialog"
            hx-target="#modal-container"
            hx-swap="innerHTML"
            hx-on::after-request="if (event.detail.successful) document.getElementById('pos-payment-dialog')?.remove();">
        <i class="ti ti-discount icon"></i>
        @(Model.HasGeneralDiscount ? "İndirimi Düzenle" : "İndirim Ekle")
    </button>
</div>
```

Mantık: "İndirim düzenle" tıklandığında payment dialog'u kapanır, cart discount modal'ı açılır. Kullanıcı indirim kaydettiğinde cart yeniden render edilir; ödeme için "Ödeme Al" butonuna tekrar basılır → PaymentDialog güncel `GrandTotal` ile gelir.

- [ ] **Step 2: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml
git commit -m "feat(pos): payment dialog'a sepet indirimi özeti + Düzenle butonu"
```

---

## Task 11: CompleteSale'de pro-rata dağıtım + Mapperly + SaleManager

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs` (CompleteSale)
- Modify: `Application/Entegrasyon.Business/Mappers/SaleMapper.cs` (ignore list güncellemesi)
- Modify: `Application/Entegrasyon.Business/Concrete/SaleManager.cs` (log + tip)
- Test: `Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs` (yeni test)

- [ ] **Step 1: Failing integration-style unit test ekle**

Edit `Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs` — yeni test ekle:

```csharp
[Fact]
public async Task MakeSale_WithGeneralDiscount_PersistsToSaleEntity()
{
    // Arrange
    SetupDbContextForMakeSale([], null);

    var dto = new MakeSaleDto(
        SalePersonId: Guid.NewGuid(),
        CustomerId: null,
        GeneralDiscount: 100m,
        BranchOfficeId: 1,
        SaleSource: SaleSource.POS,
        Note: null,
        SaleItems: [new SaleItemDto(
            ProductVariantId: Guid.NewGuid(),
            TaxPercentage: 20,
            DiscountPercent: 0,
            UnitPrice: 1000m,
            Quantity: 1,
            DiscountVoucherCode: "",
            DiscountAmount: 83.33m)],  // pro-rata distributed net pay
        Payments: [new SalePaymentDto(PaymentMethodId: 1, Amount: 1100m, null, null)],
        GeneralDiscountReasonId: null,
        GeneralDiscountReasonNote: "pazarlık");

    // Act
    var result = await _sut.MakeSale(dto);

    // Assert
    result.Success.Should().BeTrue();
    // Sale entity kaydı incelenecek — mock DbContext üzerinden son eklenen Sale
    var savedSale = mockIntegrationDbContext.Object.Sales.Local.FirstOrDefault();
    savedSale.Should().NotBeNull();
    savedSale!.GeneralDiscount.Should().Be(100m);
    savedSale.GeneralDiscountReasonNote.Should().Be("pazarlık");
}
```

(Eğer `mockIntegrationDbContext.Object.Sales.Local` yoksa, mevcut testteki saved-sale pattern'ını takip et — muhtemelen başka bir mock setup ile Add çağrısı doğrulanıyor.)

- [ ] **Step 2: Testi çalıştır, fail olduğunu gör**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MakeSale_WithGeneralDiscount_PersistsToSaleEntity" --nologo`
Expected: FAIL (GeneralDiscountReasonNote hâlâ null veya SaleMapper ignore ediyor).

- [ ] **Step 3: Mapper'ı güncelle**

Edit `Application/Entegrasyon.Business/Mappers/SaleMapper.cs`:

```csharp
[MapperIgnoreTarget(nameof(Sale.DiscountVoucher))]
[MapperIgnoreTarget(nameof(Sale.SalePerson))]
[MapperIgnoreTarget(nameof(Sale.BranchOffice))]
[MapperIgnoreTarget(nameof(Sale.Customer))]
[MapperIgnoreTarget(nameof(Sale.Payments))]
[MapperIgnoreTarget(nameof(Sale.Returns))]
[MapperIgnoreTarget(nameof(Sale.SaleNumber))]
[MapperIgnoreTarget(nameof(Sale.SaleDate))]
[MapperIgnoreTarget(nameof(Sale.SaleStatus))]
[MapperIgnoreTarget(nameof(Sale.GeneralDiscountReason))]  // FK navigation
public partial Sale MapToEntity(MakeSaleDto dto);
```

Mapperly zaten isim uyumuyla `GeneralDiscount`, `GeneralDiscountReasonId`, `GeneralDiscountReasonNote`'u otomatik map eder.

- [ ] **Step 4: SaleManager'a log ekle**

Edit `Application/Entegrasyon.Business/Concrete/SaleManager.cs` — `MakeSale`'in son log'unun hemen öncesine:

```csharp
if (dto.GeneralDiscount > 0m)
{
    await applicationLogManager.AddLog(
        $"Sepet indirimi uygulandı: {dto.GeneralDiscount:N2} TL",
        LogType.Sale, LogAction.Add);
}
```

- [ ] **Step 5: CompleteSale'de pro-rata dağıtım**

Edit `Application/Entegrasyon.MVC/Features/POS/POSController.cs` — `CompleteSale` metodunu değiştir. `saleItems` oluşturma kısmını şu yapıya çevir:

```csharp
// Kalem indirimi uygulanmış satır bilgileri
var lines = cart.Items.Select(c => new CartLine(
    UnitPrice: c.UnitPrice,
    Quantity: c.Quantity,
    VatRate: c.VatRate,
    ExistingLineDiscountAmount: c.DiscountAmount ?? Math.Round(c.LineGross * (decimal)c.DiscountPercent / 100m, 2)
)).ToList();

var generalDiscountGross = cart.GeneralDiscountAmount;
var distribution = CartDiscountDistributor.Distribute(lines, generalDiscountGross);

var saleItems = cart.Items.Select((c, idx) =>
{
    var existingDiscount = c.DiscountAmount ?? Math.Round(c.LineGross * (decimal)c.DiscountPercent / 100m, 2);
    var newDiscountAmount = existingDiscount + distribution.PerLineNetShare[idx];

    return new SaleItemDto(
        ProductVariantId: c.VariantId,
        TaxPercentage: (double)c.VatRate,
        DiscountPercent: 0,                     // normalize: her şey DiscountAmount'a
        UnitPrice: c.UnitPrice,
        Quantity: c.Quantity,
        DiscountVoucherCode: "",
        DiscountAmount: newDiscountAmount > 0m ? newDiscountAmount : null,
        DiscountReasonId: c.DiscountReasonId,
        DiscountReasonNote: c.DiscountReasonNote);
}).ToList();

var makeSaleDto = new MakeSaleDto(
    SalePersonId: GetCurrentUserId(),
    CustomerId: GetCustomerIdFromSession(),
    GeneralDiscount: distribution.AppliedGrossTotal,   // actual uygulanan (clamp sonrası)
    BranchOfficeId: DefaultBranchOfficeId,
    SaleSource: SaleSource.POS,
    Note: null,
    SaleItems: saleItems,
    Payments: payments,
    GeneralDiscountReasonId: cart.GeneralDiscountReasonId,
    GeneralDiscountReasonNote: cart.GeneralDiscountReasonNote);
```

Gerekli using ekle: `using Entegrasyon.MVC.Features.POS;` (CartLine + CartDiscountDistributor için).

- [ ] **Step 6: Test çalıştır**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MakeSale_WithGeneralDiscount_PersistsToSaleEntity" --nologo`
Expected: PASS.

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --nologo`
Expected: tüm unit testler pass.

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/POSController.cs \
        Application/Entegrasyon.Business/Mappers/SaleMapper.cs \
        Application/Entegrasyon.Business/Concrete/SaleManager.cs \
        Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs
git commit -m "feat(pos): CompleteSale pro-rata dağıtım + mapper + log"
```

---

## Task 12: SaleDetail görünümüne sepet indirimi satırı

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`

- [ ] **Step 1: Mevcut toplam bölümünü oku**

Read: `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`

Ara toplam / KDV / Genel Toplam satırlarını bul.

- [ ] **Step 2: Genel indirim satırı ekle (GrandTotal satırından önce)**

```cshtml
@if (Model.GeneralDiscount > 0)
{
    <tr>
        <td class="fw-bold">Sepet İndirimi</td>
        <td class="text-end text-success">-@Model.GeneralDiscount.ToString("N2") TL</td>
    </tr>
}
```

(Tablo yapısına göre uyarla — mevcut Subtotal/VatTotal satırlarıyla aynı kolon yapısında.)

- [ ] **Step 3: Build + manual görüntü kontrolü**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --nologo`
Expected: success.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml
git commit -m "feat(sales): SaleDetail'de sepet indirimi satırı"
```

---

## Task 13: Integration test — end-to-end sepet indirimli satış

**Files:**
- Create: `Test/Entegrasyon.IntegrationTest/Sales/SaleWithGeneralDiscountTests.cs`

- [ ] **Step 1: Mevcut integration test pattern'ı incele**

Read bir mevcut test dosyası: `Test/Entegrasyon.IntegrationTest/Sales/UnifiedSalesViewTests.cs`, collection/fixture kullanımı için.

- [ ] **Step 2: Yeni test dosyasını yaz**

```csharp
// Test/Entegrasyon.IntegrationTest/Sales/SaleWithGeneralDiscountTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Sales;

[Collection(nameof(IntegrationTestCollection))]
public class SaleWithGeneralDiscountTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task MakeSale_WithGeneralDiscount_PersistsSaleWithCorrectTotals()
    {
        // Arrange: seed variant + branch office (fixture helper varsa kullan,
        // yoksa DbContext üzerinden elle ekle)
        using var scope = fixture.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var contextFactory = sp.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var saleManager = sp.GetRequiredService<ISaleManager>();

        Guid variantId;
        Guid userId;
        int branchId;
        using (var ctx = await contextFactory.CreateDbContextAsync())
        {
            // ... (fixture'a göre) variant + user + branch oluştur
            // Örnek: var variant = new ProductVariant { ... }; ctx.ProductVariants.Add(variant);
            // await ctx.SaveChangesAsync();
            // variantId = variant.Id; branchId = 1; userId = ...;
            throw new NotImplementedException("Fixture seed gerek");
        }

        var dto = new MakeSaleDto(
            SalePersonId: userId,
            CustomerId: null,
            GeneralDiscount: 120m,
            BranchOfficeId: branchId,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: [new SaleItemDto(
                ProductVariantId: variantId,
                TaxPercentage: 20,
                DiscountPercent: 0,
                UnitPrice: 1000m,
                Quantity: 1,
                DiscountVoucherCode: "",
                DiscountAmount: 100m)],
            Payments: [new SalePaymentDto(PaymentMethodId: 1, Amount: 1080m, null, null)]);

        // Act
        var result = await saleManager.MakeSale(dto);

        // Assert
        result.Success.Should().BeTrue();

        using var verifyCtx = await contextFactory.CreateDbContextAsync();
        var saved = await verifyCtx.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == result.Data);

        saved.Should().NotBeNull();
        saved!.GeneralDiscount.Should().Be(120m);
        saved.SaleItems.Should().HaveCount(1);
        saved.SaleItems.First().DiscountAmount.Should().Be(100m);
    }
}
```

Not: Test `IntegrationTestFixture`'ın mevcut seed altyapısına dayanır. Eğer seed helper'ları yoksa, `UnifiedSalesViewTests.cs`'teki setup'ı referans al ve ürün/kullanıcı/şube oluşturma helper'ı kopyala. `Pending: fixture seed` satırını gerçek seed koduyla değiştir.

- [ ] **Step 3: Testi çalıştır**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~SaleWithGeneralDiscountTests" --nologo`
Expected: PASS (Docker çalışır olmalı; fixture PostgreSQL container'ını başlatır).

- [ ] **Step 4: Commit**

```bash
git add Test/Entegrasyon.IntegrationTest/Sales/SaleWithGeneralDiscountTests.cs
git commit -m "test(integration): sepet indirimli satış end-to-end testi"
```

---

## Task 14: Manuel debug + tam regresyon

**Files:** (kod değişikliği yok)

Bu task kullanıcı-facing davranışı doğrular (CLAUDE.md "debug-before-done" kuralı).

- [ ] **Step 1: Dev sunucuyu başlat**

Run: `cd Application/Entegrasyon.MVC && dotnet run`
Beklenen: `http://localhost:5100` üzerinde açılır.

- [ ] **Step 2: Scenario 1 — Tek kalem yüzde indirim**

1. `/pos` aç, admin/123456789 ile giriş yap
2. Kasa oturumu aç (herhangi bir başlangıç kasa)
3. Bir ürün sepete ekle (1 adet, fiyat örn. 1.000 TL)
4. Footer'da "İndirim" butonuna bas
5. Modal açılır — `Yüzde` modu, `10` gir, "Uygula"
6. Beklenen: footer'da "Sepet İndirimi -120.00 TL" (varsayılan %20 KDV için gross=1200; %10=120) ve TOPLAM düşer
7. "Ödeme Al" butonuyla payment dialog'u aç, özet kısmında sepet indirimi satırı görünür
8. Ödemeyi tamamla → /sales/sale/{id} detay sayfası → "Sepet İndirimi -120.00 TL" görünür

- [ ] **Step 3: Scenario 2 — Tam hedef toplam (TL indirim)**

1. Yeni satış, 1500 TL'lik tutarda kalem(ler) ekle
2. Sepet İndirimi modal → `TL` modu, `250` gir, "Uygula"
3. Beklenen: TOPLAM tam `1.250,00 TL` (kuruş sapması yok)
4. Ödemeyi tamamla → SaleDetail'de ara toplam 1500, indirim -250, grand total 1250
5. Farklı KDV'li 2-3 kalem için tekrarla — TOPLAM her zaman tam hedefe düşmeli

- [ ] **Step 4: Scenario 3 — Kalem + sepet indirimi kombinasyonu**

1. Sepete 2 ürün ekle, birine kalem bazlı %10 indirim uygula
2. Sonra sepete genel 100 TL indirim uygula
3. Beklenen: her iki indirim de görünür, TOPLAM hem kalem hem sepet indirimi uygulanmış
4. Satışı tamamla → SaleDetail: her kalemin indirim tutarları birleşik (kalem + pro-rata pay)

- [ ] **Step 5: Scenario 4 — İade uyumu**

1. Tamamlanmış bir sepet indirimli satış seç
2. SaleDetail'den "İade Et" → bir kalem iade et
3. Beklenen: iade tutarı indirilmiş satır toplamına eşit (genel indirim payı düşülmüş)
4. İade sonrası: Sale.GeneralDiscount aynı kalır (bilgi alanı), iade RefundAmount doğru

- [ ] **Step 6: Scenario 5 — Edge case'ler**

1. Boş sepette "İndirim" butonu → toast "Sepet boş"
2. İndirim >= sepet toplamı girişi → toast hata
3. İndirim uygula → sepete yeni ürün ekle → footer otomatik güncellenir, TOPLAM yeniden hesaplanır
4. Sepet temizle ("Sepeti Temizle") → genel indirim de sıfırlanır
5. Payment dialog'da "Düzenle" → modal açılır → değiştir → cart güncellenir → yeniden "Ödeme Al"

- [ ] **Step 7: Tam test paketini çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --nologo
dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --nologo
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --nologo
```

Expected: tüm testler pass (mevcut testler dahil, regresyon yok).

- [ ] **Step 8: Final commit (varsa ufak düzeltmeler)**

```bash
git status
# Varsa küçük düzeltmeleri ekle ve commit et
```

---

## Self-Review (plan dokümanı yazarı için)

**Spec coverage:**
- Pro-rata dağıtım → Task 1 ✓
- Katmanlı kalem+sepet indirim etkileşimi → Task 1 (ExistingLineDiscountAmount) + Task 11 ✓
- UI konumu (cart altında + payment dialog'da düzenle) → Task 9 + Task 10 ✓
- Yüzde/TL giriş tipi → Task 7 + Task 8 ✓
- Opsiyonel sebep/not → Task 7 + Task 8 ✓
- `Sale.GeneralDiscount` double→decimal → Task 2 ✓
- Yeni `GeneralDiscountReasonId/Note` alanları → Task 2 + Task 3 ✓
- Pro-rata distribüsyon Controller'da → Task 11 ✓
- `SaleItem.DiscountAmount`'a birleşik yazılır → Task 11 ✓
- Sebep alanı sadece Sale'de → Task 2 ✓
- Session şeması POSCartVm → Task 6 ✓
- FluentValidation kuralları → Task 4 ✓
- EF migration → Task 3 ✓
- ApplicationLog entry → Task 11 ✓
- SaleDetail görünümü → Task 12 ✓
- Unit + Integration + Manuel testler → Task 1, 4, 11, 13, 14 ✓

E2E test spec'te listelenmişti ama bu planda atlandı — sebep: mevcut E2E altyapısı henüz POS senaryoları içermiyor, manuel debug (Task 14) kritik akışı doğruluyor. E2E tests sonraki sprint'te ayrı plan olarak yazılabilir.

**Placeholder scan:** Task 13 Step 2'de "Fixture seed gerek" için `throw new NotImplementedException` içeriyor — bu kasıtlı bir pointer; worker fixture'ın mevcut seed pattern'ına bakıp doldurmalı. Belirsiz olmaması için açıklama eklendi.

**Type consistency:** `CartLine`, `CartDiscountDistribution`, `POSCartVm` alanları Task 1, 5, 6, 11 arasında tutarlı.
