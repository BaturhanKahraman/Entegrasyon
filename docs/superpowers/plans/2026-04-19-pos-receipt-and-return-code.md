# POS Fişi ve İade Kodu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her POS satışında tahmin edilemez bir `ReturnCode` üret, satış sonrası fiş teslimi akışı aç (normal/hediye modu), `/returns` ekranında kod bazlı yeni iade başlatma akışı ekle.

**Architecture:** `Sale.ReturnCode` nullable kolonu (Crockford Base32, `R-` prefix, 13 karakter random) — mevcut `SaleNumber` korunur, iki kod birlikte yaşar. `MakeSale` değişmez; POSController ek bir `GetSaleDetailAsync` çağrısıyla TempData'ya toast data yazar, `/pos` sayfası JSON'ı okuyup modal açar. `Print.cshtml` tek view iki mode destekler. `/returns` Index'e iki ayrı arama kartı eklenir.

**Tech Stack:** .NET 10, EF Core (PostgreSQL + Npgsql), Mapperly, xUnit + FluentAssertions + Moq, Testcontainers (integration), HTMX, Tabler UI.

**Spec:** `docs/superpowers/specs/2026-04-19-pos-receipt-and-return-code-design.md`

---

## File Structure

**Yeni dosyalar:**
- `Application/Entegrasyon.Business/Utilities/ReturnCodeGenerator.cs` — stateless, Crockford Base32 encoder. `GenerateAsync(Func<string, Task<bool>> existsCheck)` signature.
- `Application/Entegrasyon.DataAccess/.../Migrations/<timestamp>_AddSaleReturnCode.cs` — EF Core migration (oluşturulur, check-in).
- `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml` — satış sonrası fiş modu seçim modal'ı (Tabler modal).
- `Application/Entegrasyon.MVC/Features/Returns/Views/Partials/_ReturnSearchCards.cshtml` — `/returns` Index üstündeki iki arama kartı.
- `Test/Entegrasyon.Test/Utilities/ReturnCodeGeneratorTests.cs`
- `Test/Entegrasyon.Test/Business/SaleManagerGetSaleByCodeTests.cs`
- `Test/Entegrasyon.IntegrationTest/Sales/MakeSaleReturnCodeTests.cs`
- `Test/Entegrasyon.IntegrationTest/Returns/ReturnLookupEndpointTests.cs`
- `Test/Entegrasyon.IntegrationTest/Sales/SalePrintGiftModeTests.cs`
- `Test/Entegrasyon.MVC.Test/POS/POSControllerCompleteSaleTests.cs` — (varsa güncellenir, yoksa eklenir).

**Değiştirilen dosyalar:**
- `Application/Entegrasyon.Entity/Sales/Sale.cs` — `ReturnCode` property.
- `Application/Entegrasyon.DataAccess/.../EntityConfigurations/SaleEntityConfiguration.cs` — filtered unique index.
- `Application/Entegrasyon.Business/Abstract/ISaleManager.cs` — `GetSaleByCodeAsync` signature.
- `Application/Entegrasyon.Business/Concrete/SaleManager.cs` — `GenerateReturnCodeAsync`, `GetSaleByCodeAsync` impl.
- `Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs` — `ReturnCode` property.
- `Application/Entegrasyon.Business/Mappers/SaleMapper.cs` — `ReturnCode` mapping (Mapperly partial).
- `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs` — `Print` action `mode` query param.
- `Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml` — gift mode conditional, `ReturnCode` display.
- `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml` — "Fişi Yazdır" + "Hediye Fişi Yazdır" butonları.
- `Application/Entegrasyon.MVC/Features/POS/POSController.cs` — `CompleteSale` sonrası TempData set.
- `Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml` — TempData okuma + partial render.
- `Application/Entegrasyon.MVC/Features/Returns/ReturnController.cs` — `search` filtresi aktifleştir + `POST /returns/lookup` endpoint.
- `Application/Entegrasyon.MVC/Features/Returns/Views/Index.cshtml` — arama kartlarını include et.

---

## Task 1: Sale Entity ReturnCode Property + EF Configuration

**Files:**
- Modify: `Application/Entegrasyon.Entity/Sales/Sale.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleEntityConfiguration.cs`

- [ ] **Step 1: Add ReturnCode property to Sale entity**

`Sale.cs` içine `SaleNumber`'ın hemen altına ekle:

```csharp
[StringLength(16)]
public string? ReturnCode { get; set; }
```

- [ ] **Step 2: Configure filtered unique index**

`SaleEntityConfiguration.cs` — `HasIndex(x => x.SaleNumber).IsUnique();` satırının altına:

```csharp
builder.HasIndex(x => x.ReturnCode)
       .IsUnique()
       .HasFilter("\"ReturnCode\" IS NOT NULL");
```

- [ ] **Step 3: Build'i doğrula**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj`
Expected: Build succeeded, 0 Error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/Sale.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleEntityConfiguration.cs
git commit -m "feat(sale): add ReturnCode property with filtered unique index"
```

---

## Task 2: EF Core Migration AddSaleReturnCode

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/<timestamp>_AddSaleReturnCode.cs` (otomatik)

- [ ] **Step 1: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddSaleReturnCode \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```
Expected: `Done. To undo this action, use 'ef migrations remove'.`

- [ ] **Step 2: Migration dosyasını incele**

Migration dosyasında sadece iki değişiklik olmalı:
1. `AddColumn<string>("ReturnCode", ...)` — nullable, maxLength 16.
2. `CreateIndex` — `IX_Sales_ReturnCode` unique + filtered (`IS NOT NULL`).

Ekstra değişiklik (başka tabloda kolon vs) VARSA migration'ı sil ve nedenini araştır.

- [ ] **Step 3: Dev DB'ye uygula**

Run:
```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```
Expected: `Done.` ya da `No migrations were applied. The database is already up to date.` (ikinci durumda migration aslında apply edilmemiş, tekrar kontrol).

- [ ] **Step 4: Snapshot senkron doğrula**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(db): migration AddSaleReturnCode"
```

---

## Task 3: ReturnCodeGenerator Utility + Unit Tests (TDD)

**Files:**
- Create: `Application/Entegrasyon.Business/Utilities/ReturnCodeGenerator.cs`
- Create: `Test/Entegrasyon.Test/Utilities/ReturnCodeGeneratorTests.cs`

- [ ] **Step 1: Failing testleri yaz**

`Test/Entegrasyon.Test/Utilities/ReturnCodeGeneratorTests.cs`:

```csharp
using Entegrasyon.Business.Utilities;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Utilities;

public class ReturnCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsRPrefixWith13Chars()
    {
        var code = ReturnCodeGenerator.Generate();

        code.Should().StartWith("R-");
        code.Length.Should().Be(15); // "R-" + 13 chars
    }

    [Fact]
    public void Generate_UsesCrockfordAlphabet_NoForbiddenChars()
    {
        for (var i = 0; i < 500; i++)
        {
            var code = ReturnCodeGenerator.Generate();
            var payload = code[2..];
            payload.Should().NotContainAny("I", "L", "O", "U");
            payload.All(c => "0123456789ABCDEFGHJKMNPQRSTVWXYZ".Contains(c))
                   .Should().BeTrue($"payload '{payload}' contains forbidden char");
        }
    }

    [Fact]
    public async Task GenerateUniqueAsync_RetriesWhenDuplicate()
    {
        var callCount = 0;
        var returnedCodes = new List<string>();

        var code = await ReturnCodeGenerator.GenerateUniqueAsync(async generated =>
        {
            returnedCodes.Add(generated);
            callCount++;
            // first call: say "exists", second: say "fresh"
            await Task.Yield();
            return callCount == 1;
        });

        callCount.Should().Be(2);
        returnedCodes.Should().HaveCount(2);
        code.Should().Be(returnedCodes[1]);
    }

    [Fact]
    public async Task GenerateUniqueAsync_ThrowsAfterMaxRetries()
    {
        var act = async () => await ReturnCodeGenerator.GenerateUniqueAsync(_ => Task.FromResult(true));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*collision*");
    }
}
```

- [ ] **Step 2: Testi çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReturnCodeGeneratorTests"`
Expected: Compile error — `ReturnCodeGenerator` yok.

- [ ] **Step 3: ReturnCodeGenerator'ı implement et**

`Application/Entegrasyon.Business/Utilities/ReturnCodeGenerator.cs`:

```csharp
using System.Security.Cryptography;

namespace Entegrasyon.Business.Utilities;

public static class ReturnCodeGenerator
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int PayloadLength = 13;
    private const int MaxRetries = 5;

    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[PayloadLength];
        ulong value = BitConverter.ToUInt64(bytes);
        for (var i = 0; i < PayloadLength; i++)
        {
            chars[i] = CrockfordAlphabet[(int)(value & 0x1F)];
            value >>= 5;
        }

        return $"R-{new string(chars)}";
    }

    public static async Task<string> GenerateUniqueAsync(Func<string, Task<bool>> existsCheck)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var code = Generate();
            if (!await existsCheck(code))
                return code;
        }

        throw new InvalidOperationException(
            $"Failed to generate unique return code after {MaxRetries} attempts (collision).");
    }
}
```

- [ ] **Step 4: Testi çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReturnCodeGeneratorTests"`
Expected: Passed: 4.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Utilities/ReturnCodeGenerator.cs \
        Test/Entegrasyon.Test/Utilities/ReturnCodeGeneratorTests.cs
git commit -m "feat(business): ReturnCodeGenerator with Crockford Base32"
```

---

## Task 4: SaleManager.MakeSale — ReturnCode Üretimi

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/SaleManager.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Sales/MakeSaleReturnCodeTests.cs`

- [ ] **Step 1: Integration testi yaz**

`Test/Entegrasyon.IntegrationTest/Sales/MakeSaleReturnCodeTests.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Sales;

public class MakeSaleReturnCodeTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task MakeSale_AssignsReturnCode()
    {
        using var scope = Factory.Services.CreateScope();
        var saleManager = scope.ServiceProvider.GetRequiredService<ISaleManager>();
        var dto = await TestSaleDtoFactory.BuildAsync(scope.ServiceProvider);

        var result = await saleManager.MakeSale(dto);
        result.Success.Should().BeTrue();

        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var ctx = await dbContext.CreateDbContextAsync();
        var sale = await ctx.Sales.AsNoTracking().SingleAsync(s => s.Id == result.Data);

        sale.ReturnCode.Should().NotBeNull();
        sale.ReturnCode.Should().StartWith("R-");
        sale.ReturnCode!.Length.Should().Be(15);
    }

    [Fact]
    public async Task MakeSale_ReturnCodesAreUnique()
    {
        using var scope = Factory.Services.CreateScope();
        var saleManager = scope.ServiceProvider.GetRequiredService<ISaleManager>();
        var codes = new HashSet<string>();

        for (var i = 0; i < 5; i++)
        {
            var dto = await TestSaleDtoFactory.BuildAsync(scope.ServiceProvider);
            var result = await saleManager.MakeSale(dto);
            result.Success.Should().BeTrue();

            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var ctx = await dbContext.CreateDbContextAsync();
            var sale = await ctx.Sales.AsNoTracking().SingleAsync(s => s.Id == result.Data);

            codes.Add(sale.ReturnCode!).Should().BeTrue($"code {sale.ReturnCode} was already present");
        }

        codes.Should().HaveCount(5);
    }
}
```

