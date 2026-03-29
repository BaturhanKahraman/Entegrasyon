# Mapperly Migration Design

**Date:** 2026-03-30
**Status:** Draft
**Scope:** Replace Mapster (reflection-based) with Mapperly (source-generator-based) across the Business layer

---

## 1. Background and Motivation

Mapster 7.4 works at runtime using reflection. Every call to `.Adapt<T>()` or `mapper.Map<T>()` invokes reflection machinery at call time. Mapperly generates plain C# mapping code at compile time — the output is ordinary method calls with no reflection, no dictionary lookups, and no delegate caching.

**Why migrate:**

| Concern | Mapster (current) | Mapperly (target) |
|---|---|---|
| Reflection at runtime | Yes | No — source-generated code |
| Compile-time type errors | No — fails silently at runtime | Yes — build error immediately |
| Debuggability | Black box | Generated code is readable |
| Startup cost | TypeAdapterConfig warmup | Zero |
| Null-safety analysis | None | Full Roslyn analysis |
| Property-name typo detection | Silent wrong mapping | Build error |

The migration risk is low: the codebase has 45 mapping profiles, 19 `mapper.Map` / `.Adapt` call sites, and 9 services injecting `IMapper`. This is a bounded, auditable surface.

---

## 2. Current State Inventory

### 2.1 Package references

Three projects reference Mapster:

- `Entegrasyon.Business` — `Mapster 7.4.0` + `Mapster.DependencyInjection 1.0.1`
- `Entegrasyon.ApplicationBootstrap` — same
- `Entegrasyon.Blazor` — same (transitive, not used directly in Blazor code)

### 2.2 Mapping profiles (MappingConfig.cs)

All 45 profiles live in `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`. Grouped by domain area:

| Domain area | Profiles | Complexity |
|---|---|---|
| User | `AddUserDto ↔ ApplicationUser`, `UserEditDto → ApplicationUser` | Custom: `DefaultBranchOfficeId ← BranchOfficeId` |
| Brand | `AddBrandDto ↔ Brand` | Simple |
| Brand Marketplace Match | `BrandMarketPlaceMatch → BrandMarketPlaceMatchDto`, `CreateBrandMarketPlaceMatchDto → BrandMarketPlaceMatch` | Custom: `ApplicationBrandName ← ApplicationBrand.Name`, `MarketPlaceBrandName ← ""` (hardcoded) |
| Cargo Company | `AddCargoCompanyDto ↔ CargoCompany` | Simple |
| Customer | `CustomerAddDto → RetailCustomer/CorporateCustomer`, `Customer/RetailCustomer/CorporateCustomer → CustomerDetailDto` | Polymorphic: same DTO from two entity subclasses; computed: `SalesCount ← Sales.Count()`, `NameSurname ← FullName` |
| Product | `AddProductDto ↔ Product`, `ProductEditDetailDto → Product` | Simple |
| Product Variant | `AddProductVariantDto ↔ ProductVariant`, `ProductVariantEditDetailDto → ProductVariant` | Custom: `VatRate ← VatRate ?? 0m` |
| Branch Office Stock | `AddBranchOfficeStockDto ↔ BranchOfficeStock`, `EditBranchOfficeStockDto ↔ BranchOfficeStock` | Simple |
| Branch Office | `BranchOfficeAddDto → BranchOffice`, `BranchOfficeEditDto → BranchOffice` | Simple |
| Category | `AddCategoryDto ↔ Category` (Ignore CategoryAttributes, `SuperCategoryId ← src == 0 ? null : src`), `CategoryEditDetailDto ↔ Category`, `EditCategoryDto ↔ Category` (Ignore CategoryAttributes), `AddCategoryDtoStepOne ↔ Category` | Medium: conditional null mapping, ignored nav property |
| Category Attribute | `AddCategoryAttributeDto → CategoryAttribute` (Ignore Categories), `EditCategoryAttributeDto → CategoryAttributeCategory` | **Complex**: deep property remapping to nested nav-property paths |
| Trendyol Import | `TrendyolCategoryAttribute → CategoryAttribute`, `TrendyolBrand → Brand` (Ignore Id), `TrendyolAttributeValue → CategoryAttributeValue` (Ignore Id) | Medium |
| Sale | `SaleItem ↔ SaleItemDto` (renamed field), `MakeSaleDto ↔ Sale` | Custom: `UsedDiscountVoucherCode ↔ DiscountVoucherCode` rename |
| Application Settings | Not in `MappingConfig` — uses raw `.Adapt<T>()` with Mapster global defaults | Simple flat copy |
| Generic pageable | `Pageable<T> → Pageable<T>` open generic | Structural only |

