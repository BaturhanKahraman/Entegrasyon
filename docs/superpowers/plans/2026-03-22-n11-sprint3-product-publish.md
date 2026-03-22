# N11 Product Publish (Sprint 3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** N11 pazaryerine urun publish, silme, guncelleme ve satis baslat/durdur islemlerini gercek SOAP cagrilariyla implement etmek.

**Architecture:** `N11MappingValidator` → `N11ProductMapper` → `N11ProductService` pipeline'i. Mapper XElement (SOAP XML) uretir, service SOAP cagrisini yapar ve ProductMarketplace gunceller. Trendyol pattern'inin N11 SOAP adaptasyonu.

**Tech Stack:** .NET 8, System.Xml.Linq, EF Core, xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-22-n11-product-publish-design.md`

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Application/Entegrasyon.Business/Abstract/IN11ProductService.cs` | 3 yeni metod ekle |
| Create | `Application/Entegrasyon.Business/Abstract/IN11ProductMapper.cs` | Mapper interface |
| Modify | `Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs` | 3 yeni mock metod |
| Create | `Application/Entegrasyon.Business/Concrete/N11/N11MappingValidator.cs` | Publish oncesi validasyon |
| Create | `Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs` | Product → XML mapping |
| Create | `Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs` | Gercek SOAP service |
| Modify | `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | DI kaydi |
| Create | `Test/Entegrasyon.Test/N11/N11MappingValidatorTests.cs` | Validator testleri |
| Create | `Test/Entegrasyon.Test/N11/N11ProductMapperTests.cs` | Mapper testleri |
| Create | `Test/Entegrasyon.Test/N11/N11ProductServiceTests.cs` | Service testleri |

---

## Task 1: Interface Genisletme + Mock Guncelleme

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IN11ProductService.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IN11ProductMapper.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs`

- [ ] **Step 1: Extend IN11ProductService with 3 new methods**

```csharp
// Application/Entegrasyon.Business/Abstract/IN11ProductService.cs
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IN11ProductService
{
    Task<IDataResult<long>> SaveProductAsync(Guid productId);
    Task<IResult> DeleteProductAsync(Guid productId);
    Task<IResult> UpdateProductBasicAsync(Guid productId);
    Task<IResult> StartSellingAsync(Guid productId);
    Task<IResult> StopSellingAsync(Guid productId);
}
```

- [ ] **Step 2: Create IN11ProductMapper interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IN11ProductMapper.cs
using System.Xml.Linq;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 ürün mapper — ürün verisini N11 SaveProduct XML formatına dönüştürür.
/// </summary>
public interface IN11ProductMapper
{
    Task<IDataResult<XElement>> MapProductAsync(Guid productId);
}
```

- [ ] **Step 3: Update MockN11ProductService with new methods**

Add to MockN11ProductService:

```csharp
public Task<IResult> UpdateProductBasicAsync(Guid productId)
{
    logger.LogInformation("Mock: N11 UpdateProductBasic — ProductId={ProductId}", productId);
    return Task.FromResult<IResult>(new SuccessResult("Ürün güncellendi (mock)."));
}

public Task<IResult> StartSellingAsync(Guid productId)
{
    logger.LogInformation("Mock: N11 StartSelling — ProductId={ProductId}", productId);
    return Task.FromResult<IResult>(new SuccessResult("Satış başlatıldı (mock)."));
}