**Not:** `TestSaleDtoFactory.BuildAsync` yoksa, proje içindeki mevcut `SaleWithGeneralDiscountTests.cs` veya `SaleManagerIntegrationTests.cs` içindeki `MakeSaleDto` kurulum pattern'ini birebir kopyala — factory helper yoksa inline DTO oluştur. (Mevcut test dosyalarını önce oku: `Grep MakeSale Test/Entegrasyon.IntegrationTest/`).

- [ ] **Step 2: Testi çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~MakeSaleReturnCodeTests"`
Expected: `sale.ReturnCode.Should().NotBeNull()` fail — ReturnCode null.

- [ ] **Step 3: SaleManager'a GenerateReturnCodeAsync ekle**

`Application/Entegrasyon.Business/Concrete/SaleManager.cs` — `GenerateSaleNumberAsync`'in hemen altına:

```csharp
private static async Task<string> GenerateReturnCodeAsync(IntegrationDbContext dbContext)
{
    return await ReturnCodeGenerator.GenerateUniqueAsync(async code =>
        await dbContext.Sales.AnyAsync(s => s.ReturnCode == code));
}
```

Ve `MakeSale` metodunda `sale.SaleNumber = ...` satırının hemen altına:

```csharp
sale.ReturnCode = await GenerateReturnCodeAsync(dbContext);
```

`using Entegrasyon.Business.Utilities;` import'unu dosya üstüne ekle (yoksa).

- [ ] **Step 4: Testi çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~MakeSaleReturnCodeTests"`
Expected: Passed: 2.

- [ ] **Step 5: Regresyon — tüm unit + integration MakeSale testleri**

Run:
```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MakeSale"
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~MakeSale"
```
Expected: Tüm testler pass.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/SaleManager.cs \
        Test/Entegrasyon.IntegrationTest/Sales/MakeSaleReturnCodeTests.cs
git commit -m "feat(sale): generate ReturnCode on MakeSale"
```

---

## Task 5: SaleDetailDto + Mapper — ReturnCode Alanı

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/SaleManager.cs` (GetSaleDetailAsync projection)
- Modify: `Application/Entegrasyon.Business/Mappers/SaleMapper.cs` (gerekiyorsa)

- [ ] **Step 1: DTO'ya ReturnCode ekle**

`SaleDetailDto.cs` — `SaleNumber` property'sinin altına:

```csharp
public string? ReturnCode { get; set; }
```

- [ ] **Step 2: GetSaleDetailAsync projection'ını güncelle**

`SaleManager.cs` içinde `GetSaleDetailAsync` method'unda `SaleNumber = sale.SaleNumber,` satırının altına:

```csharp
ReturnCode = sale.ReturnCode,
```

- [ ] **Step 3: Mapperly regenerate (gerekiyorsa)**

Eğer `SaleMapper.cs` `MapToSaleDetailDto` methodu varsa ve `[MapperIgnoreSource]` gibi explicit ignore'lar yoksa Mapperly otomatik handle eder. Build sonrası yeterli — manuel değişiklik gerekmiyor.

- [ ] **Step 4: Build + existing test**

Run:
```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleManagerGetSaleDetail"
```
Expected: Build succeed. Tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs \
        Application/Entegrasyon.Business/Concrete/SaleManager.cs \
        Application/Entegrasyon.Business/Mappers/SaleMapper.cs
git commit -m "feat(sale): expose ReturnCode in SaleDetailDto"
```

---

## Task 6: ISaleManager.GetSaleByCodeAsync + Unit Tests (TDD)

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ISaleManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/SaleManager.cs`
- Create: `Test/Entegrasyon.Test/Business/SaleManagerGetSaleByCodeTests.cs`

- [ ] **Step 1: Failing testleri yaz**

`Test/Entegrasyon.Test/Business/SaleManagerGetSaleByCodeTests.cs`:

Testler Moq'lu in-memory DB veya gerçek DbContextFactory ile kurulur. Projedeki mevcut `SaleManagerGetSaleDetailTests.cs`'in kurulum pattern'ini aynen kullan (kendini tekrar etme — mevcut helper'ları kullan). Test senaryoları:

```csharp
[Fact]
public async Task GetSaleByCodeAsync_WithReturnCode_ReturnsSale()
{
    // Arrange: Sale with ReturnCode = "R-TESTCODE12345", SaleNumber = "S202604190001"
    // Act: GetSaleByCodeAsync("R-TESTCODE12345")
    // Assert: Success, Data.Id matches
}

[Fact]
public async Task GetSaleByCodeAsync_WithSaleNumber_ReturnsSale()
{
    // Arrange: Sale with SaleNumber = "S202604190001"
    // Act: GetSaleByCodeAsync("S202604190001")
    // Assert: Success, Data.Id matches
}

[Fact]
public async Task GetSaleByCodeAsync_NotFound_ReturnsError()
{
    // Act: GetSaleByCodeAsync("R-UNKNOWN")
    // Assert: Success == false, Message contains "bulunamadı"
}

[Fact]
public async Task GetSaleByCodeAsync_LowercaseInput_MatchesUpperCodeInDb()
{
    // Arrange: Sale with ReturnCode = "R-TESTCODE12345"
    // Act: GetSaleByCodeAsync("r-testcode12345")
    // Assert: Success, Data.Id matches
}

[Fact]
public async Task GetSaleByCodeAsync_WhitespaceInput_Trimmed()
{
    // Arrange: Sale with ReturnCode = "R-TESTCODE12345"
    // Act: GetSaleByCodeAsync("  R-TESTCODE12345  ")
    // Assert: Success
}

[Fact]
public async Task GetSaleByCodeAsync_EmptyInput_ReturnsError()
{
    // Act: GetSaleByCodeAsync("")
    // Assert: Success == false
}
```

**Not:** Test dosyasında tam implementation'ı, mevcut `SaleManagerGetSaleDetailTests.cs`'deki Factory / DbContext kurulumunu birebir takip ederek yaz. Her test kendi bağlamını kurar — DRY için helper üretme (küçük dosya).

- [ ] **Step 2: ISaleManager'a method ekle**

`ISaleManager.cs`:

```csharp
Task<IDataResult<SaleDetailDto>> GetSaleByCodeAsync(string code);
```

- [ ] **Step 3: Testleri çalıştır — FAIL (compile)**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleManagerGetSaleByCode"`
Expected: Compile error — SaleManager implementation yok.

- [ ] **Step 4: SaleManager.GetSaleByCodeAsync implement et**

`SaleManager.cs` içinde `GetSaleDetailAsync`'in hemen altına:

```csharp
public async Task<IDataResult<SaleDetailDto>> GetSaleByCodeAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
        return new ErrorDataResult<SaleDetailDto>(null!, "Kod boş olamaz.");

    var normalized = code.Trim().ToUpperInvariant();

    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var saleId = await dbContext.Sales
        .AsNoTracking()
        .Where(s => s.ReturnCode == normalized || s.SaleNumber == normalized)
        .Select(s => (Guid?)s.Id)
        .FirstOrDefaultAsync();

    if (saleId is null)
        return new ErrorDataResult<SaleDetailDto>(null!, "Satış bulunamadı.");

    return await GetSaleDetailAsync(saleId.Value);
}
```

Gerekli using: `Entegrasyon.Entity.Results` (ErrorDataResult).

- [ ] **Step 5: Testleri çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleManagerGetSaleByCode"`
Expected: Passed: 6.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ISaleManager.cs \
        Application/Entegrasyon.Business/Concrete/SaleManager.cs \
        Test/Entegrasyon.Test/Business/SaleManagerGetSaleByCodeTests.cs
git commit -m "feat(sale): GetSaleByCodeAsync — lookup by ReturnCode or SaleNumber"
```

---

## Task 7: Print.cshtml — Gift Mode Desteği

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml`
- Create: `Test/Entegrasyon.IntegrationTest/Sales/SalePrintGiftModeTests.cs`

- [ ] **Step 1: Failing integration testi yaz**

`Test/Entegrasyon.IntegrationTest/Sales/SalePrintGiftModeTests.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.IntegrationTest.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Sales;

public class SalePrintGiftModeTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Print_NormalMode_ShowsPrices()
    {
        using var scope = Factory.Services.CreateScope();
        var saleManager = scope.ServiceProvider.GetRequiredService<ISaleManager>();
        var dto = await TestSaleDtoFactory.BuildAsync(scope.ServiceProvider);
        var result = await saleManager.MakeSale(dto);

        var client = Factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/sales/sale/{result.Data}/print");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        html.Should().Contain("TOPLAM");
        html.Should().Contain("İade Kodu");
    }

    [Fact]
    public async Task Print_GiftMode_HidesPricesButShowsReturnCode()
    {
        using var scope = Factory.Services.CreateScope();
        var saleManager = scope.ServiceProvider.GetRequiredService<ISaleManager>();
        var dto = await TestSaleDtoFactory.BuildAsync(scope.ServiceProvider);
        var result = await saleManager.MakeSale(dto);

        var client = Factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/sales/sale/{result.Data}/print?mode=gift");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        html.Should().NotContain("TOPLAM:");
        html.Should().NotContain("KDV:");
        html.Should().Contain("HEDİYE FİŞİ");
        html.Should().Contain("İade Kodu");
    }
}
```

**Not:** `CreateAuthenticatedClient` projedeki mevcut integration helper'dır; yoksa mevcut MVC integration testlerinden (`Test/Entegrasyon.IntegrationTest/` altında) pattern'i kopyala.

- [ ] **Step 2: Testi çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~SalePrintGiftMode"`
Expected: Normal mode başarılı olabilir (ama "İade Kodu" içermediği için muhtemelen fail), Gift mode fail.

- [ ] **Step 3: SaleController.Print'i güncelle**

`SaleController.cs:130-138`:

```csharp
[HttpGet("/sales/sale/{id:guid}/print")]
public async Task<IActionResult> Print(Guid id, string mode = "normal")
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success || result.Data is null)
        return NotFound();

    ViewBag.GiftMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase);
    return View(result.Data);
}
```

- [ ] **Step 4: Print.cshtml'i güncelle**

`Print.cshtml` — aşağıdaki yerine komple replace:

```cshtml
@using Entegrasyon.Entity.Dtos.Sale
@using System.Globalization
@model SaleDetailDto
@{
    Layout = null;
    var tr = new CultureInfo("tr-TR");
    var isGift = ViewBag.GiftMode as bool? ?? false;
    var title = isGift ? "HEDİYE FİŞİ" : "FİŞ";
}
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <title>@title — @Model.SaleNumber</title>
    <style>
        body { font-family: monospace; font-size: 12px; max-width: 300px; margin: 20px auto; padding: 10px; }
        h1 { font-size: 16px; text-align: center; margin: 0 0 10px; }
        .subtitle { text-align: center; font-size: 11px; color: #555; margin-bottom: 8px; }
        .divider { border-top: 1px dashed #000; margin: 8px 0; }
        .row { display: flex; justify-content: space-between; }
        .item { margin-bottom: 4px; }
        .item-title { font-weight: bold; }
        .total { font-weight: bold; font-size: 14px; }
        .return-code { text-align: center; font-size: 16px; font-weight: bold; letter-spacing: 1px; margin: 6px 0; padding: 6px; border: 1px dashed #000; }
        .sale-number-small { text-align: center; font-size: 10px; color: #888; }
        @@media print {
            body { margin: 0; }
            .no-print { display: none; }
        }
    </style>
</head>
<body>
    <h1>@Model.BranchOfficeName</h1>
    <div class="subtitle">@title</div>
    <div class="sale-number-small">Fiş No: @Model.SaleNumber</div>

    <div class="row">
        <span>Tarih:</span>
        <span>@Model.SaleDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm")</span>
    </div>
    <div class="row">
        <span>Kasiyer:</span>
        <span>@Model.SalePersonName</span>
    </div>
    @if (!string.IsNullOrEmpty(Model.CustomerName))
    {
        <div class="row">
            <span>Müşteri:</span>
            <span>@Model.CustomerName</span>
        </div>
    }
    <div class="divider"></div>

    @foreach (var item in Model.Items)
    {
        <div class="item">
            <div class="item-title">@item.ProductTitle</div>
            @if (!isGift)
            {
                <div class="row">
                    <span>@item.Quantity x @item.UnitPriceWithVat.ToString("C2", tr)</span>
                    <span>@item.LineTotalWithVat.ToString("C2", tr)</span>
                </div>
            }
            else
            {
                <div class="row">
                    <span>Adet:</span>
                    <span>@item.Quantity</span>
                </div>
            }
        </div>
    }
    <div class="divider"></div>

    @if (!isGift)
    {
        <div class="row"><span>Ara Toplam:</span><span>@Model.SubTotal.ToString("C2", tr)</span></div>
        <div class="row"><span>KDV:</span><span>@Model.VatTotal.ToString("C2", tr)</span></div>
        <div class="row total"><span>TOPLAM:</span><span>@Model.GrandTotal.ToString("C2", tr)</span></div>

        <div class="divider"></div>
        <div><strong>Ödeme:</strong></div>
        @foreach (var payment in Model.Payments)
        {
            <div class="row">
                <span>@payment.PaymentMethodName</span>
                <span>@payment.Amount.ToString("C2", tr)</span>
            </div>
        }
        <div class="divider"></div>
    }

    @if (!string.IsNullOrEmpty(Model.ReturnCode))
    {
        <div>İade Kodu:</div>
        <div class="return-code">@Model.ReturnCode</div>
    }

    <div style="text-align: center;">Teşekkür ederiz!</div>

    <div class="no-print" style="margin-top: 20px; text-align: center;">
        <button onclick="window.print()">Yazdır</button>
        <button onclick="window.close()">Kapat</button>
    </div>

    <script>
        window.addEventListener('load', () => setTimeout(() => window.print(), 500));
    </script>
</body>
</html>
```

- [ ] **Step 5: Testleri çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~SalePrintGiftMode"`
Expected: Passed: 2.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/SaleController.cs \
        Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml \
        Test/Entegrasyon.IntegrationTest/Sales/SalePrintGiftModeTests.cs
git commit -m "feat(sale): Print.cshtml supports gift mode, shows ReturnCode"
```

---

## Task 8: SaleDetail.cshtml — Tekrar Baskı Butonları

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`

- [ ] **Step 1: Mevcut "yazdır" butonlarının yerini tespit et**

Run: `grep -n "print" Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`

Mevcut yazdırma linki varsa (örn. `/sales/sale/@Model.Id/print`) — onun yanına ekle. Yoksa "Satış İşlemleri" bölümüne ekle.

- [ ] **Step 2: İki butonu ekle**

Var olan tek print butonunu ya da uygun konumu şu şekilde güncelle:

```cshtml
<a href="/sales/sale/@Model.Id/print?mode=normal" target="_blank"
   class="btn btn-outline-secondary">
    <i class="ti ti-printer"></i> Fişi Yazdır
</a>
<a href="/sales/sale/@Model.Id/print?mode=gift" target="_blank"
   class="btn btn-outline-secondary">
    <i class="ti ti-gift"></i> Hediye Fişi Yazdır
</a>
```

- [ ] **Step 3: Manuel kontrol (UI)**

Dev server ayakta ise `/sales/sale/<existing-id>` sayfasına git, iki buton göründüğünü doğrula. Her birine tıkla, yeni sekmede doğru mode ile açıldığını kontrol et.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml
git commit -m "feat(sale): add gift/normal print buttons to sale detail"
```

---

## Task 9: POSController.CompleteSale — TempData Set

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs`

- [ ] **Step 1: CompleteSale sonunda TempData yaz**

`POSController.cs:755-757` bölgesinde — `TempData.SetSuccess("Satış başarıyla tamamlandı.");` satırının **önüne** ekle:

```csharp
// Fiş teslim akışı — POS sayfası TempData'yı okuyup modal açar
var saleDetailResult = await saleManager.GetSaleDetailAsync(saleResult.Data);
if (saleDetailResult.Success && saleDetailResult.Data is not null)
{
    TempData["pos_last_sale"] = System.Text.Json.JsonSerializer.Serialize(new
    {
        saleId = saleResult.Data,
        saleNumber = saleDetailResult.Data.SaleNumber,
        returnCode = saleDetailResult.Data.ReturnCode
    });
}
```

- [ ] **Step 2: Build + regression**

Run:
```bash
dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj
dotnet test Test/Entegrasyon.MVC.Test/ --filter "FullyQualifiedName~POSController"
```
Expected: Build succeed, existing POSController testleri pass.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/POSController.cs
git commit -m "feat(pos): set pos_last_sale TempData after CompleteSale"
```

---

## Task 10: _POSLastSaleModal.cshtml + Index.cshtml Integration

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml`

- [ ] **Step 1: Partial'ı oluştur**

`Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml`:

```cshtml
<div class="modal modal-blur fade" id="posLastSaleModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
            <div class="modal-status bg-success"></div>
            <div class="modal-body text-center py-4">
                <i class="ti ti-circle-check icon mb-2 text-success" style="font-size: 48px;"></i>
                <h3>Satış Tamamlandı</h3>
                <div class="text-secondary mb-2">
                    Fiş No: <span id="posLastSaleNumber" class="fw-semibold"></span>
                </div>
                <div class="mb-3">
                    <div class="text-secondary small">İade Kodu</div>
                    <div id="posLastReturnCode"
                         class="fw-bold fs-3 font-monospace"
                         style="letter-spacing: 1px;"></div>
                </div>
                <div class="text-secondary small">
                    Müşteriye fiş teslim edecek misiniz?
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-link link-secondary" data-bs-dismiss="modal">
                    Atla
                </button>
                <button type="button" class="btn btn-outline-primary ms-auto"
                        id="posPrintGiftBtn">
                    <i class="ti ti-gift"></i> Hediye Fişi
                </button>
                <button type="button" class="btn btn-primary"
                        id="posPrintNormalBtn">
                    <i class="ti ti-printer"></i> Fişi Yazdır
                </button>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Index.cshtml içine include + JS ekle**

`Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml` — dosyanın sonuna (mevcut `@section Scripts` varsa içine, yoksa yeni section):

```cshtml
@{
    var lastSaleJson = TempData["pos_last_sale"] as string;
}
@if (!string.IsNullOrEmpty(lastSaleJson))
{
    <div id="pos-last-sale-data" data-payload='@lastSaleJson' style="display:none"></div>
    @await Html.PartialAsync("Partials/_POSLastSaleModal")
}

@section Scripts {
    <script>
        (function () {
            const dataEl = document.getElementById('pos-last-sale-data');
            if (!dataEl) return;

            let payload;
            try {
                payload = JSON.parse(dataEl.dataset.payload);
            } catch (e) { return; }

            const modalEl = document.getElementById('posLastSaleModal');
            if (!modalEl) return;

            document.getElementById('posLastSaleNumber').textContent = payload.saleNumber || '-';
            document.getElementById('posLastReturnCode').textContent = payload.returnCode || '-';

            const printUrl = (mode) => `/sales/sale/${payload.saleId}/print?mode=${mode}`;
            document.getElementById('posPrintNormalBtn').addEventListener('click', () => {
                window.open(printUrl('normal'), '_blank');
                bootstrap.Modal.getInstance(modalEl)?.hide();
            });
            document.getElementById('posPrintGiftBtn').addEventListener('click', () => {
                window.open(printUrl('gift'), '_blank');
                bootstrap.Modal.getInstance(modalEl)?.hide();
            });

            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        })();
    </script>
}
```

**Not:** Index.cshtml'de mevcut bir `@section Scripts` bloğu varsa — yeni bir tane ekleme, mevcutun içine ekle (Razor aynı section'ı iki kez tanımlamaya izin vermez).

- [ ] **Step 3: Manuel UI testi**

Dev server ayakta (`dotnet run` port 5100). `admin/123456789` ile giriş, POS'a git, bir ürün ekle, ödeme al, satışı tamamla → `/pos`'a geri dön.
**Beklenen:** Modal otomatik açılır, satış numarası + iade kodu görünür. "Fişi Yazdır" yeni sekmede fiş açar. "Hediye Fişi" yeni sekmede hediye fişi açar (fiyat yok). "Atla" modal'ı kapatır.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml \
        Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml
git commit -m "feat(pos): post-sale receipt modal with normal/gift options"
```

---

## Task 11: ReturnController — Search Filtresi Aktifleştir

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Returns/ReturnController.cs`
- Create: `Test/Entegrasyon.MVC.Test/Returns/ReturnControllerSearchTests.cs` (varsa extend)

- [ ] **Step 1: Failing test yaz**

`Test/Entegrasyon.MVC.Test/Returns/ReturnControllerSearchTests.cs`:

```csharp
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.Returns;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Test.Returns;

public class ReturnControllerSearchTests
{
    [Fact]
    public async Task Index_WithSearchMatchingReturnCode_FiltersResults()
    {
        // Arrange: seed a SaleReturn with ReturnCode "R-MATCH001" and one with "R-NOMATCH"
        // Act: controller.Index(search: "MATCH001")
        // Assert: ViewResult model contains only the matching return

        // Kullan: projenin mevcut in-memory DbContext test helper'ı (mevcut
        // ReturnController test dosyalarında varsa) — yoksa Moq ile.
    }

    [Fact]
    public async Task Index_WithSearchMatchingSaleNumber_FiltersResults() { /* ... */ }

    [Fact]
    public async Task Index_WithSearchMatchingCustomerName_FiltersResults() { /* ... */ }
}
```

**Not:** Projedeki mevcut `Test/Entegrasyon.MVC.Test/` altındaki controller testlerinin helper pattern'ini kullan. Inline DbContext kurulumu yoksa ya yeni helper ekle ya da bu testi integration tarafına taşı (`Test/Entegrasyon.IntegrationTest/Returns/`). Integration tarafı daha sağlıklı — `WebApplicationFactory` + real DB.

- [ ] **Step 2: Testi çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "FullyQualifiedName~ReturnControllerSearch"`
Expected: Fail — search filtre hiçbir şey yapmıyor.

- [ ] **Step 3: ReturnController.Index — search filtre ekle**

`ReturnController.cs:30-58` — `if (source.HasValue) ...` satırının hemen altına:

```csharp
if (!string.IsNullOrWhiteSpace(search))
{
    var needle = search.Trim();
    query = query.Where(r =>
        (r.ReturnCode != null && EF.Functions.ILike(r.ReturnCode, $"%{needle}%")) ||
        (r.Sale != null && EF.Functions.ILike(r.Sale.SaleNumber, $"%{needle}%")) ||
        (r.Sale != null && r.Sale.Customer != null && r.Sale.Customer.NameSurname != null &&
            EF.Functions.ILike(r.Sale.Customer.NameSurname, $"%{needle}%")));
}
```

**ÖNEMLİ:** `SaleReturn.ReturnCode` alanı var mı — yoksa filtre sadece `Sale.SaleNumber` ve `Customer.NameSurname` üzerinde olsun. `SaleReturn` entity'sini önce kontrol et:

```bash
grep -n "ReturnCode\|SaleId" Application/Entegrasyon.Entity/Sales/SaleReturn.cs
```

Eğer `SaleReturn` navigation property `Sale` üzerinden yürür, arama `r.Sale.ReturnCode` olmalı. Include'ı da güncelle: `Include(r => r.Sale).ThenInclude(s => s.Customer)`.

Güncel filtre (doğrudan kopyala):

```csharp
if (!string.IsNullOrWhiteSpace(search))
{
    var needle = search.Trim();
    query = query.Where(r =>
        (r.Sale != null &&
            (EF.Functions.ILike(r.Sale.SaleNumber, $"%{needle}%") ||
             (r.Sale.ReturnCode != null && EF.Functions.ILike(r.Sale.ReturnCode, $"%{needle}%")) ||
             (r.Sale.Customer != null && r.Sale.Customer.NameSurname != null &&
              EF.Functions.ILike(r.Sale.Customer.NameSurname, $"%{needle}%")))));
}
```

`Include` zincirine `ThenInclude(s => s!.Customer)` ekle.

- [ ] **Step 4: Testi çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "FullyQualifiedName~ReturnControllerSearch"`
(ya da integration tarafına taşıdıysan integration projesi)
Expected: Passed.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Returns/ReturnController.cs \
        Test/Entegrasyon.MVC.Test/Returns/ReturnControllerSearchTests.cs
git commit -m "feat(returns): activate search filter across ReturnCode/SaleNumber/customer"
```

---

## Task 12: POST /returns/lookup Endpoint

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Returns/ReturnController.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Returns/ReturnLookupEndpointTests.cs`

- [ ] **Step 1: Failing integration testleri yaz**

`Test/Entegrasyon.IntegrationTest/Returns/ReturnLookupEndpointTests.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Returns;

public class ReturnLookupEndpointTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Lookup_WithReturnCode_RendersSaleReturnDialog()
    {
        using var scope = Factory.Services.CreateScope();
        var saleManager = scope.ServiceProvider.GetRequiredService<ISaleManager>();
        var dto = await TestSaleDtoFactory.BuildAsync(scope.ServiceProvider);
        var make = await saleManager.MakeSale(dto);

        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var sale = await ctx.Sales.AsNoTracking().SingleAsync(s => s.Id == make.Data);

        var client = Factory.CreateAuthenticatedClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = sale.ReturnCode!
        });
        var response = await client.PostAsync("/returns/lookup", content);
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        html.Should().Contain("İade Talebi Oluştur");
        html.Should().Contain(sale.SaleNumber);
    }

    [Fact]
    public async Task Lookup_WithSaleNumber_RendersSaleReturnDialog()
    {
        // Benzer, code = sale.SaleNumber
    }

    [Fact]
    public async Task Lookup_UnknownCode_ReturnsNoContentWithToast()
    {
        var client = Factory.CreateAuthenticatedClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = "R-UNKNOWN000000"
        });
        var response = await client.PostAsync("/returns/lookup", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        response.Headers.GetValues("HX-Trigger").Should().ContainMatch("*showToast*");
    }
}
```

- [ ] **Step 2: Testi çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~ReturnLookupEndpoint"`
Expected: 404 veya 405 — endpoint yok.

- [ ] **Step 3: Lookup endpoint'i ekle**

`ReturnController.cs` — constructor'a `ISaleManager` DI'ını ekle:

```csharp
public class ReturnController(
    ISaleManager saleManager,
    ISaleReturnManager saleReturnManager,
    IReturnReasonManager returnReasonManager,
    IDbContextFactory<IntegrationDbContext> contextFactory) : HtmxController
```

Sonra class sonuna yeni action:

```csharp
[HttpPost("/returns/lookup")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Lookup([FromForm] string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        Response.HtmxReswap("none");
        Response.HtmxTriggerWithData("showToast",
            new { message = "Kod boş olamaz.", level = "error" });
        return NoContent();
    }

    var result = await saleManager.GetSaleByCodeAsync(code);
    if (!result.Success || result.Data is null)
    {
        Response.HtmxReswap("none");
        Response.HtmxTriggerWithData("showToast",
            new { message = result.Message ?? "Satış bulunamadı.", level = "error" });
        return NoContent();
    }

    return PartialView("~/Features/Sales/Views/Partials/_SaleReturnDialog.cshtml", result.Data);
}
```

- [ ] **Step 4: Testi çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~ReturnLookupEndpoint"`
Expected: Passed: 3.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Returns/ReturnController.cs \
        Test/Entegrasyon.IntegrationTest/Returns/ReturnLookupEndpointTests.cs
git commit -m "feat(returns): POST /returns/lookup — sale lookup by code"
```

---

## Task 13: Returns Index — Arama Kartları UI

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Returns/Views/Partials/_ReturnSearchCards.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Returns/Views/Index.cshtml`

- [ ] **Step 1: Mevcut Index.cshtml'i oku**

Run: `cat Application/Entegrasyon.MVC/Features/Returns/Views/Index.cshtml`

