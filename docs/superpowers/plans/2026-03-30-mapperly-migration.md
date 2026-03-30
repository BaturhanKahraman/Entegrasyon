# Mapperly Migration (Mapster → Mapperly) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace Mapster reflection-based mapping with Mapperly source-generator mapping for compile-time safety and zero-reflection performance.

**Architecture:** One `[Mapper]` partial class per domain area under `Application/Entegrasyon.Business/Mappers/`. Mappers registered as singletons in DI. Managers inject their specific typed mapper instead of `IMapper`. The `IMapper` abstraction is removed entirely. Tests that mock `IMapper` are updated to use concrete mapper instances or extracted interfaces.

**Tech Stack:** .NET 8, Riok.Mapperly 3.x (source generator), xUnit, Moq, FluentAssertions

---

## Resolved Open Questions

**Q1: Is `Pageable<T>` open-generic profile actually used at call sites?**
No. All `Pageable<T>` instances in the codebase are constructed directly with `new Pageable<T>(items, pageIndex, pageSize, total)` or via `ToPageableAsync()` extension. No manager calls `mapper.Map<Pageable<T>>`. The profile in `MappingConfig.cs` is dead code — delete it without replacement.

**Q2: `ProductsDetailDto` from `Product` — what does `mapper.Map<ProductsDetailDto>(product)` actually populate?**
`ProductsDetailDto` has a positional constructor requiring `BrandName`, `CategoryName`, `TotalQuantity`, `TotalSoldQuantity`, `VariantCount` — all computed from navigation properties. Mapster silently maps to defaults (empty string, 0) because `Product` has no flat `BrandName` property. This call at line 101 of `ProductManager.cs` (`GetProductByBarcode`) is **broken today** — it returns a DTO with blank brand/category and zero counts. Fix: replace `mapper.Map<ProductsDetailDto>(product)` with a DB projection that includes includes `Brand.Name`, `Category.Name`, and variant/stock aggregations, matching the pattern already used in `GetProductsDetailsPageable`.

**Q3: Is `UserEditDto → ApplicationUser` profile actually used?**
No. `EditUser` in `ApplicationUserManager` manually assigns properties to `dbUser` fetched from the DB — it does not call `mapper.Map<ApplicationUser>(dto)`. The `config.NewConfig<UserEditDto, ApplicationUser>()` profile in `MappingConfig.cs` is dead code. Delete it without replacement.

**Q4: Tests that mock `IMapper` — which ones and which setups?**
Five test files hold `Mock<IMapper>`:
- `CategoryManagerTests.cs` — one `_mockMapper.Setup(m => m.Map<Category>(dto)).Returns(...)` call
- `ProductManagerTests.cs` — `Mock<IMapper>` created but no `.Setup()` found (mapper injected but not exercised in tested paths)
- `UpdateProductTests.cs` — same as ProductManagerTests
- `BrandServiceTests.cs` — `Mock<IMapper>` created but no `.Setup()` call found
- `BrandMatchServiceTests.cs` — `Mock<IMapper>` created but no `.Setup()` call found

Migration strategy per test file is spelled out in each task below.

---

## Phase 1: Infrastructure Setup

### Task 1: Add Mapperly NuGet + create empty mapper skeletons

**Goal:** Mapperly is installed and building. No functional change. Mapster still works.

**Files to modify:**
- `Application/Entegrasyon.Business/Entegrasyon.Business.csproj`

**Files to create:**
- `Application/Entegrasyon.Business/Mappers/` (10 empty mapper files)

**Step 1 — Add NuGet:**

In `Entegrasyon.Business.csproj`, add inside `<ItemGroup>`:
```xml
<PackageReference Include="Riok.Mapperly" Version="3.*" />
```
Keep Mapster packages for now — they will be removed in Phase 3.

**Step 2 — Create skeleton mapper files** (empty partial classes, no methods yet):

`Application/Entegrasyon.Business/Mappers/SettingMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public static partial class SettingMapper { }
```

`Application/Entegrasyon.Business/Mappers/CargoCompanyMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CargoCompanyMapper { }
```

`Application/Entegrasyon.Business/Mappers/BranchOfficeMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BranchOfficeMapper { }
```

`Application/Entegrasyon.Business/Mappers/BrandMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BrandMapper { }
```

`Application/Entegrasyon.Business/Mappers/SaleMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class SaleMapper { }
```

`Application/Entegrasyon.Business/Mappers/ProductMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class ProductMapper { }
```

`Application/Entegrasyon.Business/Mappers/UserMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class UserMapper { }
```

`Application/Entegrasyon.Business/Mappers/TrendyolImportMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class TrendyolImportMapper { }
```

`Application/Entegrasyon.Business/Mappers/BrandMatchMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BrandMatchMapper { }
```

`Application/Entegrasyon.Business/Mappers/CategoryMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryMapper { }
```

`Application/Entegrasyon.Business/Mappers/CategoryAttributeMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryAttributeMapper { }
```

`Application/Entegrasyon.Business/Mappers/CustomerMapper.cs`:
```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CustomerMapper { }
```

**Step 3 — Verify build:**
```bash
dotnet build Entegrasyon.sln
```
Expected: zero errors, zero Mapperly warnings (empty mappers produce no output).

**Step 4 — Run tests (must stay green):**
```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

**Commit:** `feat(mappers): add Mapperly NuGet and empty mapper skeletons`

---

## Phase 2: Migrate Domain Areas One by One

### Task 2: Migrate ApplicationSettings (SettingMapper — static, no DI)

**Complexity:** Trivial flat copy. Uses `.Adapt<T>()` directly in `ApplicationSettingManager` — no `IMapper` injection. Becomes a static mapper.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/SettingMapper.cs`
- `Application/Entegrasyon.Business/Concrete/ApplicationSettingManager.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs` (no `ApplicationSetting` profiles to remove — ApplicationSettings were never in MappingConfig, pure `.Adapt<T>()` defaults)

**TDD — Write test first:**

Create `Test/Entegrasyon.Test/Mappers/MapperConversionTests.cs`:
```csharp
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.ApplicationSettings;
using Entegrasyon.Entity.Dtos.ApplicationSettings;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Mappers;

public class MapperConversionTests
{
    [Fact]
    public void SettingMapper_MapToDto_MapsAllFields()
    {
        var setting = new ApplicationSetting { Key = "TestKey", Value = "TestValue" };
        var dto = SettingMapper.MapToDto(setting);
        dto.Key.Should().Be("TestKey");
        dto.Value.Should().Be("TestValue");
    }

    [Fact]
    public void SettingMapper_MapToDtoList_ReturnsCorrectCount()
    {
        var settings = new List<ApplicationSetting>
        {
            new() { Key = "K1", Value = "V1" },
            new() { Key = "K2", Value = "V2" }
        };
        var dtos = SettingMapper.MapToDtoList(settings);
        dtos.Should().HaveCount(2);
        dtos[0].Key.Should().Be("K1");
    }
}
```

Run test — expect RED (SettingMapper has no methods yet).

**Implement SettingMapper:**
```csharp
using Entegrasyon.Entity.ApplicationSettings;
using Entegrasyon.Entity.Dtos.ApplicationSettings;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public static partial class SettingMapper
{
    public static partial ApplicationSettingDto MapToDto(ApplicationSetting setting);
    public static partial List<ApplicationSettingDto> MapToDtoList(List<ApplicationSetting> settings);
}
```

**Update `ApplicationSettingManager.cs`:**

Replace:
```csharp
using Mapster;
// ...
return settings.Adapt<List<ApplicationSettingDto>>();
// ...
return setting?.Adapt<ApplicationSettingDto>();
```

With:
```csharp
using Entegrasyon.Business.Mappers;
// ...
return SettingMapper.MapToDtoList(settings);
// ...
return setting is null ? null : SettingMapper.MapToDto(setting);
```

Note: There are 3 `.Adapt<>()` calls in `ApplicationSettingManager.cs` — two `Adapt<List<ApplicationSettingDto>>()` calls and one `Adapt<ApplicationSettingDto>()`. Replace all three.

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate ApplicationSettings to Mapperly SettingMapper`

---

### Task 3: Migrate CargoCompany

**Complexity:** Simple bidirectional.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/CargoCompanyMapper.cs`
- `Application/Entegrasyon.Business/Concrete/CargoCompaniesManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs` (remove Cargo Company profiles)

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void CargoCompanyMapper_MapToEntity_MapsFields()
{
    var mapper = new CargoCompanyMapper();
    var dto = new AddCargoCompanyDto { Name = "DHL" };
    var entity = mapper.MapToEntity(dto);
    entity.Name.Should().Be("DHL");
}
```

Run — RED.

**Implement `CargoCompanyMapper.cs`:**
```csharp
using Entegrasyon.Entity.Core;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CargoCompanyMapper
{
    public partial CargoCompany MapToEntity(AddCargoCompanyDto dto);
    public partial AddCargoCompanyDto MapToDto(CargoCompany entity);
}
```

Note: `CargoCompany` has a `NpgsqlTsVector SearchVector` property. Mapperly will emit a warning for unmapped `SearchVector` (no matching property on `AddCargoCompanyDto`). Suppress with:
```csharp
[MapperIgnoreTarget(nameof(CargoCompany.SearchVector))]
public partial CargoCompany MapToEntity(AddCargoCompanyDto dto);
```

**Register in DI** (`ApplicationDependencyExtension.cs`):
```csharp
services.AddSingleton<CargoCompanyMapper>();
```

**Update `CargoCompaniesManager.cs`:**

Constructor before:
```csharp
public CargoCompaniesManager(..., IMapper mapper, ...)
```

Constructor after:
```csharp
public CargoCompaniesManager(..., CargoCompanyMapper mapper, ...)
```

Call site before:
```csharp
var cargo = mapper.Map<AddCargoCompanyDto, CargoCompany>(cargoCompanyDto);
```

Call site after:
```csharp
var cargo = mapper.MapToEntity(cargoCompanyDto);
```

Remove `using MapsterMapper;` from `CargoCompaniesManager.cs`.

**Remove from `MappingConfig.cs`:**
```csharp
// Remove these two lines:
config.NewConfig<AddCargoCompanyDto,CargoCompany>();
config.NewConfig<CargoCompany,AddCargoCompanyDto>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate CargoCompany to Mapperly CargoCompanyMapper`

---

### Task 4: Migrate BranchOffice

**Complexity:** Simple unidirectional (two Add/Edit DTOs → entity, no reverse needed).

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/BranchOfficeMapper.cs`
- `Application/Entegrasyon.Business/Concrete/BranchOfficeManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void BranchOfficeMapper_MapAddDtoToEntity_MapsName()
{
    var mapper = new BranchOfficeMapper();
    var dto = new BranchOfficeAddDto { Name = "Istanbul HQ" };
    var entity = mapper.MapToEntity(dto);
    entity.Name.Should().Be("Istanbul HQ");
}
```

Run — RED.

**Implement `BranchOfficeMapper.cs`:**
```csharp
using Entegrasyon.Entity.Core;
using Entegrasyon.Entity.Dtos.Branches;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BranchOfficeMapper
{
    public partial BranchOffice MapToEntity(BranchOfficeAddDto dto);
    public partial BranchOffice MapToEntity(BranchOfficeEditDto dto);
}
```

**Register in DI:** `services.AddSingleton<BranchOfficeMapper>();`

**Update `BranchOfficeManager.cs`:**
- Replace `IMapper mapper` with `BranchOfficeMapper mapper` in constructor
- Replace `mapper.Map<BranchOffice>(officeDto)` with `mapper.MapToEntity(officeDto)` (the one call in `AddBranchOffice` — `officeDto` is `BranchOfficeAddDto`)
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<BranchOfficeAddDto,BranchOffice>();
config.NewConfig<BranchOfficeEditDto,BranchOffice>();
// Also remove BranchOfficeStock profiles (AddBranchOfficeStockDto, EditBranchOfficeStockDto) —
// these are used inside ProductManager which is NOT yet migrated.
// Move BranchOfficeStock profiles to Task 6 (Product migration).
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate BranchOffice to Mapperly BranchOfficeMapper`

---

### Task 5: Migrate Brand

**Complexity:** Simple bidirectional. No custom logic.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/BrandMapper.cs`
- `Application/Entegrasyon.Business/Concrete/BrandService.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void BrandMapper_MapToEntity_MapsName()
{
    var mapper = new BrandMapper();
    var dto = new AddBrandDto { Name = "Nike" };
    var entity = mapper.MapToEntity(dto);
    entity.Name.Should().Be("Nike");
}
```

Run — RED.