public Task<IResult> StopSellingAsync(Guid productId)
{
    logger.LogInformation("Mock: N11 StopSelling — ProductId={ProductId}", productId);
    return Task.FromResult<IResult>(new SuccessResult("Satış durduruldu (mock)."));
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IN11ProductService.cs \
      Application/Entegrasyon.Business/Abstract/IN11ProductMapper.cs \
      Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs
git commit -m "feat(n11): extend IN11ProductService interface and add IN11ProductMapper"
```

---

## Task 2: N11MappingValidator

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/N11MappingValidator.cs`
- Create: `Test/Entegrasyon.Test/N11/N11MappingValidatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/N11/N11MappingValidatorTests.cs
// Tests: extend BaseTest, mock DbContext sets
// 1. ValidateAsync_WhenAllMappingsExist_ShouldReturnSuccess
// 2. ValidateAsync_WhenCategoryMatchMissing_ShouldReturnError
// 3. ValidateAsync_WhenBrandMatchMissing_ShouldReturnError
// 4. ValidateAsync_WhenRequiredAttributeMissing_ShouldReturnError
// 5. ValidateAsync_WhenBrandIdNull_ShouldReturnSuccess (N11-specific: brand not required)

// Use ReturnsDbSet() from Moq.EntityFrameworkCore to mock:
// - MainProducts, CategoryMarketplaces (NOT CategoryMarketPlaceMatches — use new table),
//   BrandMarketPlaceMatches, CategoryAttributeCategories, CategoryAttributeMarketPlaceMatches
```

- [ ] **Step 2: Run tests — verify FAIL**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11MappingValidatorTests" -v m`

- [ ] **Step 3: Implement N11MappingValidator**

Follow `TrendyolMappingValidator` pattern (at `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMappingValidator.cs`):
- Primary constructor DI: `IDbContextFactory<IntegrationDbContext>`
- Use `N11MarketPlaceId` (=2) instead of `TrendyolMarketPlaceId`
- **Key difference from Trendyol:** BrandId null is NOT an error (N11 brand is optional)
- Use `CategoryMarketplaces` table (new) OR `CategoryMarketPlaceMatches` (old) — check which one the codebase uses for N11 (Sprint 2 used `CategoryMarketplace` new table). Validator should check whichever table has N11 data.
- Error messages: "N11" instead of "Trendyol"

- [ ] **Step 4: Run tests — verify PASS**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/N11MappingValidator.cs \
      Test/Entegrasyon.Test/N11/N11MappingValidatorTests.cs
git commit -m "feat(n11): implement N11MappingValidator for pre-publish validation"
```

---

## Task 3: N11ProductMapper

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs`
- Create: `Test/Entegrasyon.Test/N11/N11ProductMapperTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/N11/N11ProductMapperTests.cs
// Tests: extend BaseTest
// 1. MapProductAsync_ShouldReturnValidXml — verify root element is <product>, has required children
// 2. MapProductAsync_ShouldMapVariantsAsStockItems — mock 2 variants, verify 2 <stockItem> elements
// 3. MapProductAsync_ShouldResolveOverrides — mock ProductMarketplace overrides, verify title/price
// 4. MapProductAsync_WhenProductNotFound_ShouldReturnError
// 5. MapProductAsync_ShouldMapProductLevelAttributes — verify <product><attributes> element
// 6. MapProductAsync_ShouldMapVariantLevelAttributes — verify <stockItem><attributes> element
// 7. MapProductAsync_ShouldOrderImages — verify <image><order> is 1-based
// 8. MapProductAsync_CurrencyType_ShouldBe1 — verify <currencyType>1</currencyType>

// Mock requirements:
// - MainProducts with Variants, Images, AttributeKeyValues
// - ProductMarketplaces with overrides
// - CategoryMarketplaces (MarketPlaceId=2) for N11 category ID
// - CategoryAttributeMarketPlaceMatches for attribute name resolution
// - CategoryAttributeValueMarketPlaceMatches for value resolution
// - MarketPlaceWarehouses or BranchOffices with IsDefaultMarketPlaceStock
// - IMinioFileStorage.GetPublicUrl() mock
```

- [ ] **Step 2: Run tests — verify FAIL**

- [ ] **Step 3: Implement N11ProductMapper**

Follow `TrendyolProductMapper` pattern but output XElement instead of DTO:

```csharp
// Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs
// Primary constructor DI: IDbContextFactory, IMinioFileStorage, ILogger
// Implements IN11ProductMapper

// Key differences from Trendyol:
// 1. Output is XElement (<product> element), not TrendyolCreateProductRequest DTO
// 2. Attributes are name-value string pairs, not integer IDs
// 3. Variants are nested <stockItems> inside single product, not separate items
// 4. <currencyType>1</currencyType> (TL)
// 5. Two-level attributes:
//    - Product-level: <product><attributes><attribute><name>...<value>...
//    - Variant-level: <stockItem><attributes><attribute><name>...<value>...
// 6. Images have <order> field (1-based)

// Data fetch (same as Trendyol):
// - Product + Variants + Images + AttributeKeyValues
// - ProductMarketplace (MarketPlaceId=2) for overrides
// - CategoryMarketplace (MarketPlaceId=2) for N11 category ID
// - All marketplace match dictionaries (MarketPlaceId=2)
// - MarketPlaceWarehouses for stock calculation

// Override resolution:
// - Title: ProductMarketplace.TitleOverride ?? product.Title
// - Description: ProductMarketplace.DescriptionOverride ?? product.Description
// - Price: ProductVariantMarketplaceOverride.SalePriceOverride ?? variant.SalePrice
```

- [ ] **Step 4: Run tests — verify PASS**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs \
      Test/Entegrasyon.Test/N11/N11ProductMapperTests.cs
git commit -m "feat(n11): implement N11ProductMapper for product-to-XML mapping"
```

---

## Task 4: N11ProductService (Gercek Implementasyon)

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs`
- Create: `Test/Entegrasyon.Test/N11/N11ProductServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/N11/N11ProductServiceTests.cs
// Tests: extend BaseTest, mock IN11SoapClient, IN11ProductMapper, N11MappingValidator,
//        IProductActivityLogger, mockContextFactory
//
// 1. SaveProductAsync_ShouldCallValidatorMapperAndSoap
//    — verify validator called, mapper called, soapClient.SendAsync("ProductService",...) called
// 2. SaveProductAsync_ShouldUpdateProductMarketplace
//    — mock successful response with <product><id>12345</id>, verify PM.ExternalProductId = "12345"
// 3. SaveProductAsync_WhenResponseFailure_ShouldReturnError
//    — mock response with <result><status>failure</status><errorMessage>Hata</errorMessage>,
//      verify ErrorResult returned
// 4. DeleteProductAsync_ShouldCallSoapWithExternalId
//    — mock PM with ExternalProductId, verify SOAP call includes the ID
// 5. UpdateProductBasicAsync_ShouldCallSoapWithLimitedFields
//    — verify SOAP body does NOT contain full mapper output, uses lighter mapping
// 6. StartSellingAsync_ShouldUseSoapProductSellingService
//    — verify soapClient.SendAsync("ProductSellingService", ...) called (NOT "ProductService")
// 7. StopSellingAsync_ShouldUseSoapProductSellingService
//    — same as above

// Mock SOAP response helper:
// Return XElement with <result><status>success</status></result><product><id>12345</id>...</product>
```

- [ ] **Step 2: Run tests — verify FAIL**

- [ ] **Step 3: Implement N11ProductService**

```csharp
// Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs
// Primary constructor DI: IN11SoapClient, IN11ProductMapper, N11MappingValidator,
//                        IProductActivityLogger, IDbContextFactory, ILogger
// Implements IN11ProductService

// WSDL routing:
// - SaveProduct, Delete, UpdateBasic → "ProductService"
// - StartSelling, StopSelling → "ProductSellingService"

// SOAP namespace: http://www.n11.com/ws/schemas

// Response status check helper:
// private static IResult CheckN11ResponseStatus(XElement response)
// {
//     var status = response.Element("result")?.Element("status")?.Value;
//     if (status == "failure")
//         return new ErrorResult(response.Element("result")?.Element("errorMessage")?.Value ?? "N11 hatasi");
//     return new SuccessResult();
// }

// SaveProductAsync flow:
// 1. validator.ValidateProductMappingsAsync → activity log
// 2. mapper.MapProductAsync → XElement (<product>)
// 3. Wrap in SaveProductRequest: new XElement(ns + "SaveProductRequest", productElement)
// 4. soapClient.SendAsync("ProductService", "", request)
// 5. CheckN11ResponseStatus(response)
// 6. Parse: response.Element("product")?.Element("id")?.Value → long
// 7. Update ProductMarketplace: ExternalProductId, Status=Published
// 8. Activity log: PublishSent, Success

// DeleteProductAsync:
// 1. Fetch PM.ExternalProductId (MarketPlaceId=2)
// 2. Build DeleteProductByIdRequest with <productId>
// 3. SendAsync("ProductService", "", request)
// 4. CheckN11ResponseStatus → PM.Status = Pending

// UpdateProductBasicAsync:
// 1. Fetch product + variants + PM (for ExternalProductId)
// 2. Build UpdateProductBasicRequest with <productId>, <price>, <stockItems>, <description>, <images>
//    (NO full mapper — lightweight inline mapping)
// 3. SendAsync("ProductService", "", request)

// StartSellingAsync:
// 1. Fetch PM.ExternalProductId
// 2. Build StartSellingProductByProductIdRequest with <productId>
// 3. SendAsync("ProductSellingService", "", request)  ← DIFFERENT WSDL

// StopSellingAsync: same but StopSellingProductByProductIdRequest
```

- [ ] **Step 4: Run tests — verify PASS**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs \
      Test/Entegrasyon.Test/N11/N11ProductServiceTests.cs
git commit -m "feat(n11): implement N11ProductService with SaveProduct, Delete, Update, Start/StopSelling"
```

---

## Task 5: DI Registration + N11:UseMock Config

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add N11 product service registrations**

After the existing N11 mock registrations, replace with:

```csharp
// N11 ürün servisleri
services.AddScoped<N11MappingValidator>();
services.AddScoped<IN11ProductMapper, N11ProductMapper>();

var useN11Mock = configuration.GetValue<bool>("N11:UseMock", true);
if (useN11Mock)
{
    services.AddScoped<IN11ProductService, MockN11ProductService>();
}
else
{
    services.AddScoped<IN11ProductService, N11ProductService>();
}
```

Remove the old unconditional mock registration:
```csharp
// REMOVE: services.AddScoped<IN11ProductService, MockN11ProductService>();
```

- [ ] **Step 2: Verify build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 3: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v m`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(n11): register N11 product services with UseMock config switch"
```

---

## Task 6: Full Test Suite + Build Verification

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v m`
Expected: All tests pass

- [ ] **Step 2: Run full solution build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors
