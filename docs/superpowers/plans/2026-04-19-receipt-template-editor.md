# Fiş Şablonu Editörü Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tenant başına özelleştirilebilir fiş şablonu (Thermal 300px + A4 5-zone), drag-drop editör, MinIO logo upload, 11 blok tipi destekli `IReceiptRenderer` servisi.

**Architecture:** Tek satırlık `ReceiptTemplate` entity (JSON kolonlar), blok bazlı render servisi + editör UI (SortableJS + Tabler). `Print.cshtml` yeni servisin ürettiği HTML'i `@Html.Raw` ile basıyor — mevcut davranış korunuyor, şablon runtime'da devreye giriyor.

**Tech Stack:** .NET 10, C# 13, EF Core 10 (PostgreSQL jsonb), Mapperly, SortableJS, Tabler UI, MinIO (`IImageManager`), xUnit + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-04-19-receipt-template-editor-design.md`

**Fedora/Podman uyarısı:** Integration testler bu makinede çalışmaz (memory `reference_podman_testcontainers.md`). Test dosyaları CI için yazılır; lokal doğrulama build + snapshot drift + unit testler ile yapılır.

---

## File Structure

**Yeni dosyalar:**
- `Application/Entegrasyon.Entity/Receipts/ReceiptTemplate.cs`
- `Application/Entegrasyon.Entity/Receipts/ReceiptMode.cs`
- `Application/Entegrasyon.Entity/Receipts/ReceiptSize.cs`
- `Application/Entegrasyon.Entity/Dtos/Receipts/ReceiptBlockDto.cs`
- `Application/Entegrasyon.Entity/Dtos/Receipts/ReceiptTemplateDto.cs`
- `Application/Entegrasyon.Entity/Dtos/Receipts/UpdateReceiptTemplateDto.cs`
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ReceiptTemplateEntityConfiguration.cs`
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/<ts>_AddReceiptTemplateTable.cs`
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/<ts>_SeedDefaultReceiptTemplate.cs`
- `Application/Entegrasyon.Business/Abstract/IReceiptTemplateManager.cs`
- `Application/Entegrasyon.Business/Concrete/ReceiptTemplateManager.cs`
- `Application/Entegrasyon.Business/Abstract/IReceiptRenderer.cs`
- `Application/Entegrasyon.Business/Concrete/ReceiptRenderer.cs`
- `Application/Entegrasyon.Business/Concrete/ReceiptBlockRenderers.cs`
- `Application/Entegrasyon.Business/Validation/ReceiptTemplateJsonValidator.cs`
- `Application/Entegrasyon.MVC/Features/Settings/ReceiptTemplateController.cs`
- `Application/Entegrasyon.MVC/Features/Settings/ViewModels/ReceiptTemplateEditorVm.cs`
- `Application/Entegrasyon.MVC/Features/Settings/Views/ReceiptTemplate/Index.cshtml`
- `Application/Entegrasyon.MVC/Features/Settings/Views/ReceiptTemplate/Partials/_Palette.cshtml`
- `Application/Entegrasyon.MVC/wwwroot/css/receipt-thermal.css`
- `Application/Entegrasyon.MVC/wwwroot/css/receipt-a4.css`
- `Application/Entegrasyon.MVC/wwwroot/js/receipt-editor.js`
- `Test/Entegrasyon.Test/Business/ReceiptTemplateJsonValidatorTests.cs`
- `Test/Entegrasyon.Test/Business/ReceiptBlockRenderersTests.cs`
- `Test/Entegrasyon.Test/Business/ReceiptRendererTests.cs`
- `Test/Entegrasyon.IntegrationTest/Receipts/ReceiptTemplateEndpointTests.cs`

**Değiştirilen dosyalar:**
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml` (tam rewrite)
- `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`
- `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml`
- `Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml`

---

## Task 1: ReceiptTemplate Entity + EF Configuration

**Files:** `Application/Entegrasyon.Entity/Receipts/{ReceiptTemplate.cs, ReceiptMode.cs, ReceiptSize.cs}`, `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ReceiptTemplateEntityConfiguration.cs`, `IntegrationDbContext.cs`.

- [ ] **Step 1: Enum dosyaları**

`ReceiptMode.cs`:
```csharp
namespace Entegrasyon.Entity.Receipts;

public enum ReceiptMode { Normal = 0, Gift = 1 }
```

`ReceiptSize.cs`:
```csharp
namespace Entegrasyon.Entity.Receipts;

public enum ReceiptSize { Thermal = 0, A4 = 1 }
```

- [ ] **Step 2: ReceiptTemplate entity**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Receipts;

public sealed class ReceiptTemplate : BaseEntity
{
    public int Id { get; set; }
    public string ThermalJson { get; set; } = "[]";
    public string A4Json { get; set; } = "{}";
    [StringLength(500)] public string? LogoUrl { get; set; }
    public int LogoWidthPx { get; set; } = 120;
    [StringLength(100)] public string StoreName { get; set; } = "";
    [StringLength(200)] public string StoreAddress { get; set; } = "";
    [StringLength(20)] public string StorePhone { get; set; } = "";
}
```

- [ ] **Step 3: EF configuration**

```csharp
using Entegrasyon.Entity.Receipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ReceiptTemplateEntityConfiguration : IEntityTypeConfiguration<ReceiptTemplate>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplate> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.ThermalJson).HasColumnType("jsonb");
        builder.Property(x => x.A4Json).HasColumnType("jsonb");
    }
}
```

- [ ] **Step 4: DbContext güncellemesi**

`IntegrationDbContext.cs` içine (using'lere `Entegrasyon.Entity.Receipts;` ekle, DbSet'lerin yanına):
```csharp
public DbSet<ReceiptTemplate> ReceiptTemplates { get; set; } = null!;
```

- [ ] **Step 5: Build**

Run: `dotnet build Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj`
Expected: 0 Error.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Entity/Receipts/ \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ReceiptTemplateEntityConfiguration.cs \
        Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs
git commit -m "feat(receipt): ReceiptTemplate entity + EF config + DbSet"
```

---

## Task 2: Migrations (Create Table + Seed Default)

- [ ] **Step 1: Migration oluştur — table**

```bash
dotnet ef migrations add AddReceiptTemplateTable \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 2: Migration incele** — sadece `CreateTable("ReceiptTemplates", ...)` olmalı.

- [ ] **Step 3: Seed migration oluştur**

```bash
dotnet ef migrations add SeedDefaultReceiptTemplate \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Migration dosyasının `Up` metodunu şu ile REPLACE et:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    var thermalJson = """
    [
      { "id": "b-logo",   "type": "logo",        "showInNormal": true,  "showInGift": true,  "settings": {} },
      { "id": "b-store",  "type": "store_info",  "showInNormal": true,  "showInGift": true,  "settings": { "align": "center", "size": "m" } },
      { "id": "b-div1",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
      { "id": "b-meta",   "type": "meta",        "showInNormal": true,  "showInGift": true,  "settings": { "showSaleNumber": true, "showDate": true, "showCashier": true, "showCustomer": true } },
      { "id": "b-div2",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
      { "id": "b-items",  "type": "items",       "showInNormal": true,  "showInGift": true,  "settings": { "showBarcode": false, "showVatColumn": false } },
      { "id": "b-div3",   "type": "divider",     "showInNormal": true,  "showInGift": false, "settings": { "style": "dashed", "color": "black" } },
      { "id": "b-totals", "type": "totals",      "showInNormal": true,  "showInGift": false, "settings": {} },
      { "id": "b-vat",    "type": "vat_summary", "showInNormal": true,  "showInGift": false, "settings": {} },
      { "id": "b-pay",    "type": "payments",    "showInNormal": true,  "showInGift": false, "settings": {} },
      { "id": "b-div4",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
      { "id": "b-return", "type": "return_code", "showInNormal": true,  "showInGift": true,  "settings": { "showLabel": true } },
      { "id": "b-thanks", "type": "text",        "showInNormal": true,  "showInGift": true,  "settings": { "content": "Teşekkür ederiz!", "align": "center", "bold": false, "size": "m" } }
    ]
    """;

    var a4Json = """
    {
      "hl": [ { "id": "a-logo", "type": "logo", "showInNormal": true, "showInGift": true, "settings": {} } ],
      "hr": [ { "id": "a-store", "type": "store_info", "showInNormal": true, "showInGift": true, "settings": { "align": "left", "size": "m" } } ],
      "body": [
        { "id": "a-divb",   "type": "divider",     "showInNormal": true, "showInGift": true, "settings": { "style": "solid", "color": "black" } },
        { "id": "a-meta",   "type": "meta",        "showInNormal": true, "showInGift": true, "settings": { "showSaleNumber": true, "showDate": true, "showCashier": true, "showCustomer": true } },
        { "id": "a-items",  "type": "items",       "showInNormal": true, "showInGift": true, "settings": { "showBarcode": true, "showVatColumn": true } },
        { "id": "a-totals", "type": "totals",      "showInNormal": true, "showInGift": false, "settings": {} },
        { "id": "a-vat",    "type": "vat_summary", "showInNormal": true, "showInGift": false, "settings": {} }
      ],
      "fl": [
        { "id": "a-pay",    "type": "payments",    "showInNormal": true, "showInGift": false, "settings": {} },
        { "id": "a-divf",   "type": "divider",     "showInNormal": true, "showInGift": true,  "settings": { "style": "solid", "color": "black" } },
        { "id": "a-return", "type": "return_code", "showInNormal": true, "showInGift": true,  "settings": { "showLabel": true } }
      ],
      "fr": [
        { "id": "a-thanks", "type": "text", "showInNormal": true, "showInGift": true, "settings": { "content": "Teşekkür ederiz!", "align": "right", "bold": false, "size": "m" } },
        { "id": "a-sign",   "type": "text", "showInNormal": true, "showInGift": false, "settings": { "content": "Müşteri imzası: ______________", "align": "right", "bold": false, "size": "s" } }
      ]
    }
    """;

    migrationBuilder.InsertData(
        table: "ReceiptTemplates",
        columns: ["Id", "ThermalJson", "A4Json", "LogoUrl", "LogoWidthPx", "StoreName", "StoreAddress", "StorePhone", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt"],
        values: [1, thermalJson, a4Json, null, 120, "", "", "", false, null, DateTimeOffset.UtcNow, null]);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DELETE FROM \"ReceiptTemplates\" WHERE \"Id\" = 1;");
}
```

- [ ] **Step 4: DB'ye uygula**

```bash
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```

- [ ] **Step 5: Snapshot drift check**

```bash
dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(db): ReceiptTemplate table + default seed"
```

---

## Task 3: DTOs + Block Type Constants

**Files:** `Application/Entegrasyon.Entity/Dtos/Receipts/{ReceiptBlockDto.cs, ReceiptTemplateDto.cs, UpdateReceiptTemplateDto.cs}`

- [ ] **Step 1: ReceiptBlockDto + block types**

```csharp
using System.Text.Json.Nodes;

namespace Entegrasyon.Entity.Dtos.Receipts;

public sealed class ReceiptBlockDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public bool ShowInNormal { get; set; } = true;
    public bool ShowInGift { get; set; } = true;
    public JsonObject Settings { get; set; } = new();
}

public sealed class A4ReceiptBlocksDto
{
    public List<ReceiptBlockDto> Hl { get; set; } = [];
    public List<ReceiptBlockDto> Hr { get; set; } = [];
    public List<ReceiptBlockDto> Body { get; set; } = [];
    public List<ReceiptBlockDto> Fl { get; set; } = [];
    public List<ReceiptBlockDto> Fr { get; set; } = [];
}

public static class ReceiptBlockTypes
{
    public const string Logo = "logo";
    public const string StoreInfo = "store_info";
    public const string Text = "text";
    public const string Divider = "divider";
    public const string Meta = "meta";
    public const string Items = "items";
    public const string Totals = "totals";
    public const string Payments = "payments";
    public const string VatSummary = "vat_summary";
    public const string ReturnCode = "return_code";
    public const string Spacer = "spacer";

    public static readonly HashSet<string> All =
    [
        Logo, StoreInfo, Text, Divider, Meta, Items, Totals, Payments, VatSummary, ReturnCode, Spacer
    ];
}
```

- [ ] **Step 2: ReceiptTemplateDto ve UpdateReceiptTemplateDto**

```csharp
namespace Entegrasyon.Entity.Dtos.Receipts;

public sealed class ReceiptTemplateDto
{
    public string ThermalJson { get; set; } = "[]";
    public string A4Json { get; set; } = "{}";
    public string? LogoUrl { get; set; }
    public int LogoWidthPx { get; set; } = 120;
    public string StoreName { get; set; } = "";
    public string StoreAddress { get; set; } = "";
    public string StorePhone { get; set; } = "";
}

public sealed record UpdateReceiptTemplateDto(
    string ThermalJson, string A4Json, int LogoWidthPx,
    string StoreName, string StoreAddress, string StorePhone);
```

- [ ] **Step 3: Build**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: 0 Error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Receipts/
git commit -m "feat(receipt): block DTOs + type constants"
```

---

## Task 4: ReceiptTemplateJsonValidator + Unit Tests (TDD)

**Files:** `Application/Entegrasyon.Business/Validation/ReceiptTemplateJsonValidator.cs`, `Test/Entegrasyon.Test/Business/ReceiptTemplateJsonValidatorTests.cs`.

- [ ] **Step 1: Failing test dosyası**

```csharp
using Entegrasyon.Business.Validation;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptTemplateJsonValidatorTests
{
    [Fact]
    public void ValidateThermal_EmptyArray_IsValid()
    {
        var r = ReceiptTemplateJsonValidator.ValidateThermal("[]");
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateThermal_SingleLogoBlock_IsValid()
    {
        var json = """[ { "id":"1", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        ReceiptTemplateJsonValidator.ValidateThermal(json).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateThermal_UnknownBlockType_IsInvalid()
    {
        var json = """[ { "id":"1", "type":"unknown_type", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        var r = ReceiptTemplateJsonValidator.ValidateThermal(json);
        r.IsValid.Should().BeFalse();
        r.Error.Should().Contain("unknown_type");
    }

    [Fact]
    public void ValidateThermal_MissingType_IsInvalid()
    {
        var json = """[ { "id":"1", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        ReceiptTemplateJsonValidator.ValidateThermal(json).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateThermal_MalformedJson_IsInvalid()
    {
        ReceiptTemplateJsonValidator.ValidateThermal("not json").IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateA4_EmptyObject_IsValid()
    {
        ReceiptTemplateJsonValidator.ValidateA4("{}").IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateA4_AllZones_IsValid()
    {
        var json = """
        { "hl": [{ "id":"a", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} }],
          "hr": [], "body": [], "fl": [], "fr": [] }
        """;
        ReceiptTemplateJsonValidator.ValidateA4(json).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateA4_UnknownBlockInZone_IsInvalid()
    {
        var json = """{ "body": [{ "id":"1", "type":"xyz", "showInNormal":true, "showInGift":true, "settings":{} }] }""";
        ReceiptTemplateJsonValidator.ValidateA4(json).IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: FAIL**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReceiptTemplateJsonValidatorTests"`
Expected: compile error.

- [ ] **Step 3: Implement validator**

```csharp
using System.Text.Json;
using Entegrasyon.Entity.Dtos.Receipts;

namespace Entegrasyon.Business.Validation;

public readonly record struct ValidationOutcome(bool IsValid, string? Error);

public static class ReceiptTemplateJsonValidator
{
    public static ValidationOutcome ValidateThermal(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new(false, "Thermal JSON root must be an array.");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var err = ValidateBlock(element);
                if (err is not null) return new(false, err);
            }
            return new(true, null);
        }
        catch (JsonException ex) { return new(false, $"Invalid JSON: {ex.Message}"); }
    }

    public static ValidationOutcome ValidateA4(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new(false, "A4 JSON root must be an object.");

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name is not ("hl" or "hr" or "body" or "fl" or "fr"))
                    return new(false, $"Unknown A4 zone '{property.Name}'.");
                if (property.Value.ValueKind != JsonValueKind.Array)
                    return new(false, $"A4 zone '{property.Name}' must be an array.");

                foreach (var element in property.Value.EnumerateArray())
                {
                    var err = ValidateBlock(element);
                    if (err is not null) return new(false, err);
                }
            }
            return new(true, null);
        }
        catch (JsonException ex) { return new(false, $"Invalid JSON: {ex.Message}"); }
    }

    private static string? ValidateBlock(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return "Block must be an object.";
        if (!element.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            return "Block missing 'type'.";
        var type = typeEl.GetString()!;
        if (!ReceiptBlockTypes.All.Contains(type)) return $"Unknown block type '{type}'.";
        return null;
    }
}
```

- [ ] **Step 4: PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReceiptTemplateJsonValidatorTests"`
Expected: Passed: 8.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Validation/ReceiptTemplateJsonValidator.cs \
        Test/Entegrasyon.Test/Business/ReceiptTemplateJsonValidatorTests.cs
git commit -m "feat(receipt): JSON validator for thermal & A4 schemas"
```

---

## Task 5: IReceiptTemplateManager + Implementation

**Files:** `Application/Entegrasyon.Business/Abstract/IReceiptTemplateManager.cs`, `Application/Entegrasyon.Business/Concrete/ReceiptTemplateManager.cs`, DI kayıt.

- [ ] **Step 1: Önce `IImageManager` signature'ını tespit et**

Run: `grep -n "UploadAsync\|DeleteAsync" Application/Entegrasyon.Business/Abstract/IImageManager.cs`

Çıktıya göre `ReceiptTemplateManager.UploadLogoAsync` içindeki `imageManager.UploadAsync(...)` ve `DeleteAsync(...)` çağrılarını mevcut imzaya uyumla. Aşağıdaki implementasyon **ortak imzayı** varsayıyor; farklıysa minimum uyum gerekir.

- [ ] **Step 2: Interface**

```csharp
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Results;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Business.Abstract;