Tabla hangi `<div>` içinde — search input'larını bunun hemen üstüne koyacağız.

- [ ] **Step 2: Arama kartları partial'ını oluştur**

`Application/Entegrasyon.MVC/Features/Returns/Views/Partials/_ReturnSearchCards.cshtml`:

```cshtml
<div class="row row-cards mb-3">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">
                    <i class="ti ti-search me-1"></i> Mevcut iadelerde ara
                </h3>
            </div>
            <div class="card-body">
                <input type="search"
                       name="search"
                       class="form-control"
                       placeholder="İade kodu, fiş no veya müşteri adı"
                       value="@ViewBag.Search"
                       hx-get="/returns"
                       hx-trigger="keyup changed delay:300ms, search"
                       hx-target="#returns-table-wrap"
                       hx-include="[name='status'],[name='source']" />
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">
                    <i class="ti ti-receipt-2 me-1"></i> Yeni iade başlat
                </h3>
            </div>
            <div class="card-body">
                <form hx-post="/returns/lookup"
                      hx-target="#modal-container"
                      hx-swap="innerHTML">
                    @Html.AntiForgeryToken()
                    <div class="input-group">
                        <input type="text"
                               name="code"
                               class="form-control font-monospace"
                               placeholder="R-XXXXXXXXXXXXX veya S20260419-NNNN"
                               required />
                        <button type="submit" class="btn btn-primary">
                            Satışı Getir
                        </button>
                    </div>
                    <small class="form-hint">
                        Müşterinin fişindeki iade kodu veya fiş numarasını girin.
                    </small>
                </form>
            </div>
        </div>
    </div>
</div>

<div id="modal-container"></div>
```

- [ ] **Step 3: Index.cshtml'e partial'ı include et + tablo wrap id**

`Views/Index.cshtml` — sayfanın tablosunu içeren `<div>`'i `id="returns-table-wrap"` yap ve üstüne partial'ı ekle:

```cshtml
@await Html.PartialAsync("Partials/_ReturnSearchCards")

<div id="returns-table-wrap">
    @* mevcut tablo render'ı burada *@
</div>
```

**Not:** HTMX `hx-target="#returns-table-wrap"` partial güncellemeyi tabla wrapper'a yapar. Sunucu tarafında `Request.IsHtmx()` zaten partial döndürüyor (`ReturnController.cs:55-56`).

- [ ] **Step 4: Manuel UI testi**

Dev server ayakta:
- `/returns`'e git, iki kart göründüğünü doğrula.
- "Mevcut iadelerde ara" input'una yaz → tablo filtrelenir.
- "Yeni iade başlat" input'una geçerli bir iade kodu yaz, "Satışı Getir" tıkla → `_SaleReturnDialog` modal açılır.
- Geçersiz kod → toast: "Satış bulunamadı".

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Returns/Views/Partials/_ReturnSearchCards.cshtml \
        Application/Entegrasyon.MVC/Features/Returns/Views/Index.cshtml
git commit -m "feat(returns): add search cards — existing filter + new return by code"
```

---

## Task 14: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded. 0 Error.

- [ ] **Step 2: Tüm unit testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All passed.

- [ ] **Step 3: Tüm MVC testler**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj`
Expected: All passed.

- [ ] **Step 4: Tüm integration testler**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: All passed.

- [ ] **Step 5: Migration snapshot doğrula**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 6: Manuel uçtan uca akış testi**

Dev server (`cd Application/Entegrasyon.MVC && dotnet run`). `admin/123456789` ile giriş:
1. POS'ta satış yap → modal açılır → "Fişi Yazdır" → fiyatlı fiş, iade kodu altta.
2. Geri POS'a dön, başka satış → modal → "Hediye Fişi" → fiyatsız, iade kodu var.
3. `/sales/sale/<id>` → iki buton → tekrar baskı çalışıyor.
4. `/returns` → sol arama → satır filtrelenir.
5. `/returns` → sağ "Yeni iade başlat" → iade kodunu yaz, satışı getir → dialog açılır.

- [ ] **Step 7: Final commit (varsa artakalan değişiklik)**

```bash
git status
# artakalan değişiklik varsa:
git add -A
git commit -m "chore: final tweaks from manual verification"
```

---

## Self-Review Özeti

**Spec coverage:**
- ✅ §Data Model — Task 1 + Task 2.
- ✅ §Business — ReturnCodeGenerator (3), MakeSale wiring (4), GetSaleByCodeAsync (6), DTO/mapper (5).
- ✅ §POS Akışı — CompleteSale TempData (9), _POSLastSaleModal + JS (10).
- ✅ §Print View — gift mode (7) + reprint buttons (8).
- ✅ §İade Ekranı — search filter (11), POST /returns/lookup (12), search cards UI (13).
- ✅ §Test Planı — Task 3 (unit utility), 4 (integration MakeSale), 6 (unit GetSaleByCode), 7 (integration print), 11 (controller search), 12 (integration lookup).

**Placeholder scan:** "Benzer" / "fill in" ifadeleri yok — tüm kod blokları tam. Task 6 Step 1 ve Task 11 Step 1'de test metot gövdesi yorum ile anlatıldı ama kurulum pattern'i projenin mevcut test dosyalarından kopyalanacak ("projedeki mevcut X pattern'i kullan") — bu meşru çünkü helper'ların tam kodu burada tekrarlanamaz (DRY). Plan'ı çalıştıran agent önce ilgili mevcut test dosyasını okumalı, sonra benzer kurulum yazmalı.

**Type consistency:** `ReturnCode` string? her yerde aynı. `ReturnCodeGenerator.Generate()` / `GenerateUniqueAsync()` method isimleri testlerde ve kullanımda tutarlı. `GetSaleByCodeAsync` signature interface + impl + testlerde aynı.