**Implement `BrandMapper.cs`:**
```csharp
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BrandMapper
{
    public partial Brand MapToEntity(AddBrandDto dto);
    public partial AddBrandDto MapToDto(Brand entity);
}
```

**Test file update — `BrandServiceTests.cs`:**

Currently: `private readonly Mock<IMapper> _mockMapper = new();`

After migration `BrandService` will inject `BrandMapper` (a concrete class). Options:
- Extract `IBrandMapper` interface if mocking is needed
- OR use a real `BrandMapper` instance in the test (no mocking needed for a pure mapping class)

Use a real instance — simpler and tests the actual mapping:
```csharp
private readonly BrandMapper _brandMapper = new();
// Pass _brandMapper instead of _mockMapper.Object to BrandService constructor
```

**Register in DI:** `services.AddSingleton<BrandMapper>();`

**Update `BrandService.cs`:**
- Replace `IMapper mapper` with `BrandMapper mapper` in constructor
- Replace `mapper.Map<AddBrandDto, Brand>(brandDto)` with `mapper.MapToEntity(brandDto)`
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<AddBrandDto,Brand>();
config.NewConfig<Brand,AddBrandDto>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate Brand to Mapperly BrandMapper`

---

### Task 6: Migrate Sale

**Complexity:** One field rename (`UsedDiscountVoucherCode ↔ DiscountVoucherCode`), one simple flat mapping.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/SaleMapper.cs`
- `Application/Entegrasyon.Business/Concrete/SaleManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void SaleMapper_SaleItemToDto_RemapsDiscountVoucherCode()
{
    var mapper = new SaleMapper();
    var item = new SaleItem { UsedDiscountVoucherCode = "SAVE10" };
    var dto = mapper.MapToDto(item);
    dto.DiscountVoucherCode.Should().Be("SAVE10");
}

[Fact]
public void SaleMapper_DtoToSaleItem_RemapsDiscountVoucherCode()
{
    var mapper = new SaleMapper();
    var dto = new SaleItemDto { DiscountVoucherCode = "SAVE10" };
    var item = mapper.MapToEntity(dto);
    item.UsedDiscountVoucherCode.Should().Be("SAVE10");
}
```

Run — RED.

**Implement `SaleMapper.cs`:**
```csharp
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class SaleMapper
{
    [MapProperty(nameof(SaleItem.UsedDiscountVoucherCode), nameof(SaleItemDto.DiscountVoucherCode))]
    public partial SaleItemDto MapToDto(SaleItem item);

    [MapProperty(nameof(SaleItemDto.DiscountVoucherCode), nameof(SaleItem.UsedDiscountVoucherCode))]
    public partial SaleItem MapToEntity(SaleItemDto dto);

    public partial Sale MapToEntity(MakeSaleDto dto);
    public partial MakeSaleDto MapToDto(Sale sale);
}
```

**Register in DI:** `services.AddSingleton<SaleMapper>();`

**Update `SaleManager.cs`:**
- Replace `IMapper mapper` with `SaleMapper mapper` in constructor
- Replace `mapper.Map<Sale>(dto)` with `mapper.MapToEntity(dto)`
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<SaleItem,SaleItemDto>()
      .Map(dest => dest.DiscountVoucherCode,src => src.UsedDiscountVoucherCode);
config.NewConfig<SaleItemDto,SaleItem>()
      .Map(dest => dest.UsedDiscountVoucherCode,src => src.DiscountVoucherCode);
config.NewConfig<MakeSaleDto,Sale>();
config.NewConfig<Sale,MakeSaleDto>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate Sale to Mapperly SaleMapper`

---

### Task 7: Migrate Product + ProductVariant + BranchOfficeStock

**Complexity:** Medium. One null-coalescing (`VatRate ?? 0m`). Also fixes the broken `mapper.Map<ProductsDetailDto>(product)` call.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/ProductMapper.cs`
- `Application/Entegrasyon.Business/Concrete/ProductManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void ProductMapper_VatRateNull_DefaultsToZero()
{
    var mapper = new ProductMapper();
    var dto = new AddProductVariantDto { VatRate = null };
    var variant = mapper.MapToEntity(dto);
    variant.VatRate.Should().Be(0m);
}

[Fact]
public void ProductMapper_VatRateSet_PreservesValue()
{
    var mapper = new ProductMapper();
    var dto = new AddProductVariantDto { VatRate = 18m };
    var variant = mapper.MapToEntity(dto);
    variant.VatRate.Should().Be(18m);
}
```

Run — RED.

**Implement `ProductMapper.cs`:**
```csharp
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Core;
using Entegrasyon.Entity.Products;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class ProductMapper
{
    // Product
    [MapperIgnoreTarget(nameof(Product.SearchVector))]
    public partial Product MapToEntity(AddProductDto dto);
    public partial AddProductDto MapToDto(Product entity);

    [MapperIgnoreTarget(nameof(Product.SearchVector))]
    public partial Product MapToEntity(ProductEditDetailDto dto);

    // ProductVariant
    [MapProperty(nameof(AddProductVariantDto.VatRate), nameof(ProductVariant.VatRate),
                 Use = nameof(NullableDecimalToDecimal))]
    public partial ProductVariant MapToEntity(AddProductVariantDto dto);
    public partial AddProductVariantDto MapToDto(ProductVariant entity);

    [MapperIgnoreTarget(nameof(ProductVariant.VatRate))]
    public partial ProductVariant MapToEntity(ProductVariantEditDetailDto dto);

    // BranchOfficeStock
    public partial BranchOfficeStock MapToEntity(AddBranchOfficeStockDto dto);
    public partial AddBranchOfficeStockDto MapToDto(BranchOfficeStock entity);
    public partial BranchOfficeStock MapToEntity(EditBranchOfficeStockDto dto);
    public partial EditBranchOfficeStockDto MapToEditDto(BranchOfficeStock entity);

    private static decimal NullableDecimalToDecimal(decimal? value) => value ?? 0m;
}
```

**Note on `ProductVariantEditDetailDto → ProductVariant` VatRate:** Check the DTO definition. If `ProductVariantEditDetailDto.VatRate` is `decimal` (not nullable) then Mapperly maps it automatically without conversion. Only `AddProductVariantDto.VatRate` is nullable per MappingConfig. Verify types before adding converters.

**Fix the broken `GetProductByBarcode` mapping (line 101 of `ProductManager.cs`):**

Replace:
```csharp
var product = await dbContext.MainProducts
    .FirstOrDefaultAsync(x => x.ProductVariants.Any(pv => pv.Barcode == barcode));
if (product == null)
    return new ErrorResult("Barkoda ait ürün bulunamadı.");
return new SuccessDataResult<ProductsDetailDto>(mapper.Map<ProductsDetailDto>(product));
```

With a DB projection (same pattern as `GetProductsDetailsPageable`):
```csharp
var dto = await dbContext.MainProducts
    .Where(x => x.ProductVariants.Any(pv => pv.Barcode == barcode))
    .Select(x => new ProductsDetailDto(
        x.Id, x.Title, x.Description ?? "", x.StockCode ?? "",
        x.Brand!.Name!, x.Category!.Name!,
        x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
        x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
        x.ProductVariants.Count()))
    .FirstOrDefaultAsync();
if (dto == null)
    return new ErrorResult("Barkoda ait ürün bulunamadı.");
return new SuccessDataResult<ProductsDetailDto>(dto);
```

This removes the one remaining `mapper.Map<ProductsDetailDto>` call in ProductManager entirely — no Mapperly method needed for this type (it's always projected from DB).

**Test file update — `ProductManagerTests.cs` and `UpdateProductTests.cs`:**

Both have `Mock<IMapper>` but no `.Setup()` calls. The tests do not exercise code paths that call `mapper.Map`. Replace `Mock<IMapper>` with `ProductMapper` real instance:
```csharp
private readonly ProductMapper _productMapper = new();
// Pass _productMapper to ProductManager constructor instead of _mockMapper.Object
```

**Register in DI:** `services.AddSingleton<ProductMapper>();`

**Update `ProductManager.cs`:**
- Replace `IMapper mapper` with `ProductMapper mapper` in constructor
- Replace `mapper.Map<Product>(dto)` with `mapper.MapToEntity(dto)` (two calls for `AddProductDto` and `ProductEditDetailDto`)
- Remove the `mapper.Map<ProductsDetailDto>(product)` call as described above (replaced with DB projection)
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove Product, ProductVariant, BranchOfficeStock profiles (8 profiles total)
config.NewConfig<AddProductDto,Product>();
config.NewConfig<Product,AddProductDto>();
config.NewConfig<ProductEditDetailDto,Product>();
config.NewConfig<AddProductVariantDto,ProductVariant>()
      .Map(dest => dest.VatRate, src => src.VatRate ?? 0m);
config.NewConfig<ProductVariant,AddProductVariantDto>();
config.NewConfig<ProductVariantEditDetailDto,ProductVariant>();
config.NewConfig<AddBranchOfficeStockDto,BranchOfficeStock>();
config.NewConfig<BranchOfficeStock,AddBranchOfficeStockDto>();
config.NewConfig<EditBranchOfficeStockDto,BranchOfficeStock>();
config.NewConfig<BranchOfficeStock,EditBranchOfficeStockDto>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate Product/ProductVariant/BranchOfficeStock to Mapperly ProductMapper`

---

### Task 8: Migrate User

**Complexity:** One property rename (`BranchOfficeId → DefaultBranchOfficeId`). `UserEditDto → ApplicationUser` profile is dead code — deleted, not migrated.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/UserMapper.cs`
- `Application/Entegrasyon.Business/Concrete/Auth/ApplicationUserManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void UserMapper_AddUserDto_MapsBranchOfficeIdToDefaultBranchOfficeId()
{
    var mapper = new UserMapper();
    var dto = new AddUserDto { BranchOfficeId = 42, UserName = "test", Email = "t@t.com" };
    var user = mapper.MapToEntity(dto);
    user.DefaultBranchOfficeId.Should().Be(42);
}
```

Run — RED.

**Implement `UserMapper.cs`:**
```csharp
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class UserMapper
{
    [MapProperty(nameof(AddUserDto.BranchOfficeId), nameof(ApplicationUser.DefaultBranchOfficeId))]
    public partial ApplicationUser MapToEntity(AddUserDto dto);

    public partial AddUserDto MapToDto(ApplicationUser user);
}
```

**Note on `ApplicationUser` properties:** `ApplicationUser` inherits from IdentityUser — it has many identity-managed properties (`PasswordHash`, `SecurityStamp`, etc.) that have no matching field in `AddUserDto`. Mapperly will emit warnings for every unmapped target property. Suppress by adding `[MapperIgnoreTarget]` for each, OR configure the mapper with `[Mapper(UnmappedTargetMemberHandling = MemberHandling.Ignore)]` attribute option. Use the class-level option:

```csharp
[Mapper(UnmappedTargetMemberHandling = MemberHandling.Ignore)]
public partial class UserMapper
{
    [MapProperty(nameof(AddUserDto.BranchOfficeId), nameof(ApplicationUser.DefaultBranchOfficeId))]
    public partial ApplicationUser MapToEntity(AddUserDto dto);
    public partial AddUserDto MapToDto(ApplicationUser user);
}
```

**Register in DI:** `services.AddSingleton<UserMapper>();`

**Update `ApplicationUserManager.cs`:**
- Replace `IMapper mapper` with `UserMapper mapper` in constructor
- Replace `mapper.Map<AddUserDto, ApplicationUser>(dto)` with `mapper.MapToEntity(dto)`
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<AddUserDto,ApplicationUser>()
      .Map(dest => dest.DefaultBranchOfficeId,src => src.BranchOfficeId);
config.NewConfig<ApplicationUser,AddUserDto>();
config.NewConfig<UserEditDto,ApplicationUser>(); // dead code — just delete
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate User to Mapperly UserMapper`

---

### Task 9: Migrate Trendyol Import

**Complexity:** Nested source path, two Ignore(Id) guards. No call-site manager injects `IMapper` for this — check where these profiles are actually used.

**Investigation:** Search for `TrendyolCategoryAttribute`, `TrendyolBrand`, `TrendyolAttributeValue` mapper usage:
```bash
grep -rn "mapper.Map.*Trendyol\|Map.*TrendyolBrand\|Map.*TrendyolCategoryAttribute\|Map.*TrendyolAttributeValue" Application/ --include="*.cs"
```
If no call sites found: the Trendyol import profiles are dead code — delete from `MappingConfig.cs` without creating a Mapperly mapper. If call sites exist: implement `TrendyolImportMapper.cs` as below.

**Expected implementation if call sites exist:**

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void TrendyolImportMapper_MapCategoryAttribute_UsesAttributeName()
{
    var mapper = new TrendyolImportMapper();
    var src = new TrendyolCategoryAttribute
    {
        Attribute = new TrendyolAttribute { Name = "Renk" }
    };
    var dest = mapper.MapFromTrendyol(src);
    dest.CategoryAttributeKey.Should().Be("Renk");
    dest.CategoryAttributeHumanized.Should().Be("Renk");
}

[Fact]
public void TrendyolImportMapper_MapBrand_IgnoresId()
{
    var mapper = new TrendyolImportMapper();
    var src = new TrendyolBrand { Id = 999, Name = "Adidas" };
    var dest = mapper.MapFromTrendyol(src);
    dest.Id.Should().Be(0); // Id ignored
    dest.Name.Should().Be("Adidas");
}
```

**Implement `TrendyolImportMapper.cs`:**
```csharp
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class TrendyolImportMapper
{
    [MapProperty(
        [nameof(TrendyolCategoryAttribute.Attribute), nameof(TrendyolAttribute.Name)],
        nameof(CategoryAttribute.CategoryAttributeKey))]
    [MapProperty(
        [nameof(TrendyolCategoryAttribute.Attribute), nameof(TrendyolAttribute.Name)],
        nameof(CategoryAttribute.CategoryAttributeHumanized))]
    public partial CategoryAttribute MapFromTrendyol(TrendyolCategoryAttribute src);

    [MapperIgnoreTarget(nameof(Brand.Id))]
    public partial Brand MapFromTrendyol(TrendyolBrand src);

    [MapperIgnoreTarget(nameof(CategoryAttributeValue.Id))]
    public partial CategoryAttributeValue MapFromTrendyol(TrendyolAttributeValue src);
}
```

**Register in DI (only if used):** `services.AddSingleton<TrendyolImportMapper>();`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<TrendyolCategoryAttribute,CategoryAttribute>()
      .Map(dest => dest.CategoryAttributeKey,src => src.Attribute.Name)
      .Map(dest => dest.CategoryAttributeHumanized,src => src.Attribute.Name);
config.NewConfig<TrendyolBrand,Brand>()
      .Ignore(dest => dest.Id);
config.NewConfig<TrendyolAttributeValue,CategoryAttributeValue>()
      .Ignore(dest => dest.Id);
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate TrendyolImport to Mapperly TrendyolImportMapper`

---

### Task 10: Migrate BrandMarketPlaceMatch

**Complexity:** Read from nav-property (`ApplicationBrand.Name`), one hardcoded `""` field handled post-mapping.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/BrandMatchMapper.cs`
- `Application/Entegrasyon.Business/Concrete/BrandMatchService.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void BrandMatchMapper_MapToDto_ReadsApplicationBrandName()
{
    var mapper = new BrandMatchMapper();
    var entity = new BrandMarketPlaceMatch
    {
        ApplicationBrand = new Brand { Name = "Nike" }
    };
    var dto = mapper.MapToDto(entity);
    dto.ApplicationBrandName.Should().Be("Nike");
    dto.MarketPlaceBrandName.Should().Be(string.Empty);
}
```

Run — RED.

**Implement `BrandMatchMapper.cs`:**
```csharp
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BrandMatchMapper
{
    [MapProperty(
        [nameof(BrandMarketPlaceMatch.ApplicationBrand), nameof(Brand.Name)],
        nameof(BrandMarketPlaceMatchDto.ApplicationBrandName))]
    [MapperIgnoreTarget(nameof(BrandMarketPlaceMatchDto.MarketPlaceBrandName))]
    public partial BrandMarketPlaceMatchDto MapToDto(BrandMarketPlaceMatch src);

    public partial BrandMarketPlaceMatch MapToEntity(CreateBrandMarketPlaceMatchDto dto);

    public List<BrandMarketPlaceMatchDto> MapToDtoList(List<BrandMarketPlaceMatch> src)
        => src.Select(x =>
        {
            var dto = MapToDto(x);
            dto.MarketPlaceBrandName = string.Empty; // TODO: resolve from external API
            return dto;
        }).ToList();
}
```

**Note on `MarketPlaceBrandName`:** Since `[MapperIgnoreTarget]` leaves the property unset (default `null`), set it to `string.Empty` in the list mapper helper. The `BrandMarketPlaceMatchDto.MarketPlaceBrandName` property needs a public setter for this — verify it's not `init`-only.

**Test file update — `BrandMatchServiceTests.cs`:**
Replace `Mock<IMapper>` with `BrandMatchMapper` real instance.

**Register in DI:** `services.AddSingleton<BrandMatchMapper>();`

**Update `BrandMatchService.cs`:**
- Replace `IMapper mapper` with `BrandMatchMapper mapper` in constructor
- Replace both `mapper.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(mappings)` calls with `mapper.MapToDtoList(mappings)`
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<BrandMarketPlaceMatch, BrandMarketPlaceMatchDto>()
      .Map(dest => dest.ApplicationBrandName, src => src.ApplicationBrand.Name)
      .Map(dest => dest.MarketPlaceBrandName, src => "");
config.NewConfig<CreateBrandMarketPlaceMatchDto, BrandMarketPlaceMatch>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate BrandMarketPlaceMatch to Mapperly BrandMatchMapper`

---

### Task 11: Migrate Category

**Complexity:** Conditional null mapping (`SuperCategoryId == 0 → null`), two `Ignore(CategoryAttributes)` guards.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/CategoryMapper.cs`
- `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void CategoryMapper_ZeroSuperCategoryId_MapsToNull()
{
    var mapper = new CategoryMapper();
    var dto = new AddCategoryDto { Name = "Test", SuperCategoryId = 0 };
    var entity = mapper.MapToEntity(dto);
    entity.SuperCategoryId.Should().BeNull();
}

[Fact]
public void CategoryMapper_NonZeroSuperCategoryId_MapsValue()
{
    var mapper = new CategoryMapper();
    var dto = new AddCategoryDto { Name = "Child", SuperCategoryId = 5 };
    var entity = mapper.MapToEntity(dto);
    entity.SuperCategoryId.Should().Be(5);
}

[Fact]
public void CategoryMapper_CategoryAttributes_NotMapped()
{
    var mapper = new CategoryMapper();
    var dto = new AddCategoryDto { Name = "Test", SuperCategoryId = 0 };
    var entity = mapper.MapToEntity(dto);
    entity.CategoryAttributes.Should().BeNullOrEmpty();
}
```

Run — RED.

**Implement `CategoryMapper.cs`:**
```csharp
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryMapper
{
    // AddCategoryDto → Category: ignore nav-prop, conditional null for SuperCategoryId
    [MapperIgnoreTarget(nameof(Category.CategoryAttributes))]
    [MapProperty(nameof(AddCategoryDto.SuperCategoryId), nameof(Category.SuperCategoryId),
                 Use = nameof(ZeroToNull))]
    public partial Category MapToEntity(AddCategoryDto dto);

    public partial AddCategoryDto MapToDto(Category entity);

    // CategoryEditDetailDto ↔ Category
    public partial Category MapToEntity(CategoryEditDetailDto dto);
    public partial CategoryEditDetailDto MapToEditDetailDto(Category entity);

    // EditCategoryDto → Category: ignore nav-prop
    [MapperIgnoreTarget(nameof(Category.CategoryAttributes))]
    public partial Category MapToEntity(EditCategoryDto dto);
    public partial EditCategoryDto MapToEditDto(Category entity);

    // AddCategoryDtoStepOne ↔ Category
    public partial Category MapToEntity(AddCategoryDtoStepOne dto);
    public partial AddCategoryDtoStepOne MapToStepOneDto(Category entity);

    private static int? ZeroToNull(int value) => value == 0 ? null : value;
}
```

**Test file update — `CategoryManagerTests.cs`:**

Currently one setup: `_mockMapper.Setup(m => m.Map<Category>(dto)).Returns(new Category { ... })`.

After migration `CategoryManager` injects `CategoryMapper`. The test constructs `CategoryManager` with a `CategoryMapper` instance. Update the test that had the mock setup:

```csharp
private readonly CategoryMapper _categoryMapper = new();
// In constructor: pass _categoryMapper instead of _mockMapper.Object

// The test that relied on _mockMapper.Setup no longer needs setup —
// CategoryMapper.MapToEntity(dto) will produce a real Category from dto.
// Adjust the test to work with the real mapped result or set dto fields appropriately.
```

Specifically the test that did:
```csharp
_mockMapper.Setup(m => m.Map<Category>(dto)).Returns(new Category { Id = 99, Name = "Child", SuperCategoryId = 10 });
```
This set a specific `Id = 99` which Mapperly cannot do (entity Id is set by DB). The test should be updated: the `Category` returned by `mapper.MapToEntity(dto)` will have `Id = 0` (default) — if the test assertion depends on `Id = 99`, the test is brittle and should be rethought. Likely the assertion doesn't depend on Id — verify and update.

**Register in DI:** `services.AddSingleton<CategoryMapper>();`

**Update `CategoryManager.cs`:**
- Replace `IMapper mapper` with `CategoryMapper mapper` in constructor
- First `mapper.Map<Category>(dto)` where `dto` is `AddCategoryDtoStepOne` → `mapper.MapToEntity(dto)`
- Second `mapper.Map<Category>(dto)` where `dto` is `AddCategoryDto` → `mapper.MapToEntity(dto)`
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<AddCategoryDto,Category>()
      .Ignore(dest => dest.CategoryAttributes)
      .Map(dest => dest.SuperCategoryId,src => src.SuperCategoryId == 0 ? (int?)null : src.SuperCategoryId);
config.NewConfig<Category,AddCategoryDto>();
config.NewConfig<CategoryEditDetailDto,Category>();
config.NewConfig<Category,CategoryEditDetailDto>();
config.NewConfig<EditCategoryDto,Category>()
      .Ignore(dest => dest.CategoryAttributes);
config.NewConfig<Category,EditCategoryDto>();
config.NewConfig<AddCategoryDtoStepOne,Category>();
config.NewConfig<Category,AddCategoryDtoStepOne>();
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate Category to Mapperly CategoryMapper`

---

### Task 12: Migrate CategoryAttribute (includes factory method)

**Complexity:** High. `EditCategoryAttributeDto → CategoryAttributeCategory` writes into a nested nav-property — converted to a static factory method.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/CategoryAttributeMapper.cs`
- `Application/Entegrasyon.Business/Concrete/CategoryAttributeManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void CategoryAttributeMapper_MapToEntity_IgnoresCategories()
{
    var mapper = new CategoryAttributeMapper();
    var dto = new AddCategoryAttributeDto { CategoryAttributeKey = "color" };
    var entity = mapper.MapToEntity(dto);
    entity.Categories.Should().BeNullOrEmpty();
}

[Fact]
public void CategoryAttributeMapper_ToJunctionUpdate_BuildsNestedObject()
{
    var dto = new EditCategoryAttributeDto
    {
        Id = 7,
        CategoryAttributeKey = "color",
        CategoryAttributeHumanized = "Renk",
        AllowCustom = true
    };
    var junction = CategoryAttributeMapper.ToJunctionUpdate(dto);
    junction.CategoryAttributeId.Should().Be(7);
    junction.CategoryAttribute.Id.Should().Be(7);
    junction.CategoryAttribute.CategoryAttributeHumanized.Should().Be("Renk");
    junction.CategoryAttribute.AllowCustom.Should().BeTrue();
}
```

Run — RED.

**Implement `CategoryAttributeMapper.cs`:**
```csharp
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryAttributeMapper
{
    [MapperIgnoreTarget(nameof(CategoryAttribute.Categories))]
    public partial CategoryAttribute MapToEntity(AddCategoryAttributeDto dto);

    /// <summary>
    /// Builds a CategoryAttributeCategory graph for an edit update.
    /// Mapperly cannot write into nested nav-property paths — this is a manual factory method.
    /// </summary>
    public static CategoryAttributeCategory ToJunctionUpdate(EditCategoryAttributeDto dto) => new()
    {
        CategoryAttributeId = dto.Id,
        CategoryAttribute = new CategoryAttribute
        {
            Id = dto.Id,
            CategoryAttributeKey = dto.CategoryAttributeKey,
            CategoryAttributeHumanized = dto.CategoryAttributeHumanized,
            CategoryAttributeValues = dto.CategoryAttributeValues,
            AllowCustom = dto.AllowCustom
        }
    };
}
```

**Register in DI:** `services.AddSingleton<CategoryAttributeMapper>();`

**Update `CategoryAttributeManager.cs`:**
- Replace `IMapper mapper` with `CategoryAttributeMapper mapper` in constructor
- Replace `mapper.Map<CategoryAttribute>(dto)` with `mapper.MapToEntity(dto)` in `AddCategoryAttribute`
- If `UpdateCategoryAttribute` calls the `EditCategoryAttributeDto → CategoryAttributeCategory` mapping anywhere, replace with `CategoryAttributeMapper.ToJunctionUpdate(dto)` (verify — the current code in `UpdateCategoryAttribute` does manual property assignment, not mapping)
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<AddCategoryAttributeDto,CategoryAttribute>()
     .Ignore(dest => dest.Categories);
config.NewConfig<AddCategoryAttributeDto,CategoryAttribute>(); // duplicate — remove both
config.NewConfig<EditCategoryAttributeDto,CategoryAttributeCategory>()
      .Map(dest => dest.CategoryAttributeId,src => src.Id)
      // ... rest of nested mapping
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate CategoryAttribute to Mapperly CategoryAttributeMapper`

---

### Task 13: Migrate Customer (polymorphic)

**Complexity:** Highest. Polymorphic dispatch (`CustomerAddDto → RetailCustomer | CorporateCustomer`), `NpgsqlTsVector` fields to ignore, `FuncMappings.CustomerToDetailDto` already handles the complex projection mapping — it stays as-is.

**Files to modify:**
- `Application/Entegrasyon.Business/Mappers/CustomerMapper.cs`
- `Application/Entegrasyon.Business/Concrete/CustomerManager.cs`
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`

**Note on `FuncMappings.CustomerToDetailDto`:** This function already correctly handles the `Customer → CustomerDetailDto` projection (used in LINQ `.Select()` calls). The Mapster profiles `config.NewConfig<RetailCustomer, CustomerDetailDto>()` and `config.NewConfig<CorporateCustomer, CustomerDetailDto>()` are only used by the two `_mapper.Map<CustomerDetailDto>()` calls in `AddCustomer`. After migration, those two calls use Mapperly directly. `FuncMappings.CustomerToDetailDto` is NOT replaced — it stays.

**TDD — Add to `MapperConversionTests.cs`:**
```csharp
[Fact]
public void CustomerMapper_MapToRetail_MapsNationalIdentity()
{
    var mapper = new CustomerMapper();
    var dto = new CustomerAddDto { NationalIdentity = "12345678901", CustomerType = "Retail" };
    var entity = mapper.MapToRetail(dto);
    entity.NationalIdentity.Should().Be("12345678901");
}

[Fact]
public void CustomerMapper_MapToCorporate_MapsNationalIdentityToTaxNumber()
{
    var mapper = new CustomerMapper();
    var dto = new CustomerAddDto { NationalIdentity = "1234567890", CustomerType = "Corporate" };
    var entity = mapper.MapToCorporate(dto);
    entity.TaxNumber.Should().Be("1234567890");
}

[Fact]
public void CustomerMapper_MapFromRetail_MapsFullNameToNameSurname()
{
    var mapper = new CustomerMapper();
    var customer = new RetailCustomer { Name = "Ali", Surname = "Veli" };
    // FullName is computed — set manually for test
    customer.FullName = "Ali Veli";
    var dto = mapper.MapFromRetail(customer);
    dto.NameSurname.Should().Be("Ali Veli");
}
```

Run — RED.

**Implement `CustomerMapper.cs`:**
```csharp
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(UnmappedTargetMemberHandling = MemberHandling.Ignore)]
public partial class CustomerMapper
{
    // CustomerAddDto → polymorphic targets
    public partial RetailCustomer MapToRetail(CustomerAddDto dto);

    [MapProperty(nameof(CustomerAddDto.NationalIdentity), nameof(CorporateCustomer.TaxNumber))]
    public partial CorporateCustomer MapToCorporate(CustomerAddDto dto);

    // Entity → DTO (used in AddCustomer for the newly-created entity)
    [MapProperty(nameof(RetailCustomer.FullName), nameof(CustomerDetailDto.NameSurname))]
    [MapProperty(nameof(RetailCustomer.NationalIdentity), nameof(CustomerDetailDto.NationalIdentityOrTaxNumber))]
    [MapperIgnoreSource(nameof(RetailCustomer.RetailSearchVector))]
    public partial CustomerDetailDto MapFromRetail(RetailCustomer src);

    [MapProperty(nameof(Customer.FullName), nameof(CustomerDetailDto.NameSurname))]
    [MapperIgnoreSource(nameof(CorporateCustomer.CorporateSearchVector))]
    public partial CustomerDetailDto MapFromCustomer(Customer src);
}
```

**Post-mapping hook for `SalesCount`:**
```csharp
partial void AfterMapFromRetail(RetailCustomer src, CustomerDetailDto dest)
{
    // SalesCount is not automatically mappable — set from collection
    // Note: dest.SalesCount has init setter — if init-only, post-map hooks cannot set it.
    // Verify CustomerDetailDto.SalesCount setter. If init-only, set after map in CustomerManager.
}
```

**Check `CustomerDetailDto.SalesCount`:** The property is declared as `public int SalesCount { get; init; }`. This means Mapperly's AfterMap hook cannot set it (init-only). Handle in `CustomerManager.AddCustomer` by re-constructing the DTO or use the full constructor:

```csharp
// In CustomerManager.AddCustomer, after mapping:
Customer customer = dto.CustomerType == "Retail"
    ? mapper.MapToRetail(dto)
    : mapper.MapToCorporate(dto);
dbContext.Customers.Add(customer);
await dbContext.SaveChangesAsync();

CustomerDetailDto result;
if (customer is RetailCustomer retailCustomer)
{
    var mapped = mapper.MapFromRetail(retailCustomer);
    result = new CustomerDetailDto(mapped.CreatedAt, mapped.Id,
        mapped.NationalIdentityOrTaxNumber, mapped.NameSurname,
        mapped.CorporateName, retailCustomer.Sales?.Count() ?? 0,
        mapped.PhoneNumber, mapped.Address, mapped.CustomerType);
}
else
{
    var mapped = mapper.MapFromCustomer(customer);
    result = new CustomerDetailDto(mapped.CreatedAt, mapped.Id,
        mapped.NationalIdentityOrTaxNumber, mapped.NameSurname,
        mapped.CorporateName, customer.Sales?.Count() ?? 0,
        mapped.PhoneNumber, mapped.Address, mapped.CustomerType);
}
```

**Register in DI:** `services.AddSingleton<CustomerMapper>();`

**Update `CustomerManager.cs`:**
- Replace `private readonly IMapper _mapper` with `private readonly CustomerMapper _mapper`
- Replace constructor parameter `IMapper mapper` with `CustomerMapper mapper`
- Replace `_mapper.Map<RetailCustomer>(dto)` with `_mapper.MapToRetail(dto)`
- Replace `_mapper.Map<CorporateCustomer>(dto)` with `_mapper.MapToCorporate(dto)`
- Replace `_mapper.Map<CustomerDetailDto>(retailCustomer)` and `_mapper.Map<CustomerDetailDto>(customer)` with the pattern above
- Remove `using MapsterMapper;`

**Remove from `MappingConfig.cs`:**
```csharp
// Remove:
config.NewConfig<CustomerAddDto,RetailCustomer>()
      .Map(dest => dest.NationalIdentity,src => src.NationalIdentity);
config.NewConfig<CustomerAddDto,CorporateCustomer>()
      .Map(dest => dest.TaxNumber,src => src.NationalIdentity);
config.NewConfig<Customer,CustomerDetailDto>()
      .Map(dest => dest.SalesCount,src => src.Sales.Count());
config.NewConfig<RetailCustomer,CustomerDetailDto>()
      .Map(dest => dest.NameSurname,src => src.FullName);
config.NewConfig<CorporateCustomer,CustomerDetailDto>()
      .Map(dest => dest.NameSurname,src => src.FullName);
```

Also remove the `Pageable<T>` dead-code profile:
```csharp
// Remove (dead code — no call sites):
config.NewConfig(typeof(Pageable<>),typeof(Pageable<>));
```

**Run tests:** GREEN.

**Commit:** `feat(mappers): migrate Customer to Mapperly CustomerMapper`

---

## Phase 3: Remove Mapster Entirely

### Task 14: Final cleanup — delete MappingConfig and remove Mapster packages

**Pre-condition:** All domain areas migrated. `MappingConfig.cs` contains only the `AddBusinessMapping` method scaffold and the `services.AddSingleton(config); services.AddMapster();` calls. All `config.NewConfig<>()` calls have been removed in previous tasks.

**Verify `MappingConfig.cs` is empty of profiles:**
```bash
grep -c "NewConfig" Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs
# Expected: 0
```

**Step 1 — Delete MappingConfig.cs:**
```bash
rm Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs
```

**Step 2 — Remove `AddBusinessMapping()` call from DI:**

In `ApplicationDependencyExtension.cs`, find and remove:
```csharp
services.AddBusinessMapping();
```
Also remove any `using Entegrasyon.Business.MapperProfiles;` import.

**Step 3 — Remove Mapster NuGet from all three csproj files:**

`Entegrasyon.Business.csproj`:
```xml
<!-- Remove: -->
<PackageReference Include="Mapster" Version="7.4.0" />
<PackageReference Include="Mapster.DependencyInjection" Version="1.0.1" />
```

`Entegrasyon.ApplicationBootstrap.csproj`:
```xml
<!-- Remove: -->
<PackageReference Include="Mapster" Version="7.4.0" />
<PackageReference Include="Mapster.DependencyInjection" Version="1.0.1" />
```

`Entegrasyon.Blazor.csproj`:
```xml
<!-- Remove: -->
<PackageReference Include="Mapster" Version="7.4.0" />
<PackageReference Include="Mapster.DependencyInjection" Version="1.0.1" />
```

**Step 4 — Build to find any remaining Mapster references:**
```bash
dotnet build Entegrasyon.sln
```
Any remaining `using Mapster;` or `using MapsterMapper;` become compile errors — fix each.

**Step 5 — Run full test suite:**
```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

**Commit:** `feat(mappers): remove Mapster packages and MappingConfig — Mapperly migration complete`

---

## Test File Summary

| Test File | Current Mock | Migration Action |
|---|---|---|
| `CategoryManagerTests.cs` | `Mock<IMapper>` + 1 `.Setup()` | Replace with real `CategoryMapper`. Update the one test that relied on mock-returned `Id = 99` — set DTO fields instead |
| `ProductManagerTests.cs` | `Mock<IMapper>`, no `.Setup()` | Replace with real `ProductMapper` |
| `UpdateProductTests.cs` | `Mock<IMapper>`, no `.Setup()` | Replace with real `ProductMapper` |
| `BrandServiceTests.cs` | `Mock<IMapper>`, no `.Setup()` | Replace with real `BrandMapper` |
| `BrandMatchServiceTests.cs` | `Mock<IMapper>`, no `.Setup()` | Replace with real `BrandMatchMapper` |

---

## DI Registration Summary

Add to `ApplicationDependencyExtension.cs`:
```csharp
services.AddSingleton<CargoCompanyMapper>();
services.AddSingleton<BranchOfficeMapper>();
services.AddSingleton<BrandMapper>();
services.AddSingleton<SaleMapper>();
services.AddSingleton<ProductMapper>();
services.AddSingleton<UserMapper>();
services.AddSingleton<TrendyolImportMapper>();   // only if call sites found in Task 9
services.AddSingleton<BrandMatchMapper>();
services.AddSingleton<CategoryMapper>();
services.AddSingleton<CategoryAttributeMapper>();
services.AddSingleton<CustomerMapper>();
// SettingMapper is static — no registration needed
```

---

## New Files Checklist

```
Application/Entegrasyon.Business/Mappers/
  SettingMapper.cs           (Task 2 — static)
  CargoCompanyMapper.cs      (Task 3)
  BranchOfficeMapper.cs      (Task 4)
  BrandMapper.cs             (Task 5)
  SaleMapper.cs              (Task 6)
  ProductMapper.cs           (Task 7)
  UserMapper.cs              (Task 8)
  TrendyolImportMapper.cs    (Task 9 — conditional)
  BrandMatchMapper.cs        (Task 10)
  CategoryMapper.cs          (Task 11)
  CategoryAttributeMapper.cs (Task 12)
  CustomerMapper.cs          (Task 13)

Test/Entegrasyon.Test/Mappers/
  MapperConversionTests.cs   (Task 2 — grows through tasks 2–13)
```

---

## Verification After Each Task

```bash
# Unit tests must stay green after every task
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj

# Integration tests after Phase 3 completion
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj

# Build must be clean (zero Mapperly warnings = zero unmapped properties)
dotnet build Entegrasyon.sln
```