public interface IReceiptTemplateManager
{
    Task<IDataResult<ReceiptTemplateDto>> GetAsync();
    Task<IResult> UpdateAsync(UpdateReceiptTemplateDto dto);
    Task<IDataResult<string>> UploadLogoAsync(IFormFile file);
    Task<IResult> DeleteLogoAsync();
}
```

- [ ] **Step 3: Manager implementation**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class ReceiptTemplateManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IImageManager imageManager,
    ILogger<ReceiptTemplateManager> logger) : IReceiptTemplateManager
{
    private const int SingletonId = 1;
    private const long MaxLogoBytes = 500_000;
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];

    public async Task<IDataResult<ReceiptTemplateDto>> GetAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null) return new ErrorDataResult<ReceiptTemplateDto>(null!, "Şablon bulunamadı.");

        return new SuccessDataResult<ReceiptTemplateDto>(new ReceiptTemplateDto
        {
            ThermalJson = row.ThermalJson, A4Json = row.A4Json,
            LogoUrl = row.LogoUrl, LogoWidthPx = row.LogoWidthPx,
            StoreName = row.StoreName, StoreAddress = row.StoreAddress, StorePhone = row.StorePhone
        });
    }

    public async Task<IResult> UpdateAsync(UpdateReceiptTemplateDto dto)
    {
        var tv = ReceiptTemplateJsonValidator.ValidateThermal(dto.ThermalJson);
        if (!tv.IsValid) return new ErrorResult($"Thermal JSON hatalı: {tv.Error}");

        var av = ReceiptTemplateJsonValidator.ValidateA4(dto.A4Json);
        if (!av.IsValid) return new ErrorResult($"A4 JSON hatalı: {av.Error}");

        if (dto.LogoWidthPx < 80 || dto.LogoWidthPx > 200)
            return new ErrorResult("Logo genişliği 80-200px aralığında olmalı.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null) return new ErrorResult("Şablon bulunamadı.");

        row.ThermalJson = dto.ThermalJson;
        row.A4Json = dto.A4Json;
        row.LogoWidthPx = dto.LogoWidthPx;
        row.StoreName = dto.StoreName?.Trim() ?? "";
        row.StoreAddress = dto.StoreAddress?.Trim() ?? "";
        row.StorePhone = dto.StorePhone?.Trim() ?? "";

        await db.SaveChangesAsync();
        await applicationLogManager.AddLog("Fiş şablonu güncellendi.", LogType.Setting, LogAction.Update);
        logger.LogInformation("ReceiptTemplate updated.");
        return new SuccessResult("Şablon kaydedildi.");
    }

    public async Task<IDataResult<string>> UploadLogoAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return new ErrorDataResult<string>("", "Dosya boş.");
        if (file.Length > MaxLogoBytes)
            return new ErrorDataResult<string>("", "Dosya 500KB'dan büyük.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            return new ErrorDataResult<string>("", "Sadece PNG, JPEG, WEBP kabul edilir.");

        using var stream = file.OpenReadStream();
        // IImageManager.UploadAsync signature projedeki mevcut imzaya göre adapte edilir.
        var url = await imageManager.UploadAsync(stream, file.FileName, file.ContentType, "receipt-logos");

        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null) return new ErrorDataResult<string>("", "Şablon bulunamadı.");

        var oldUrl = row.LogoUrl;
        row.LogoUrl = url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldUrl))
        {
            try { await imageManager.DeleteAsync(oldUrl); }
            catch (Exception ex) { logger.LogWarning(ex, "Eski logo silinemedi."); }
        }

        return new SuccessDataResult<string>(url, "Logo yüklendi.");
    }

    public async Task<IResult> DeleteLogoAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null || string.IsNullOrEmpty(row.LogoUrl))
            return new SuccessResult("Silinecek logo yok.");

        var url = row.LogoUrl;
        row.LogoUrl = null;
        await db.SaveChangesAsync();

        try { await imageManager.DeleteAsync(url); }
        catch (Exception ex) { logger.LogWarning(ex, "Logo silinirken hata."); }

        return new SuccessResult("Logo silindi.");
    }
}
```

- [ ] **Step 4: DI kaydı**

`Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` içinde `AddApplicationDependencies` metoduna diğer Scoped kayıtların yanına:
```csharp
services.AddScoped<IReceiptTemplateManager, ReceiptTemplateManager>();
```

- [ ] **Step 5: Build**

Run: `dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj`
Expected: 0 Error.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IReceiptTemplateManager.cs \
        Application/Entegrasyon.Business/Concrete/ReceiptTemplateManager.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(receipt): ReceiptTemplateManager with JSON validation + logo upload"
```

---

## Task 6: ReceiptBlockRenderers + Tests

**Files:** `Application/Entegrasyon.Business/Concrete/ReceiptBlockRenderers.cs`, `Test/Entegrasyon.Test/Business/ReceiptBlockRenderersTests.cs`.

- [ ] **Step 1: Failing testler**

```csharp
using System.Text.Json.Nodes;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptBlockRenderersTests
{
    private static SaleDetailDto SampleSale() => new()
    {
        Id = Guid.NewGuid(), SaleNumber = "S20260419-0042",
        ReturnCode = "R-TESTCODE12345",
        SaleDate = new DateTimeOffset(2026, 4, 19, 12, 0, 0, TimeSpan.Zero),
        CustomerName = "Ahmet Yılmaz", SalePersonName = "Demo Kasiyer", BranchOfficeName = "Merkez",
        SubTotal = 250m, VatTotal = 50m, GrandTotal = 300m,
        Items =
        [
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Ürün A", Barcode = "123", Quantity = 1, UnitPriceWithVat = 120m, VatRate = 20m, LineTotalWithVat = 120m },
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Ürün B", Barcode = "456", Quantity = 2, UnitPriceWithVat = 90m, VatRate = 20m, LineTotalWithVat = 180m }
        ],
        Payments = [ new SaleDetailPaymentDto { PaymentMethodName = "Nakit", Amount = 300m, PaidAt = DateTimeOffset.UtcNow } ]
    };

    private static ReceiptTemplateDto SampleTemplate(string logoUrl = "/logo.png") => new()
    {
        LogoUrl = logoUrl, LogoWidthPx = 100,
        StoreName = "Demo Mağaza", StoreAddress = "Cadde 1 No:5", StorePhone = "0555 555 55 55"
    };

    private static ReceiptBlockDto B(string type, string settingsJson = "{}") => new()
    {
        Id = Guid.NewGuid().ToString(), Type = type, ShowInNormal = true, ShowInGift = true,
        Settings = (JsonObject)JsonNode.Parse(settingsJson)!
    };

    [Fact] public void RenderLogo_WithUrl_IncludesImgTag()
    {
        var html = ReceiptBlockRenderers.Render(B("logo"), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("<img").And.Contain("/logo.png").And.Contain("width=\"100\"");
    }

    [Fact] public void RenderLogo_WithoutUrl_RendersEmpty()
    {
        var html = ReceiptBlockRenderers.Render(B("logo"), SampleSale(), SampleTemplate(logoUrl: null!), ReceiptMode.Normal);
        html.Should().BeEmpty();
    }

    [Fact] public void RenderStoreInfo_RendersAllFields()
    {
        var html = ReceiptBlockRenderers.Render(B("store_info", """{"align":"center","size":"m"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("Demo Mağaza").And.Contain("Cadde 1 No:5").And.Contain("0555 555 55 55");
    }

    [Fact] public void RenderText_EncodesHtml()
    {
        var html = ReceiptBlockRenderers.Render(B("text", """{"content":"<script>alert(1)</script>","align":"left","bold":false,"size":"m"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }

    [Fact] public void RenderDivider_DashedStyle()
    {
        var html = ReceiptBlockRenderers.Render(B("divider", """{"style":"dashed","color":"black"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("dashed");
    }

    [Fact] public void RenderMeta_ShowsFlaggedFieldsOnly()
    {
        var html = ReceiptBlockRenderers.Render(B("meta", """{"showSaleNumber":true,"showDate":false,"showCashier":false,"showCustomer":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("S20260419-0042").And.NotContain("Demo Kasiyer");
    }

    [Fact] public void RenderItems_ContainsAllProducts()
    {
        var html = ReceiptBlockRenderers.Render(B("items", """{"showBarcode":false,"showVatColumn":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("Ürün A").And.Contain("Ürün B");
    }

    [Fact] public void RenderTotals_InGiftMode_RendersEmpty()
        => ReceiptBlockRenderers.Render(B("totals"), SampleSale(), SampleTemplate(), ReceiptMode.Gift).Should().BeEmpty();

    [Fact] public void RenderTotals_InNormalMode_ContainsAmounts()
        => ReceiptBlockRenderers.Render(B("totals"), SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().Contain("TOPLAM");

    [Fact] public void RenderPayments_InGiftMode_RendersEmpty()
        => ReceiptBlockRenderers.Render(B("payments"), SampleSale(), SampleTemplate(), ReceiptMode.Gift).Should().BeEmpty();

    [Fact] public void RenderReturnCode_RendersCodeInBox()
    {
        var html = ReceiptBlockRenderers.Render(B("return_code", """{"showLabel":true}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("R-TESTCODE12345").And.Contain("İade Kodu");
    }

    [Fact] public void RenderReturnCode_WithoutLabel_NoLabelShown()
    {
        var html = ReceiptBlockRenderers.Render(B("return_code", """{"showLabel":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("R-TESTCODE12345").And.NotContain("İade Kodu");
    }

    [Fact] public void RenderSpacer_RendersDiv()
        => ReceiptBlockRenderers.Render(B("spacer", """{"size":"l"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().Contain("rcpt-spacer");

    [Fact] public void Render_ShowInNormalFalse_NormalMode_Empty()
    {
        var block = B("text", """{"content":"hi","align":"left","bold":false,"size":"m"}""");
        block.ShowInNormal = false;
        ReceiptBlockRenderers.Render(block, SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().BeEmpty();
    }
}
```

- [ ] **Step 2: FAIL kontrol**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReceiptBlockRenderersTests"`

- [ ] **Step 3: Implement ReceiptBlockRenderers**

```csharp
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Concrete;

public static class ReceiptBlockRenderers
{
    private static readonly CultureInfo TR = new("tr-TR");
    private static readonly HtmlEncoder Enc = HtmlEncoder.Default;

    public static string Render(ReceiptBlockDto block, SaleDetailDto sale, ReceiptTemplateDto template, ReceiptMode mode)
    {
        var visible = mode == ReceiptMode.Gift ? block.ShowInGift : block.ShowInNormal;
        if (!visible) return "";

        return block.Type switch
        {
            ReceiptBlockTypes.Logo       => RenderLogo(template),
            ReceiptBlockTypes.StoreInfo  => RenderStoreInfo(block.Settings, template),
            ReceiptBlockTypes.Text       => RenderText(block.Settings),
            ReceiptBlockTypes.Divider    => RenderDivider(block.Settings),
            ReceiptBlockTypes.Meta       => RenderMeta(block.Settings, sale),
            ReceiptBlockTypes.Items      => RenderItems(block.Settings, sale, mode),
            ReceiptBlockTypes.Totals     => mode == ReceiptMode.Gift ? "" : RenderTotals(sale),
            ReceiptBlockTypes.Payments   => mode == ReceiptMode.Gift ? "" : RenderPayments(sale),
            ReceiptBlockTypes.VatSummary => mode == ReceiptMode.Gift ? "" : RenderVatSummary(sale),
            ReceiptBlockTypes.ReturnCode => RenderReturnCode(block.Settings, sale),
            ReceiptBlockTypes.Spacer     => RenderSpacer(block.Settings),
            _ => ""
        };
    }

    private static string RenderLogo(ReceiptTemplateDto t)
        => string.IsNullOrEmpty(t.LogoUrl) ? ""
        : $"<div class=\"rcpt-logo\"><img src=\"{Enc.Encode(t.LogoUrl)}\" width=\"{t.LogoWidthPx}\" alt=\"Logo\" /></div>";

    private static string RenderStoreInfo(JsonObject s, ReceiptTemplateDto t)
    {
        if (string.IsNullOrWhiteSpace(t.StoreName) && string.IsNullOrWhiteSpace(t.StoreAddress) && string.IsNullOrWhiteSpace(t.StorePhone))
            return "";
        var align = s["align"]?.GetValue<string>() ?? "center";
        var size = s["size"]?.GetValue<string>() ?? "m";
        var sb = new StringBuilder();
        sb.Append($"<div class=\"rcpt-store-info rcpt-align-{align} rcpt-size-{size}\">");
        if (!string.IsNullOrWhiteSpace(t.StoreName))   sb.Append($"<div class=\"rcpt-store-name\"><strong>{Enc.Encode(t.StoreName)}</strong></div>");
        if (!string.IsNullOrWhiteSpace(t.StoreAddress))sb.Append($"<div>{Enc.Encode(t.StoreAddress)}</div>");
        if (!string.IsNullOrWhiteSpace(t.StorePhone))  sb.Append($"<div>{Enc.Encode(t.StorePhone)}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderText(JsonObject s)
    {
        var content = s["content"]?.GetValue<string>() ?? "";
        var align = s["align"]?.GetValue<string>() ?? "left";
        var bold = s["bold"]?.GetValue<bool>() ?? false;
        var size = s["size"]?.GetValue<string>() ?? "m";
        var tag = bold ? "strong" : "span";
        return $"<div class=\"rcpt-text rcpt-align-{align} rcpt-size-{size}\"><{tag}>{Enc.Encode(content)}</{tag}></div>";
    }

    private static string RenderDivider(JsonObject s)
    {
        var style = s["style"]?.GetValue<string>() ?? "dashed";
        var color = s["color"]?.GetValue<string>() ?? "black";
        return $"<div class=\"rcpt-divider rcpt-divider-{style} rcpt-divider-{color}\"></div>";
    }

    private static string RenderMeta(JsonObject s, SaleDetailDto sale)
    {
        var sn = s["showSaleNumber"]?.GetValue<bool>() ?? true;
        var sd = s["showDate"]?.GetValue<bool>() ?? true;
        var sc = s["showCashier"]?.GetValue<bool>() ?? true;
        var su = s["showCustomer"]?.GetValue<bool>() ?? true;
        var sb = new StringBuilder("<div class=\"rcpt-meta\">");
        if (sn) sb.Append($"<div class=\"rcpt-meta-row\"><span>Fiş No:</span><span>{Enc.Encode(sale.SaleNumber)}</span></div>");
        if (sd) sb.Append($"<div class=\"rcpt-meta-row\"><span>Tarih:</span><span>{sale.SaleDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm", TR)}</span></div>");
        if (sc) sb.Append($"<div class=\"rcpt-meta-row\"><span>Kasiyer:</span><span>{Enc.Encode(sale.SalePersonName)}</span></div>");
        if (su && !string.IsNullOrEmpty(sale.CustomerName))
                sb.Append($"<div class=\"rcpt-meta-row\"><span>Müşteri:</span><span>{Enc.Encode(sale.CustomerName)}</span></div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderItems(JsonObject s, SaleDetailDto sale, ReceiptMode mode)
    {
        var showBarcode = s["showBarcode"]?.GetValue<bool>() ?? false;
        var showVat = s["showVatColumn"]?.GetValue<bool>() ?? false;
        var isGift = mode == ReceiptMode.Gift;
        var sb = new StringBuilder("<table class=\"rcpt-items\"><thead><tr><th>Ürün</th><th>Adet</th>");
        if (!isGift) { sb.Append("<th>Birim</th>"); if (showVat) sb.Append("<th>KDV</th>"); sb.Append("<th>Toplam</th>"); }
        sb.Append("</tr></thead><tbody>");
        foreach (var it in sale.Items)
        {
            sb.Append($"<tr><td>{Enc.Encode(it.ProductTitle)}");
            if (showBarcode && !string.IsNullOrEmpty(it.Barcode))
                sb.Append($"<div class=\"rcpt-barcode\">{Enc.Encode(it.Barcode)}</div>");
            sb.Append($"</td><td>{it.Quantity}</td>");
            if (!isGift)
            {
                sb.Append($"<td>{it.UnitPriceWithVat.ToString("C2", TR)}</td>");
                if (showVat) sb.Append($"<td>%{it.VatRate}</td>");
                sb.Append($"<td>{it.LineTotalWithVat.ToString("C2", TR)}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderTotals(SaleDetailDto sale)
        => $"<div class=\"rcpt-totals\">" +
           $"<div class=\"rcpt-total-row\"><span>Ara Toplam:</span><span>{sale.SubTotal.ToString("C2", TR)}</span></div>" +
           $"<div class=\"rcpt-total-row\"><span>KDV:</span><span>{sale.VatTotal.ToString("C2", TR)}</span></div>" +
           $"<div class=\"rcpt-total-row rcpt-grand\"><span>TOPLAM:</span><span>{sale.GrandTotal.ToString("C2", TR)}</span></div></div>";

    private static string RenderPayments(SaleDetailDto sale)
    {
        var sb = new StringBuilder("<div class=\"rcpt-payments\"><div class=\"rcpt-payments-title\">Ödeme:</div>");
        foreach (var p in sale.Payments)
            sb.Append($"<div class=\"rcpt-payment-row\"><span>{Enc.Encode(p.PaymentMethodName)}</span><span>{p.Amount.ToString("C2", TR)}</span></div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderVatSummary(SaleDetailDto sale)
    {
        if (sale.VatSummary is null || sale.VatSummary.Count == 0) return "";
        var sb = new StringBuilder("<table class=\"rcpt-vat-summary\"><thead><tr><th>KDV</th><th>Matrah</th><th>KDV Tutarı</th></tr></thead><tbody>");
        foreach (var v in sale.VatSummary)
            sb.Append($"<tr><td>%{v.VatRate}</td><td>{v.TaxBase.ToString("C2", TR)}</td><td>{v.VatAmount.ToString("C2", TR)}</td></tr>");
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderReturnCode(JsonObject s, SaleDetailDto sale)
    {
        if (string.IsNullOrEmpty(sale.ReturnCode)) return "";
        var showLabel = s["showLabel"]?.GetValue<bool>() ?? true;
        var sb = new StringBuilder("<div class=\"rcpt-return-code-wrap\">");
        if (showLabel) sb.Append("<div class=\"rcpt-return-code-label\">İade Kodu</div>");
        sb.Append($"<div class=\"rcpt-return-code\">{Enc.Encode(sale.ReturnCode)}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderSpacer(JsonObject s)
    {
        var size = s["size"]?.GetValue<string>() ?? "m";
        return $"<div class=\"rcpt-spacer rcpt-spacer-{size}\"></div>";
    }
}
```

- [ ] **Step 4: Test PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReceiptBlockRenderersTests"`
Expected: Passed: 14.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ReceiptBlockRenderers.cs \
        Test/Entegrasyon.Test/Business/ReceiptBlockRenderersTests.cs
git commit -m "feat(receipt): block renderers for all 11 types"
```

---

## Task 7: IReceiptRenderer Service + Tests

**Files:** `Application/Entegrasyon.Business/Abstract/IReceiptRenderer.cs`, `Application/Entegrasyon.Business/Concrete/ReceiptRenderer.cs`, DI kayıt, `Test/Entegrasyon.Test/Business/ReceiptRendererTests.cs`.

- [ ] **Step 1: Interface**

```csharp
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Abstract;

public interface IReceiptRenderer
{
    Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size);
}
```

- [ ] **Step 2: Failing testler**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Moq;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptRendererTests
{
    private readonly Mock<IReceiptTemplateManager> _mgr = new();
    private SaleDetailDto Sale() => new() { Id = Guid.NewGuid(), SaleNumber = "S1", ReturnCode = "R-A", SaleDate = DateTimeOffset.UtcNow, BranchOfficeName = "X", SalePersonName = "Y", SubTotal = 100, VatTotal = 20, GrandTotal = 120, Items = [], Payments = [] };
    private ReceiptTemplateDto T(string th = "[]", string a4 = "{}") => new() { ThermalJson = th, A4Json = a4 };

    [Fact] public async Task Render_ThermalEmpty_ReturnsWrapper()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T()));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("receipt-thermal");
    }

    [Fact] public async Task Render_ThermalWithReturnCode_IncludesCode()
    {
        var json = """[{ "id":"x", "type":"return_code", "showInNormal":true, "showInGift":true, "settings":{"showLabel":true} }]""";
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T(th: json)));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("R-A");
    }

    [Fact] public async Task Render_A4Empty_RendersAllFiveZones()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T()));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.A4);
        html.Should().Contain("receipt-a4").And.Contain("zone-hl").And.Contain("zone-hr").And.Contain("zone-body").And.Contain("zone-fl").And.Contain("zone-fr");
    }

    [Fact] public async Task Render_A4WithLogoInHl_RendersImg()
    {
        var json = """{ "hl": [{ "id":"l", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} }] }""";
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(
            new ReceiptTemplateDto { ThermalJson = "[]", A4Json = json, LogoUrl = "/x.png", LogoWidthPx = 100 }));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.A4);
        html.Should().Contain("/x.png");
    }

    [Fact] public async Task Render_TemplateMissing_ReturnsFallback()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new ErrorDataResult<ReceiptTemplateDto>(null!, "Yok"));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("Şablon yüklenemedi");
    }
}
```

- [ ] **Step 3: FAIL kontrol**

- [ ] **Step 4: Implement ReceiptRenderer**

```csharp
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Concrete;

public sealed class ReceiptRenderer(IReceiptTemplateManager templateManager) : IReceiptRenderer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size)
    {
        var result = await templateManager.GetAsync();
        if (!result.Success || result.Data is null)
            return "<div class=\"receipt-fallback\">Şablon yüklenemedi.</div>";
        var template = result.Data;
        return size == ReceiptSize.Thermal ? RenderThermal(template, sale, mode) : RenderA4(template, sale, mode);
    }

    private string RenderThermal(ReceiptTemplateDto t, SaleDetailDto sale, ReceiptMode mode)
    {
        var blocks = ParseThermal(t.ThermalJson);
        var sb = new StringBuilder("<div class=\"receipt-thermal\">");
        foreach (var b in blocks) sb.Append(ReceiptBlockRenderers.Render(b, sale, t, mode));
        sb.Append("</div>");
        return sb.ToString();
    }

    private string RenderA4(ReceiptTemplateDto t, SaleDetailDto sale, ReceiptMode mode)
    {
        var z = ParseA4(t.A4Json);
        var sb = new StringBuilder("<div class=\"receipt-a4\">");
        AppendZone(sb, "hl", z.Hl, sale, t, mode);
        AppendZone(sb, "hr", z.Hr, sale, t, mode);
        AppendZone(sb, "body", z.Body, sale, t, mode);
        AppendZone(sb, "fl", z.Fl, sale, t, mode);
        AppendZone(sb, "fr", z.Fr, sale, t, mode);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendZone(StringBuilder sb, string name, List<ReceiptBlockDto> blocks, SaleDetailDto sale, ReceiptTemplateDto t, ReceiptMode mode)
    {
        sb.Append($"<div class=\"zone-{name}\">");
        foreach (var b in blocks) sb.Append(ReceiptBlockRenderers.Render(b, sale, t, mode));
        sb.Append("</div>");
    }

    private static List<ReceiptBlockDto> ParseThermal(string json)
    {
        try { return JsonSerializer.Deserialize<List<ReceiptBlockDto>>(json, JsonOpts) ?? []; }
        catch (JsonException) { return []; }
    }

    private static A4ReceiptBlocksDto ParseA4(string json)
    {
        try { return JsonSerializer.Deserialize<A4ReceiptBlocksDto>(json, JsonOpts) ?? new(); }
        catch (JsonException) { return new(); }
    }
}
```

- [ ] **Step 5: DI kaydı**

`ApplicationDependencyExtension.cs`:
```csharp
services.AddScoped<IReceiptRenderer, ReceiptRenderer>();
```

- [ ] **Step 6: Test PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ReceiptRendererTests"`
Expected: Passed: 5.

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IReceiptRenderer.cs \
        Application/Entegrasyon.Business/Concrete/ReceiptRenderer.cs \
        Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
        Test/Entegrasyon.Test/Business/ReceiptRendererTests.cs
git commit -m "feat(receipt): ReceiptRenderer composes blocks into thermal/a4 HTML"
```

---

## Task 8: Receipt CSS Files

**Files:** `Application/Entegrasyon.MVC/wwwroot/css/{receipt-thermal.css, receipt-a4.css}`.

- [ ] **Step 1: receipt-thermal.css**

```css
.receipt-thermal { font-family: 'Courier New', Courier, monospace; font-size: 12px; max-width: 300px; margin: 0 auto; padding: 10px; color: #000; }
.receipt-thermal .rcpt-logo { text-align: center; margin-bottom: 4px; }
.receipt-thermal .rcpt-logo img { max-width: 100%; }
.receipt-thermal .rcpt-store-info { margin-bottom: 6px; }
.receipt-thermal .rcpt-store-name { font-size: 14px; margin-bottom: 2px; }
.receipt-thermal .rcpt-align-left { text-align: left; }
.receipt-thermal .rcpt-align-center { text-align: center; }
.receipt-thermal .rcpt-align-right { text-align: right; }
.receipt-thermal .rcpt-size-s { font-size: 10px; }
.receipt-thermal .rcpt-size-m { font-size: 12px; }
.receipt-thermal .rcpt-size-l { font-size: 14px; }
.receipt-thermal .rcpt-divider { height: 0; margin: 6px 0; }
.receipt-thermal .rcpt-divider-dashed { border-top: 1px dashed #000; }
.receipt-thermal .rcpt-divider-solid { border-top: 1px solid #000; }
.receipt-thermal .rcpt-divider-dotted { border-top: 1px dotted #000; }
.receipt-thermal .rcpt-divider-gray { border-color: #888; }
.receipt-thermal .rcpt-meta-row { display: flex; justify-content: space-between; }
.receipt-thermal .rcpt-items { width: 100%; border-collapse: collapse; }
.receipt-thermal .rcpt-items th, .receipt-thermal .rcpt-items td { padding: 2px 0; text-align: left; }
.receipt-thermal .rcpt-items th:nth-child(n+2), .receipt-thermal .rcpt-items td:nth-child(n+2) { text-align: right; }
.receipt-thermal .rcpt-barcode { font-size: 10px; color: #555; }
.receipt-thermal .rcpt-totals { margin: 4px 0; }
.receipt-thermal .rcpt-total-row { display: flex; justify-content: space-between; }
.receipt-thermal .rcpt-grand { font-weight: bold; font-size: 14px; }
.receipt-thermal .rcpt-payments-title { font-weight: bold; margin-top: 4px; }
.receipt-thermal .rcpt-payment-row { display: flex; justify-content: space-between; }
.receipt-thermal .rcpt-vat-summary { width: 100%; border-collapse: collapse; font-size: 10px; }
.receipt-thermal .rcpt-vat-summary th, .receipt-thermal .rcpt-vat-summary td { padding: 2px 0; text-align: right; }
.receipt-thermal .rcpt-vat-summary th:first-child, .receipt-thermal .rcpt-vat-summary td:first-child { text-align: left; }
.receipt-thermal .rcpt-return-code-wrap { text-align: center; margin: 8px 0; }
.receipt-thermal .rcpt-return-code-label { font-size: 10px; color: #555; }
.receipt-thermal .rcpt-return-code { font-size: 15px; font-weight: bold; letter-spacing: 1.5px; padding: 6px; border: 1px dashed #000; margin-top: 2px; }
.receipt-thermal .rcpt-spacer-s { height: 4px; }
.receipt-thermal .rcpt-spacer-m { height: 8px; }
.receipt-thermal .rcpt-spacer-l { height: 16px; }
.receipt-thermal .rcpt-text { margin: 2px 0; }
@media print { body { margin: 0; } .no-print { display: none; } }
```

- [ ] **Step 2: receipt-a4.css**

```css
@page { size: A4; margin: 20mm; }
.receipt-a4 {
    display: grid;
    grid-template-areas: 'hl hr' 'body body' 'fl fr';
    grid-template-columns: 1fr 1fr;
    grid-gap: 16px;
    font-family: Arial, Helvetica, sans-serif;
    font-size: 11pt; color: #000;
    max-width: 170mm; margin: 0 auto;
}
.receipt-a4 .zone-hl { grid-area: hl; }
.receipt-a4 .zone-hr { grid-area: hr; text-align: right; }
.receipt-a4 .zone-body { grid-area: body; }
.receipt-a4 .zone-fl { grid-area: fl; }
.receipt-a4 .zone-fr { grid-area: fr; text-align: right; }
.receipt-a4 .rcpt-logo img { max-width: 100%; }
.receipt-a4 .rcpt-store-info { line-height: 1.5; }
.receipt-a4 .rcpt-store-name { font-size: 14pt; margin-bottom: 4px; }
.receipt-a4 .rcpt-align-left { text-align: left; }
.receipt-a4 .rcpt-align-center { text-align: center; }
.receipt-a4 .rcpt-align-right { text-align: right; }
.receipt-a4 .rcpt-size-s { font-size: 9pt; }
.receipt-a4 .rcpt-size-m { font-size: 11pt; }
.receipt-a4 .rcpt-size-l { font-size: 14pt; }
.receipt-a4 .rcpt-divider { border-top: 1px solid #000; margin: 8px 0; }
.receipt-a4 .rcpt-divider-dashed { border-top-style: dashed; }
.receipt-a4 .rcpt-divider-dotted { border-top-style: dotted; }
.receipt-a4 .rcpt-divider-gray { border-top-color: #888; }
.receipt-a4 .rcpt-meta { display: grid; grid-template-columns: repeat(2, 1fr); gap: 4px 16px; margin: 8px 0; }
.receipt-a4 .rcpt-meta-row { display: flex; justify-content: space-between; }
.receipt-a4 .rcpt-items { width: 100%; border-collapse: collapse; margin: 8px 0; }
.receipt-a4 .rcpt-items th, .receipt-a4 .rcpt-items td { padding: 6px; border-bottom: 1px solid #eee; text-align: left; }
.receipt-a4 .rcpt-items th:nth-child(n+2), .receipt-a4 .rcpt-items td:nth-child(n+2) { text-align: right; }
.receipt-a4 .rcpt-totals { margin-top: 8px; text-align: right; }
.receipt-a4 .rcpt-total-row { display: flex; justify-content: flex-end; gap: 16px; }
.receipt-a4 .rcpt-total-row span:first-child { min-width: 120px; text-align: right; }
.receipt-a4 .rcpt-grand { font-weight: bold; font-size: 13pt; margin-top: 4px; }
.receipt-a4 .rcpt-payments { margin: 8px 0; }
.receipt-a4 .rcpt-payment-row { display: flex; justify-content: space-between; max-width: 280px; }
.receipt-a4 .rcpt-vat-summary { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: 10pt; }
.receipt-a4 .rcpt-vat-summary th, .receipt-a4 .rcpt-vat-summary td { padding: 4px 8px; border: 1px solid #ccc; text-align: right; }
.receipt-a4 .rcpt-return-code-wrap { margin: 12px 0; }
.receipt-a4 .rcpt-return-code-label { font-size: 9pt; color: #555; }
.receipt-a4 .rcpt-return-code { font-size: 14pt; font-weight: bold; letter-spacing: 2px; padding: 8px; border: 1px dashed #000; max-width: 280px; text-align: center; margin-top: 4px; }
.receipt-a4 .rcpt-spacer-s { height: 4pt; }
.receipt-a4 .rcpt-spacer-m { height: 10pt; }
.receipt-a4 .rcpt-spacer-l { height: 20pt; }
@media print { body { margin: 0; } .no-print { display: none; } }
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/css/receipt-thermal.css \
        Application/Entegrasyon.MVC/wwwroot/css/receipt-a4.css
git commit -m "feat(receipt): thermal + a4 stylesheets (5-zone grid, print media)"
```

---

## Task 9: Update SaleController.Print + Print.cshtml

**Files:** `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`, `Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml`.

- [ ] **Step 1: Constructor'a IReceiptRenderer ekle**

`SaleController.cs` primary constructor'ına `IReceiptRenderer receiptRenderer` parametresi ekle. Ek olarak `Print` action'ını replace et:

```csharp
[HttpGet("/sales/sale/{id:guid}/print")]
public async Task<IActionResult> Print(Guid id, string mode = "normal", string size = "thermal")
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success || result.Data is null) return NotFound();

    var rMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase)
        ? Entegrasyon.Entity.Receipts.ReceiptMode.Gift
        : Entegrasyon.Entity.Receipts.ReceiptMode.Normal;
    var rSize = string.Equals(size, "a4", StringComparison.OrdinalIgnoreCase)
        ? Entegrasyon.Entity.Receipts.ReceiptSize.A4
        : Entegrasyon.Entity.Receipts.ReceiptSize.Thermal;

    ViewBag.RenderedHtml = await receiptRenderer.RenderAsync(result.Data, rMode, rSize);
    ViewBag.ReceiptSize = rSize;
    return View(result.Data);
}
```

- [ ] **Step 2: Print.cshtml — tam rewrite**

```cshtml
@using Entegrasyon.Entity.Dtos.Sale
@using Entegrasyon.Entity.Receipts
@model SaleDetailDto
@{
    Layout = null;
    var size = (ReceiptSize)(ViewBag.ReceiptSize ?? ReceiptSize.Thermal);
    var cssFile = size == ReceiptSize.A4 ? "receipt-a4.css" : "receipt-thermal.css";
}
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <title>Fiş — @Model.SaleNumber</title>
    <link rel="stylesheet" href="~/css/@cssFile" />
</head>
<body>
    @Html.Raw(ViewBag.RenderedHtml as string ?? "")
    <div class="no-print" style="margin-top: 20px; text-align: center;">
        <button onclick="window.print()">Yazdır</button>
        <button onclick="window.close()">Kapat</button>
    </div>
    <script>window.addEventListener('load', () => setTimeout(() => window.print(), 500));</script>
</body>
</html>
```

- [ ] **Step 3: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: 0 Error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/SaleController.cs \
        Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml
git commit -m "feat(sale): Print uses ReceiptRenderer + supports ?size=a4"
```

---

## Task 10: ReceiptTemplateController + Editor Skeleton

**Files:** `Application/Entegrasyon.MVC/Features/Settings/ReceiptTemplateController.cs`, `ViewModels/ReceiptTemplateEditorVm.cs`, `Views/ReceiptTemplate/Index.cshtml`, `Views/ReceiptTemplate/Partials/_Palette.cshtml`.

- [ ] **Step 1: ViewModel**

```csharp
using Entegrasyon.Entity.Dtos.Receipts;

namespace Entegrasyon.MVC.Features.Settings.ViewModels;

public class ReceiptTemplateEditorVm
{
    public ReceiptTemplateDto Template { get; set; } = new();
}
```

- [ ] **Step 2: Controller**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;
using Entegrasyon.MVC.Features.Settings.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Settings;

[Authorize(Roles = "Admin")]
public class ReceiptTemplateController(
    IReceiptTemplateManager templateManager,
    IReceiptRenderer receiptRenderer) : Controller
{
    [HttpGet("/settings/receipt-template")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Fiş Şablonu");
        ViewData.SetActiveNav("settings");

        var result = await templateManager.GetAsync();
        var vm = new ReceiptTemplateEditorVm { Template = result.Data ?? new ReceiptTemplateDto() };
        return View("~/Features/Settings/Views/ReceiptTemplate/Index.cshtml", vm);
    }

    [HttpPost("/settings/receipt-template")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateReceiptTemplateDto dto)
    {
        var result = await templateManager.UpdateAsync(dto);
        if (!result.Success)
        {
            Response.HtmxTriggerWithData("showToast", new { message = result.Message, level = "error" });
            return BadRequest(result.Message);
        }
        Response.HtmxTriggerWithData("showToast", new { message = "Şablon kaydedildi.", level = "success" });
        return Ok();
    }

    [HttpPost("/settings/receipt-template/upload-logo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var result = await templateManager.UploadLogoAsync(file);
        return result.Success ? Ok(new { url = result.Data }) : BadRequest(result.Message);
    }

    [HttpDelete("/settings/receipt-template/logo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLogo()
    {
        var result = await templateManager.DeleteLogoAsync();
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("/settings/receipt-template/preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm] string size, [FromForm] string mode)
    {
        var rMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase) ? ReceiptMode.Gift : ReceiptMode.Normal;
        var rSize = string.Equals(size, "a4", StringComparison.OrdinalIgnoreCase) ? ReceiptSize.A4 : ReceiptSize.Thermal;
        var html = await receiptRenderer.RenderAsync(BuildPlaceholderSale(), rMode, rSize);
        return Content(html, "text/html");
    }

    private static SaleDetailDto BuildPlaceholderSale() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        SaleNumber = "S20260419-0042", ReturnCode = "R-7K4QN9XM2P9AB",
        SaleDate = new DateTimeOffset(2026, 4, 19, 14, 30, 0, TimeSpan.FromHours(3)),
        CustomerName = "Ahmet Yılmaz", SalePersonName = "Demo Kasiyer", BranchOfficeName = "Merkez Şube",
        SubTotal = 250m, VatTotal = 50m, GrandTotal = 300m,
        Items =
        [
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Örnek Ürün A", Barcode = "1234567890123", Quantity = 1, UnitPriceWithVat = 120m, VatRate = 20m, LineTotalWithVat = 120m },
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Örnek Ürün B", Barcode = "9876543210987", Quantity = 2, UnitPriceWithVat = 90m, VatRate = 20m, LineTotalWithVat = 180m }
        ],
        Payments = [ new SaleDetailPaymentDto { PaymentMethodName = "Nakit", Amount = 300m, PaidAt = DateTimeOffset.UtcNow } ],
        VatSummary = [ new VatSummaryLineDto(20m, 250m, 50m, 300m) ]
    };
}
```

- [ ] **Step 3: Index.cshtml (skeleton)**

```cshtml
@using Entegrasyon.Entity.Dtos.Receipts
@model Entegrasyon.MVC.Features.Settings.ViewModels.ReceiptTemplateEditorVm

<div class="page-header d-flex justify-content-between align-items-center mb-3">
    <h2 class="page-title">Fiş Şablonu</h2>
    <button type="button" class="btn btn-primary" id="btn-save-template">
        <i class="ti ti-device-floppy me-1"></i>Sakla
    </button>
</div>

<div class="card mb-3">
    <div class="card-body d-flex align-items-center gap-3">
        <div class="btn-group" role="group">
            <input type="radio" class="btn-check" name="rcpt-size" id="size-thermal" value="thermal" checked>
            <label class="btn btn-outline-primary" for="size-thermal">Thermal</label>
            <input type="radio" class="btn-check" name="rcpt-size" id="size-a4" value="a4">
            <label class="btn btn-outline-primary" for="size-a4">A4</label>
        </div>
        <div class="btn-group" role="group">
            <input type="radio" class="btn-check" name="rcpt-mode" id="mode-normal" value="normal" checked>
            <label class="btn btn-outline-secondary" for="mode-normal">Normal</label>
            <input type="radio" class="btn-check" name="rcpt-mode" id="mode-gift" value="gift">
            <label class="btn btn-outline-secondary" for="mode-gift">Hediye</label>
        </div>
    </div>
</div>

<form id="anti-forgery-form">@Html.AntiForgeryToken()</form>

<div class="row g-3">
    <div class="col-lg-3">
        <partial name="~/Features/Settings/Views/ReceiptTemplate/Partials/_Palette.cshtml" model="Model" />
    </div>
    <div class="col-lg-5">
        <div class="card">
            <div class="card-header"><h3 class="card-title">Şablon</h3></div>
            <div class="card-body" id="editor-zones"></div>
        </div>
    </div>
    <div class="col-lg-4">
        <div class="card">
            <div class="card-header"><h3 class="card-title">Canlı Önizleme</h3></div>
            <div class="card-body" id="preview-container">
                <div class="text-center text-secondary">Yükleniyor...</div>
            </div>
        </div>
    </div>
</div>

<div id="block-settings-offcanvas-container"></div>

@section Scripts {
    <script src="~/lib/sortablejs/Sortable.min.js"></script>
    <script>
        window.__rcptState = {
            template: @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.Template)),
            activeSize: 'thermal',
            activeMode: 'normal'
        };
    </script>
    <script src="~/js/receipt-editor.js"></script>
}
```

- [ ] **Step 4: _Palette.cshtml partial**

```cshtml
@model Entegrasyon.MVC.Features.Settings.ViewModels.ReceiptTemplateEditorVm
@{ var t = Model.Template; }

<div class="card mb-3">
    <div class="card-header"><h3 class="card-title">Blok Ekle</h3></div>
    <div class="card-body d-grid gap-2">
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="logo">+ Logo</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="store_info">+ Mağaza Bilgisi</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="text">+ Metin</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="divider">+ Ayırıcı</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="meta">+ Meta Satırı</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="items">+ Ürün Listesi</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="totals">+ Toplamlar</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="payments">+ Ödeme Özeti</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="vat_summary">+ KDV Özeti</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="return_code">+ İade Kodu</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-add-block="spacer">+ Boşluk</button>
    </div>
</div>

<div class="card mb-3">
    <div class="card-header"><h3 class="card-title">Logo</h3></div>
    <div class="card-body">
        <div id="logo-preview" class="text-center mb-2">
            @if (!string.IsNullOrEmpty(t.LogoUrl))
            {
                <img src="@t.LogoUrl" style="max-width:100%; max-height:100px" />
            }
            else
            {
                <div class="text-secondary small">(Logo yüklü değil)</div>
            }
        </div>
        <input type="file" id="logo-file-input" accept="image/png,image/jpeg,image/webp" class="form-control form-control-sm mb-2" />
        <div class="mb-2">
            <label class="form-label small">Logo Genişliği</label>
            <input type="range" id="logo-width-range" min="80" max="200" step="5" value="@t.LogoWidthPx" class="form-range" />
            <div class="small text-secondary"><span id="logo-width-value">@t.LogoWidthPx</span> px</div>
        </div>
        <button type="button" id="btn-delete-logo" class="btn btn-sm btn-outline-danger w-100" @(string.IsNullOrEmpty(t.LogoUrl) ? "disabled" : "")>Logoyu Sil</button>
    </div>
</div>

<div class="card">
    <div class="card-header"><h3 class="card-title">Mağaza Bilgileri</h3></div>
    <div class="card-body">
        <div class="mb-2">
            <label class="form-label small">Mağaza Adı</label>
            <input type="text" id="store-name-input" class="form-control form-control-sm" maxlength="100" value="@t.StoreName" />
        </div>
        <div class="mb-2">
            <label class="form-label small">Adres</label>
            <textarea id="store-address-input" class="form-control form-control-sm" maxlength="200" rows="2">@t.StoreAddress</textarea>
        </div>
        <div class="mb-2">
            <label class="form-label small">Telefon</label>
            <input type="text" id="store-phone-input" class="form-control form-control-sm" maxlength="20" value="@t.StorePhone" />
        </div>
    </div>
</div>
```

- [ ] **Step 5: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: 0 Error.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Settings/
git commit -m "feat(settings): ReceiptTemplateController + editor skeleton"
```

---

## Task 11: Receipt Editor JS

**Files:** `Application/Entegrasyon.MVC/wwwroot/js/receipt-editor.js`, minimal CSS additions.

**Özet:** Vanilla JS modülü. State = `window.__rcptState`. Parça parça implementation:

- State model: `{ template: { thermalJson, a4Json, logoUrl, logoWidthPx, storeName, storeAddress, storePhone }, activeSize: 'thermal'|'a4', activeMode: 'normal'|'gift' }`.
- Helper'lar: `uuid()`, `defaultBlock(type)`, `thermalBlocks()`, `a4Zones()`, `escapeHtml()`.
- `renderEditor()`: thermal'de tek `<div data-zone="thermal-body">`, A4'te 5 zone div'i. Her zone `new Sortable(el, {group:'receipt-blocks', handle:'.rcpt-drag-handle', animation:150, onEnd: captureZonesFromDom })` ile bağlanır.
- Blok satırı: `data-block-id`, `data-toggle-mode="normal|gift"`, `data-edit-block`, `data-remove-block` attribute'lu butonlar.
- `openBlockSettings(id)`: offcanvas HTML render eder; tip bazlı form (text → content/align/bold/size; divider → style/color; meta → 4 checkbox; items → 2 checkbox; return_code → showLabel; spacer/store_info → size/align; vs).
- Form değişimi: `data-setting="key"` attribute'una sahip input'lar. `change` (radio/checkbox) + `input` (text/textarea) event'lerinde `updateBlock(id, b => ({ settings: { ...b.settings, [key]: value } }))` çağrılır.
- `refreshPreview()`: 250ms debounce'lu `fetch('/settings/receipt-template/preview', { method: 'POST', body: FormData('size', 'mode', '__RequestVerificationToken') })` → `.text()` → preview container'a yaz (`<link rel="stylesheet" href="/css/receipt-(thermal|a4).css"><div class="receipt-preview-wrapper">{html}</div>`).
  - **Güvenlik notu:** Server tarafı `IReceiptRenderer` HtmlEncoder kullanarak çıktı üretiyor; user-supplied content her blok renderer içinde encode edilmiş durumda. Preview container'a server'dan gelen HTML doğrudan enjekte edilir — third-party sanitization gereksiz (server tarafı güvenli).
- `saveTemplate()`: `fetch('/settings/receipt-template', POST, FormData: ThermalJson, A4Json, LogoWidthPx, StoreName, StoreAddress, StorePhone, __RequestVerificationToken)`.
- `uploadLogo(file)`: `fetch('/settings/receipt-template/upload-logo', POST, FormData: file)` → response.url alıp state'e yaz, logo preview güncelle.
- `deleteLogo()`: `fetch('/settings/receipt-template/logo', DELETE)`.
- `addBlock(type)`: default block oluştur → thermal'da body'nin sonuna, A4'te `body` zone'una push. `saveThermalBlocks()` / `saveA4Zones()` state'i güncelle. `renderEditor()` + `refreshPreview()` çağır.
- `removeBlock(id)`, `updateBlock(id, mutator)`, `toggleMode(id, which)` state mutasyon helper'ları.
- Init: `DOMContentLoaded` → palette buton listener, save buton, logo input, logo width slider (live), size/mode radio'ları → `renderEditor()` + `refreshPreview()`.

- [ ] **Step 1: receipt-editor.js dosyasını yaz**

Yukarıdaki özete göre full implementasyonu yaz. ~250 satır vanilla JS. `escapeHtml` helper her kullanıcı içeriğinin HTML'e enjeksiyonundan önce çağrılmalı. DOM inşası için template literals + setter pattern kullan (server-rendered preview hariç — o güvenli).

Referans: Mevcut projedeki `Application/Entegrasyon.MVC/wwwroot/js/pos.js` veya benzeri dosyanın stilini incele.

- [ ] **Step 2: Minimal CSS**

`Application/Entegrasyon.MVC/wwwroot/css/site.css` (veya proje ortak CSS'i) içine:

```css
.rcpt-zone { min-height: 60px; padding: 8px; background: #f8f9fa; border-radius: 4px; }
.rcpt-zone-label { font-size: 11px; font-weight: bold; text-transform: uppercase; color: #6c757d; margin-bottom: 4px; }
.rcpt-zone-empty { opacity: 0.5; border: 1px dashed #ccc; border-radius: 4px; padding: 8px; text-align: center; }
.rcpt-block-row { background: white; }
.rcpt-drag-handle { cursor: grab; }
.rcpt-drag-handle:active { cursor: grabbing; }
```

- [ ] **Step 3: Sortable.min.js'nin projede olduğunu doğrula**

Run: `find Application/Entegrasyon.MVC/wwwroot/lib -name "Sortable.min.js"`

Yoksa indir:
```bash
mkdir -p Application/Entegrasyon.MVC/wwwroot/lib/sortablejs
curl -o Application/Entegrasyon.MVC/wwwroot/lib/sortablejs/Sortable.min.js https://cdn.jsdelivr.net/npm/sortablejs@1.15.0/Sortable.min.js
```

- [ ] **Step 4: Manuel UI testi**

Dev server (`dotnet run`). `/settings/receipt-template` aç:
- Blok ekle → orta zone'a düşer, preview update.
- Sürükle-bırak sıralama → preview güncellenir.
- Ayarlar düzenle → offcanvas açılır, değişiklik canlı yansır.
- Size toggle (Thermal/A4) → editor zones'ı değişir, preview CSS'i değişir.
- Mode toggle (Normal/Hediye) → preview'da fiyatlar görünür/gizlenir.
- Logo upload → MinIO'ya yüklenir, preview'da görünür.
- "Sakla" → toast "Şablon kaydedildi.".

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/js/receipt-editor.js \
        Application/Entegrasyon.MVC/wwwroot/lib/sortablejs/ \
        Application/Entegrasyon.MVC/wwwroot/css/site.css
git commit -m "feat(settings): receipt editor JS — palette, drag-drop, preview"
```

---

## Task 12: SaleDetail Print Dropdown + POS Modal A4 Toggle

**Files:** `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`, `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml`, `Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml`.

- [ ] **Step 1: SaleDetail.cshtml — iki link → dropdown**

Mevcut iki `<a>` print butonunu bununla replace et:

```cshtml
<div class="dropdown d-inline-block">
    <button type="button" class="btn btn-outline-primary dropdown-toggle" data-bs-toggle="dropdown">
        <i class="ti ti-printer me-1"></i>Fişi Yazdır
    </button>
    <div class="dropdown-menu">
        <a class="dropdown-item" href="/sales/sale/@Model.Id/print?mode=normal&size=thermal" target="_blank">
            <i class="ti ti-receipt me-2"></i>Normal (Thermal)
        </a>
        <a class="dropdown-item" href="/sales/sale/@Model.Id/print?mode=normal&size=a4" target="_blank">
            <i class="ti ti-file me-2"></i>Normal (A4)
        </a>
        <div class="dropdown-divider"></div>
        <a class="dropdown-item" href="/sales/sale/@Model.Id/print?mode=gift&size=thermal" target="_blank">
            <i class="ti ti-gift me-2"></i>Hediye (Thermal)
        </a>
        <a class="dropdown-item" href="/sales/sale/@Model.Id/print?mode=gift&size=a4" target="_blank">
            <i class="ti ti-file me-2"></i>Hediye (A4)
        </a>
    </div>
</div>
```

- [ ] **Step 2: _POSLastSaleModal.cshtml — modal footer'a size toggle**

Mevcut `<div class="modal-footer">` bloğunu bununla replace et:

```cshtml
<div class="modal-footer d-flex flex-column align-items-stretch">
    <div class="btn-group btn-group-sm w-100 mb-2" role="group">
        <input type="radio" class="btn-check" name="rcpt-size-choice" id="rs-thermal" value="thermal" checked>
        <label class="btn btn-outline-primary" for="rs-thermal">Thermal</label>
        <input type="radio" class="btn-check" name="rcpt-size-choice" id="rs-a4" value="a4">
        <label class="btn btn-outline-primary" for="rs-a4">A4</label>
    </div>
    <div class="d-flex gap-2">
        <button type="button" class="btn btn-link link-secondary flex-grow-1" data-bs-dismiss="modal">Atla</button>
        <button type="button" class="btn btn-outline-primary" id="posPrintGiftBtn">
            <i class="ti ti-gift me-1"></i>Hediye
        </button>
        <button type="button" class="btn btn-primary" id="posPrintNormalBtn">
            <i class="ti ti-printer me-1"></i>Normal
        </button>
    </div>
</div>
```

- [ ] **Step 3: Index.cshtml — printUrl fonksiyonunu size parametresi alacak şekilde güncelle**

Mevcut:
```javascript
const printUrl = (mode) => `/sales/sale/${payload.saleId}/print?mode=${mode}`;
```

Yenisi:
```javascript
const printUrl = (mode) => {
    const size = document.querySelector('input[name="rcpt-size-choice"]:checked')?.value || 'thermal';
    return `/sales/sale/${payload.saleId}/print?mode=${mode}&size=${size}`;
};
```

- [ ] **Step 4: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: 0 Error.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml \
        Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSLastSaleModal.cshtml \
        Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml
git commit -m "feat(ui): print dropdown + POS modal A4 size toggle"
```

---

## Task 13: Integration Tests (CI için)

**Files:** `Test/Entegrasyon.IntegrationTest/Receipts/ReceiptTemplateEndpointTests.cs`.

**Not:** Fedora/podman ortamında çalışmaz; CI için yazılır.

- [ ] **Step 1: Test dosyası**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Receipts;

[Trait("Category", "Integration")]
public class ReceiptTemplateEndpointTests : IntegrationTestBase
{
    public ReceiptTemplateEndpointTests(PostgreSqlFixture pg, WireMockFixture wm) : base(pg, wm) { }

    [Fact]
    public async Task GetAsync_ReturnsSeedDefault()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();
        var result = await mgr.GetAsync();
        result.Success.Should().BeTrue();
        result.Data!.ThermalJson.Should().Contain("return_code");
        result.Data.A4Json.Should().Contain("hl");
    }

    [Fact]
    public async Task UpdateAsync_InvalidThermalJson_ReturnsError()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();
        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("not json", "{}", 120, "", "", ""));
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Thermal JSON");
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesAndPersists()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();
        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("[]", "{}", 100, "Test Mağaza", "Cadde 1", "0555"));
        result.Success.Should().BeTrue();
        var reloaded = await mgr.GetAsync();
        reloaded.Data!.StoreName.Should().Be("Test Mağaza");
        reloaded.Data.LogoWidthPx.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_LogoWidthOutOfRange_ReturnsError()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();
        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("[]", "{}", 50, "", "", ""));
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("80-200");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Test/Entegrasyon.IntegrationTest/Receipts/
git commit -m "test(receipt): integration tests for template endpoints"
```

---

## Task 14: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 Error.

- [ ] **Step 2: Migration snapshot drift**

Run:
```bash
dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 3: Receipt unit testleri**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Receipt"`
Expected: ≥27 test PASS (8 validator + 14 renderers + 5 renderer).

- [ ] **Step 4: Manuel uçtan uca akış**

Dev server (`dotnet run`). `admin/123456789` giriş:

1. `/settings/receipt-template` — blok ekle/sil, sürükle-bırak, ayar düzenle, logo yükle, "Sakla".
2. `/pos` — satış yap → modal → A4 toggle → "Fişi Yazdır" (yeni sekmede A4 grid).
3. `/sales/sale/{id}` — dropdown → 4 kombinasyon test (Normal/Hediye × Thermal/A4).

- [ ] **Step 5: Final commit (varsa)**

```bash
git status
# varsa:
git add -A
git commit -m "chore: manual verification tweaks"
```

---

## Self-Review

**Spec coverage:**
- ✅ §Veri Modeli → Task 1 + Task 2.
- ✅ §Blok JSON + validation → Task 3 + Task 4.
- ✅ §Business → Task 5 (manager), Task 6 (renderers), Task 7 (service).
- ✅ §CSS → Task 8.
- ✅ §Print integration → Task 9.
- ✅ §Editör UI → Task 10 (skeleton) + Task 11 (JS).
- ✅ §SaleDetail/POS UI → Task 12.
- ✅ §Default seed → Task 2 JSON.
- ✅ §Testler → Task 4, 6, 7 (unit), Task 13 (integration).

**Placeholder scan:** Task 11 JS implementasyonu özet/pseudocode formunda (security linter innerHTML pattern'ini tetiklediği için full kod inline yazılmadı). Agent tarafından yazılacak full JS'de güvenlik pratikleri (escapeHtml wrapper, server-rendered preview dışında innerHTML kullanımından kaçınma) Step 1 açıklamasında belirtilmiş.

**Type consistency:**
- `ReceiptMode` / `ReceiptSize` enum'ları `Entegrasyon.Entity.Receipts` namespace'i altında tek tanım — controller, renderer, test'ler aynı namespace'ten import eder.
- `ReceiptBlockDto.Settings` tipi `JsonObject` — renderers + validator + editor JS state hep aynı tip.
- `IReceiptTemplateManager` signature'ları read/write/upload/delete — controller ve tests aynı method signature'larını kullanır.
- Blok tip string'leri `ReceiptBlockTypes.*` constants üzerinden; validator, renderers ve editor JS aynı sabit setini kullanır.

**IImageManager uyumu:** Task 5 Step 1'de `IImageManager.UploadAsync/DeleteAsync` signature'ının projedeki mevcut imzaya uyarlanması belirtildi. Gerçek signature farklıysa agent Step 3 kodundaki çağrıları adapte eder.

**Tahmini effort:** 5-8 gün (mid-senior dev). Backend ağırlıklı: Task 1-9 = ~2-3 gün. UI: Task 10-12 = ~3-4 gün (özellikle Task 11 JS). Test + verification = ~1 gün.