### 2.3 Call sites

**`IMapper.Map` calls (19 total across 9 manager files):**

| File | Calls | Notes |
|---|---|---|
| `ApplicationUserManager.cs` | 1 | `mapper.Map<AddUserDto, ApplicationUser>(dto)` |
| `BranchOfficeManager.cs` | 1 | `mapper.Map<BranchOffice>(officeDto)` |
| `BrandMatchService.cs` | 2 | `mapper.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(mappings)` (twice) |
| `BrandService.cs` | 1 | `mapper.Map<AddBrandDto, Brand>(brandDto)` |
| `CargoCompaniesManager.cs` | 1 | `mapper.Map<AddCargoCompanyDto, CargoCompany>(cargoCompanyDto)` |
| `CategoryAttributeManager.cs` | 1 | `mapper.Map<CategoryAttribute>(dto)` |
| `CategoryManager.cs` | 2 | `mapper.Map<Category>(dto)` — two different overloads |
| `CustomerManager.cs` | 4 | Polymorphic: `_mapper.Map<RetailCustomer>(dto)`, `_mapper.Map<CorporateCustomer>(dto)`, `_mapper.Map<CustomerDetailDto>(retailCustomer)`, `_mapper.Map<CustomerDetailDto>(customer)` |
| `ProductManager.cs` | 2 | `mapper.Map<Product>(dto)`, `mapper.Map<ProductsDetailDto>(product)` |
| `SaleManager.cs` | 1 | `mapper.Map<Sale>(dto)` |

**`.Adapt<T>()` calls (3 total — no IMapper):**

| File | Calls | Notes |
|---|---|---|
| `ApplicationSettingManager.cs` | 3 | `settings.Adapt<List<ApplicationSettingDto>>()`, `setting?.Adapt<ApplicationSettingDto>()` — no custom config, pure flat copy |

---

## 3. Complex Mapping Analysis

### 3.1 EditCategoryAttributeDto → CategoryAttributeCategory (highest complexity)

This mapping is the most unusual in the codebase. In MappingConfig:

```csharp
config.NewConfig<EditCategoryAttributeDto, CategoryAttributeCategory>()
      .Map(dest => dest.CategoryAttributeId, src => src.Id)
      .Map(dest => dest.CategoryAttribute.Id, src => src.Id)
      .Map(dest => dest.CategoryAttribute.CategoryAttributeHumanized, src => src.CategoryAttributeHumanized)
      .Map(dest => dest.CategoryAttribute.CategoryAttributeValues, src => src.CategoryAttributeValues)
      .Map(dest => dest.CategoryAttribute.AllowCustom, src => src.AllowCustom)
      .Map(dest => dest.CategoryAttribute.CategoryAttributeKey, src => src.CategoryAttributeKey);
```

It writes into a nested navigation property (`CategoryAttribute.*`) rather than a flat object. Mapperly does not support writing to navigation properties via `[MapProperty]` chains — the nested object must be instantiated by the caller or created via a factory method.

**Migration approach:** Convert to a manual factory method. This mapping is already architecturally suspect (it builds a partial entity graph for a junction table update). A factory method is cleaner:

```csharp
public static CategoryAttributeCategory ToJunctionUpdate(EditCategoryAttributeDto dto) => new()
{
    CategoryAttributeId = dto.Id,
    CategoryAttribute = new CategoryAttribute
    {
        Id = dto.Id,
        CategoryAttributeHumanized = dto.CategoryAttributeHumanized,
        CategoryAttributeValues = dto.CategoryAttributeValues,
        AllowCustom = dto.AllowCustom,
        CategoryAttributeKey = dto.CategoryAttributeKey
    }
};
```

### 3.2 Customer polymorphic mapping

`CustomerAddDto → RetailCustomer` and `CustomerAddDto → CorporateCustomer` are two separate target types from one source. `Customer/RetailCustomer/CorporateCustomer → CustomerDetailDto` maps base class and two subclasses to the same DTO. Mapperly handles this cleanly via separate explicit methods:

```csharp
CustomerDetailDto MapFromRetail(RetailCustomer src);
CustomerDetailDto MapFromCorporate(CorporateCustomer src);
RetailCustomer MapToRetail(CustomerAddDto src);
CorporateCustomer MapToCorporate(CustomerAddDto src);
```

The polymorphic dispatch at call sites remains in `CustomerManager` — it already uses `if (isRetail)` branching and calls the appropriate mapper method.

### 3.3 BrandMarketPlaceMatch → BrandMarketPlaceMatchDto (hardcoded field)

`MarketPlaceBrandName` is mapped to `""` as a placeholder (TODO comment in source). Mapperly cannot express a hardcoded constant as a `[MapProperty]`. Options:

- `[MapProperty]` with `[MapperIgnore]` on the destination property, then set it manually after mapping
- A custom partial method `partial void After(BrandMarketPlaceMatch src, BrandMarketPlaceMatchDto dest)` using Mapperly's `[AfterMap]`
- Simply: map everything auto + set the field in a post-mapping step in `BrandMatchService`

The cleanest approach is post-mapping assignment (already done as `""` so it's a no-op for now).

### 3.4 ProductVariant VatRate null-coalescing

```csharp
.Map(dest => dest.VatRate, src => src.VatRate ?? 0m)
```

In Mapperly this is handled with `[MapProperty]` plus a custom conversion method:

```csharp
private static decimal NullableToDecimal(decimal? value) => value ?? 0m;
```

Or, if the `ProductVariant.VatRate` is declared as `decimal` (not `decimal?`) and `AddProductVariantDto.VatRate` is `decimal?`, Mapperly's built-in null-to-default mapping handles it automatically when `NullableReferenceTypesAttributeHandling` is set to `TreatAsNullable`.

### 3.5 Category SuperCategoryId conditional null

```csharp
.Map(dest => dest.SuperCategoryId, src => src.SuperCategoryId == 0 ? (int?)null : src.SuperCategoryId)
```

Mapperly supports this via a `[MapProperty]` with a custom conversion method:

```csharp
private static int? ZeroToNull(int value) => value == 0 ? null : value;
```

### 3.6 SaleItem ↔ SaleItemDto field rename

`UsedDiscountVoucherCode` ↔ `DiscountVoucherCode` is a simple `[MapProperty]` attribute in Mapperly.

### 3.7 Trendyol import: nested source property

```csharp
.Map(dest => dest.CategoryAttributeKey, src => src.Attribute.Name)
```

Mapperly handles `src.Attribute.Name` via `[MapProperty(nameof(TrendyolCategoryAttribute.Attribute) + "." + nameof(...), Use = ...)]` or more commonly:

```csharp
[MapProperty([nameof(TrendyolCategoryAttribute.Attribute), nameof(TrendyolAttribute.Name)], nameof(CategoryAttribute.CategoryAttributeKey))]
```

This is fully supported.

---

## 4. Target Architecture

### 4.1 Mapper class layout — one partial class per domain area

All mapper classes live in `Application/Entegrasyon.Business/Mappers/`:

```
Mappers/
  UserMapper.cs
  BrandMapper.cs
  CargoCompanyMapper.cs
  CustomerMapper.cs
  ProductMapper.cs
  CategoryMapper.cs
  CategoryAttributeMapper.cs
  SaleMapper.cs
  SettingMapper.cs
  TrendyolImportMapper.cs
```

Each file:

```csharp
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryMapper
{
    // Mapperly generates all methods at compile time
    public partial Category MapToCategory(AddCategoryDtoStepOne dto);
    public partial AddCategoryDtoStepOne MapFromCategory(Category category);
    // ...
}
```

### 4.2 DI registration

Mapperly mappers are plain classes — no framework registration needed. Two options:

**Option A — Singleton DI registration (recommended):** Register each mapper as a singleton in `ApplicationDependencyExtension.cs`. Managers inject the specific mapper they need instead of `IMapper`.

```csharp
services.AddSingleton<CategoryMapper>();
services.AddSingleton<ProductMapper>();
// etc.
```

**Option B — Static methods:** For mappers with no injected dependencies, use `static partial class`. No DI needed — call as `CategoryMapper.MapToCategory(dto)`. Simpler but harder to mock in tests.

Recommendation: **Option A** for managers that already inject IMapper (smooth refactor), **Option B** for `ApplicationSettingManager` (currently uses `.Adapt<T>()` with no injected mapper — convert to static).

### 4.3 Injection pattern change

Before (Mapster):
```csharp
public class CategoryManager(IMapper mapper, ...) : ICategoryService
{
    var category = mapper.Map<Category>(dto);
}
```

After (Mapperly):
```csharp
public class CategoryManager(CategoryMapper mapper, ...) : ICategoryService
{
    var category = mapper.MapToCategory(dto);
}
```

The `IMapper` abstraction disappears — which is intentional. Mapperly mappers are typed and specific. If cross-cutting mapper abstraction is needed in tests, create a thin interface `ICategoryMapper` with the exact methods the mapper exposes, and have `CategoryMapper` implement it.

---

## 5. Migration Strategy — Three Phases

### Phase 1: Infrastructure setup (no functional change)

**Goal:** Add Mapperly to the project and create empty mapper skeletons. Mapster continues to work unchanged.

Steps:
1. Add `Riok.Mapperly` NuGet to `Entegrasyon.Business.csproj` (analyzer + runtime package)
2. Create `Application/Entegrasyon.Business/Mappers/` directory
3. Create skeleton partial classes (empty, no methods yet) for each domain area
4. Build — verify zero errors
5. No DI changes, no call-site changes

Package reference to add:
```xml
<PackageReference Include="Riok.Mapperly" Version="3.*" />
```

Note: Mapperly 3.x targets .NET 8. The package includes both the source generator and the `Riok.Mapperly.Abstractions` DLL needed for attributes. No separate abstractions package is required.

### Phase 2: Migrate by domain area (Mapster and Mapperly coexist)

Migrate one domain area at a time. For each area:

1. Implement the Mapperly partial class with all required methods and attributes
2. Register the mapper in DI
3. Update manager constructor: replace `IMapper mapper` with `XxxMapper mapper`
4. Replace `mapper.Map<T>(src)` calls with `mapper.MapToXxx(src)` at each call site
5. Remove `using MapsterMapper;` from the migrated file
6. Run tests: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
7. Remove the corresponding `config.NewConfig<...>()` entries from `MappingConfig.cs`

**Recommended migration order** (simplest to most complex):

| Order | Area | Complexity | Call sites |
|---|---|---|---|
| 1 | Application Settings | Trivial flat copy, uses static `.Adapt<T>()` | 3 |
| 2 | Cargo Company | Simple bidirectional | 1 |
| 3 | Branch Office / Stock | Simple bidirectional | 1 |
| 4 | Brand | Simple bidirectional | 1 |
| 5 | Sale / SaleItem | One rename | 1 |
| 6 | Product / Product Variant | One null-coalescing | 2 |
| 7 | User | One property rename | 1 |
| 8 | Trendyol Import | Nested source path, two Ignore(Id) | 0 (no direct call sites — called internally) |
| 9 | Brand Marketplace Match | One nav-property read, one hardcoded field | 2 |
| 10 | Category | Conditional null, two Ignore(nav) | 2 |
| 11 | Category Attribute | Complex nested write → factory method | 1 |
| 12 | Customer | Polymorphic, computed field | 4 |

### Phase 3: Remove Mapster

After all areas migrated and all tests green:

1. Delete `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs`
2. Remove `services.AddSingleton(config)` and `services.AddMapster()` from `ApplicationDependencyExtension.cs`
3. Remove Mapster `PackageReference` from `Entegrasyon.Business.csproj`, `Entegrasyon.ApplicationBootstrap.csproj`, `Entegrasyon.Blazor.csproj`
4. Build — any remaining `using Mapster;` or `using MapsterMapper;` become errors and guide final cleanup
5. Run full test suite

---

## 6. Mapping Conversion Reference

### 6.1 Simple flat copy (e.g., ApplicationSetting → ApplicationSettingDto)

```csharp
// Before (Mapster)
return settings.Adapt<List<ApplicationSettingDto>>();

// After (Mapperly — static mapper)
[Mapper]
public static partial class SettingMapper
{
    public static partial ApplicationSettingDto MapToDto(ApplicationSetting setting);
    public static partial List<ApplicationSettingDto> MapToDtoList(List<ApplicationSetting> settings);
}

// Call site
return SettingMapper.MapToDtoList(settings);
```

### 6.2 Property rename (SaleItem ↔ SaleItemDto)

```csharp
// Before (MappingConfig)
config.NewConfig<SaleItem, SaleItemDto>()
      .Map(dest => dest.DiscountVoucherCode, src => src.UsedDiscountVoucherCode);

// After (Mapperly)
[MapProperty(nameof(SaleItem.UsedDiscountVoucherCode), nameof(SaleItemDto.DiscountVoucherCode))]
public partial SaleItemDto MapToDto(SaleItem item);
```

### 6.3 Ignored navigation property (AddCategoryDto → Category)

```csharp
// Before
config.NewConfig<AddCategoryDto, Category>()
      .Ignore(dest => dest.CategoryAttributes)
      .Map(dest => dest.SuperCategoryId, src => src.SuperCategoryId == 0 ? (int?)null : src.SuperCategoryId);

// After
[MapperIgnoreTarget(nameof(Category.CategoryAttributes))]
[MapProperty(nameof(AddCategoryDto.SuperCategoryId), nameof(Category.SuperCategoryId), Use = nameof(ZeroToNull))]
public partial Category MapToCategory(AddCategoryDto dto);

private static int? ZeroToNull(int value) => value == 0 ? null : value;
```

### 6.4 Nested source path (TrendyolCategoryAttribute → CategoryAttribute)

```csharp
// Before
config.NewConfig<TrendyolCategoryAttribute, CategoryAttribute>()
      .Map(dest => dest.CategoryAttributeKey, src => src.Attribute.Name)
      .Map(dest => dest.CategoryAttributeHumanized, src => src.Attribute.Name);

// After
[MapProperty([nameof(TrendyolCategoryAttribute.Attribute), nameof(TrendyolAttribute.Name)],
             nameof(CategoryAttribute.CategoryAttributeKey))]
[MapProperty([nameof(TrendyolCategoryAttribute.Attribute), nameof(TrendyolAttribute.Name)],
             nameof(CategoryAttribute.CategoryAttributeHumanized))]
public partial CategoryAttribute MapFromTrendyol(TrendyolCategoryAttribute src);
```

### 6.5 Ignore entity Id on import (TrendyolBrand → Brand)

```csharp
// Before
config.NewConfig<TrendyolBrand, Brand>().Ignore(dest => dest.Id);

// After
[MapperIgnoreTarget(nameof(Brand.Id))]
public partial Brand MapFromTrendyol(TrendyolBrand src);
```

### 6.6 Null-coalescing value type (AddProductVariantDto → ProductVariant)

```csharp
// Before
config.NewConfig<AddProductVariantDto, ProductVariant>()
      .Map(dest => dest.VatRate, src => src.VatRate ?? 0m);

// After
// Option A: if ProductVariant.VatRate is decimal and AddProductVariantDto.VatRate is decimal?,
// Mapperly converts automatically with UseDefaultValueForNull = true at the config level.
// Option B: explicit converter
[MapProperty(nameof(AddProductVariantDto.VatRate), nameof(ProductVariant.VatRate), Use = nameof(NullableDecimalToDecimal))]
public partial ProductVariant MapToVariant(AddProductVariantDto dto);

private static decimal NullableDecimalToDecimal(decimal? value) => value ?? 0m;
```

### 6.7 Nav-property read (BrandMarketPlaceMatch → BrandMarketPlaceMatchDto)

```csharp
// Before
config.NewConfig<BrandMarketPlaceMatch, BrandMarketPlaceMatchDto>()
      .Map(dest => dest.ApplicationBrandName, src => src.ApplicationBrand.Name)
      .Map(dest => dest.MarketPlaceBrandName, src => "");

// After
[MapProperty([nameof(BrandMarketPlaceMatch.ApplicationBrand), nameof(Brand.Name)],
             nameof(BrandMarketPlaceMatchDto.ApplicationBrandName))]
[MapperIgnoreTarget(nameof(BrandMarketPlaceMatchDto.MarketPlaceBrandName))]
public partial BrandMarketPlaceMatchDto MapToDto(BrandMarketPlaceMatch src);

// Call site — BrandMatchService sets the ignored field after mapping:
var dto = mapper.MapToDto(match);
dto.MarketPlaceBrandName = string.Empty; // TODO: resolve from external API
```

### 6.8 Complex nested write — convert to factory method (EditCategoryAttributeDto → CategoryAttributeCategory)

```csharp
// Before (Mapster — writes into nested nav property, unusual)
config.NewConfig<EditCategoryAttributeDto, CategoryAttributeCategory>()
      .Map(dest => dest.CategoryAttributeId, src => src.Id)
      .Map(dest => dest.CategoryAttribute.Id, src => src.Id)
      // ... more nested writes

// After — no Mapperly mapping. Static factory method in CategoryAttributeCategory or mapper:
public static CategoryAttributeCategory FromEditDto(EditCategoryAttributeDto dto) => new()
{
    CategoryAttributeId = dto.Id,
    CategoryAttribute = new CategoryAttribute
    {
        Id = dto.Id,
        CategoryAttributeHumanized = dto.CategoryAttributeHumanized,
        CategoryAttributeValues = dto.CategoryAttributeValues,
        AllowCustom = dto.AllowCustom,
        CategoryAttributeKey = dto.CategoryAttributeKey
    }
};
```

### 6.9 Polymorphic source to single DTO (Customer hierarchy → CustomerDetailDto)

```csharp
// Before (three separate Mapster profiles map to same DTO target)

// After (Mapperly — two explicit methods, one per concrete subtype)
[MapProperty(nameof(RetailCustomer.NationalIdentity), nameof(CustomerDetailDto.NationalIdentityOrTaxNumber))]
[MapProperty(nameof(RetailCustomer.FullName), nameof(CustomerDetailDto.NameSurname))]
public partial CustomerDetailDto MapFromRetail(RetailCustomer src);

[MapProperty(nameof(CorporateCustomer.TaxNumber), nameof(CustomerDetailDto.NationalIdentityOrTaxNumber))]
[MapProperty(nameof(CorporateCustomer.FullName), nameof(CustomerDetailDto.NameSurname))]
public partial CustomerDetailDto MapFromCorporate(CorporateCustomer src);
```

Note: `SalesCount ← Sales.Count()` is a computed projection. Mapperly does not run LINQ projections. This property must be set post-mapping:

```csharp
var dto = mapper.MapFromRetail(customer);
dto.SalesCount = customer.Sales.Count(); // Mapperly ignores computed fields
```

Or, add a custom `[AfterMap]` partial method:

```csharp
partial void AfterMapFromRetail(RetailCustomer src, CustomerDetailDto dest)
{
    dest.SalesCount = src.Sales.Count();
}
```

---

## 7. Risk Assessment

### 7.1 Benefits that look like risks

**Compile-time errors for property mismatches** — Mapster silently leaves unmapped properties at their default values. Mapperly emits a build warning (configurable to error) for every unmapped property. This is strictly better, but it means migration may surface latent silent-mapping bugs. Each warning should be inspected rather than suppressed.

**DTO type changes become breaking** — With Mapster you rename a DTO property and get a silent null at runtime. With Mapperly you get a build error. This is the intended behaviour.

### 7.2 Actual risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Record types with init-only properties fail mapping | Low | Most mapped records in this codebase are positional or have parameterless constructors. Verify each record DTO before migrating. Mapperly supports `init` setters natively. |
| `CustomerDetailDto` has two constructors + `init` properties | Medium | Mapperly selects the constructor it can satisfy. The parameterless constructor exists (`public CustomerDetailDto()`) so Mapperly will use it and then set properties. Verify the generated output. |
| `ApplicationUser` has no public setters for some identity columns | Low | Only the mapped properties in `AddUserDto` need setters. Others are ignored. Check generated code. |
| `NpgsqlTsVector` properties on `RetailCustomer` / `CorporateCustomer` | Low | Mark them with `[MapperIgnoreSource]` in the mapper. They are never mapped. |
| Open-generic `Pageable<T>` mapping | Medium | Mapster supports open-generic config. Mapperly does not support open generics in the same way. Solution: check if `Pageable<T>` is actually mapped anywhere at call sites (no `mapper.Map<Pageable<T>>` calls found in grep). The profile may be vestigial — remove it and verify. |
| Test mocking: tests that mock `IMapper` | Low | Unit tests in `Entegrasyon.Test` that mock `IMapper` must be updated to mock the specific `XxxMapper` class or its extracted interface. Run tests after each phase to catch this immediately. |

### 7.3 Performance comparison

| Metric | Mapster | Mapperly |
|---|---|---|
| First-call overhead | ~5-50ms config warmup per type | Zero — static code |
| Per-mapping call overhead | Delegate lookup + invocation | Direct method call |
| Memory | Cached delegates + config graph | Zero per-mapping allocation |
| Startup (cold) | TypeAdapterConfig.GlobalSettings applied at DI registration | Zero |

For this codebase the performance gain is not the primary motivation — correctness and compile-time safety are. But it is a free bonus.

---

## 8. Test Strategy

The existing 97+ unit tests in `Entegrasyon.Test` provide coverage of manager methods that use mappings. They remain the primary regression guard. No new mapper-specific tests are required unless a mapper has custom logic (conversion methods, AfterMap hooks).

**For each migration phase:**
1. Run `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` — must stay green
2. Build the solution — zero warnings from Mapperly means all properties are mapped
3. Run integration tests after Phase 3: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`

**For custom mapper logic** (factory methods, AfterMap hooks): write a unit test in `Test/Entegrasyon.Test/Mappers/` covering the non-trivial conversions:
- `ZeroToNull(0)` returns null, `ZeroToNull(5)` returns 5
- `CategoryAttributeCategory.FromEditDto(dto)` populates nested object correctly
- `NullableDecimalToDecimal(null)` returns 0m

---

## 9. Files to Create / Modify

### New files
- `Application/Entegrasyon.Business/Mappers/UserMapper.cs`
- `Application/Entegrasyon.Business/Mappers/BrandMapper.cs`
- `Application/Entegrasyon.Business/Mappers/CargoCompanyMapper.cs`
- `Application/Entegrasyon.Business/Mappers/CustomerMapper.cs`
- `Application/Entegrasyon.Business/Mappers/ProductMapper.cs`
- `Application/Entegrasyon.Business/Mappers/CategoryMapper.cs`
- `Application/Entegrasyon.Business/Mappers/CategoryAttributeMapper.cs`
- `Application/Entegrasyon.Business/Mappers/SaleMapper.cs`
- `Application/Entegrasyon.Business/Mappers/SettingMapper.cs`
- `Application/Entegrasyon.Business/Mappers/TrendyolImportMapper.cs`
- `Test/Entegrasyon.Test/Mappers/MapperConversionTests.cs`

### Modified files
- `Application/Entegrasyon.Business/Entegrasyon.Business.csproj` — add Mapperly, remove Mapster (Phase 3)
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` — register mappers, remove AddMapster
- `Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj` — remove Mapster (Phase 3)
- `Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj` — remove Mapster (Phase 3)
- `Application/Entegrasyon.Business/Concrete/*.cs` — replace IMapper injection with typed mapper injection (all 9 files)
- `Application/Entegrasyon.Business/MapperProfiles/MappingConfig.cs` — progressively emptied, deleted in Phase 3

---

## 10. Open Questions

1. **`Pageable<T>` open-generic profile** — verify whether any manager actually calls `mapper.Map<Pageable<T>>`. If not, the profile is dead code and should simply be deleted rather than migrated.

2. **`ProductsDetailDto` from `Product`** — `ProductsDetailDto` has properties like `BrandName`, `CategoryName`, `TotalQuantity`, `TotalSoldQuantity`, `VariantCount` that are clearly aggregated from navigation properties (not flat columns on `Product`). How is Mapster currently populating these? This mapping likely fails silently and sets all properties to defaults. Investigate `ProductManager.GetProductDetailAsync` before migrating.

3. **`UserEditDto → ApplicationUser`** — `UserEditDto` definition was not found in the file search. Verify it exists and is used, or remove it as dead code.

4. **`IMapper` in test mocks** — check whether any unit test creates `Mock<IMapper>` and sets up specific `.Map<T>()` expectations. These must be refactored when `IMapper` is removed from the corresponding manager.
