# Product Edit Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/products/edit/{Id:guid}` rotasında sekme tabanlı tek sayfalık ürün düzenleme deneyimi ekle.

**Architecture:** Tek Kaydet butonu ile tüm sekmeleri (Genel / Varyantlar / Görseller / Özellikler) bir arada sunan MudTabs layout. Business layer 3 adımlı pipeline (Validation → Business Rules → Execution), EF Core tracked load ile atomic güncelleme. Stok ve marketplace sync bu sayfanın dışında.

**Tech Stack:** .NET 8, Blazor Server, MudBlazor, EF Core (PostgreSQL/Npgsql), FluentValidation, Mapster, EventChannel pattern.

---

## File Structure

**CREATE:**
```
Application/Entegrasyon.Entity/Dtos/Product/EditProductDto.cs
Application/Entegrasyon.Entity/Dtos/Product/EditProductVariantDto.cs
Application/Entegrasyon.Entity/Dtos/Product/ProductEditPageDto.cs
Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncStatusDto.cs
Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs
Application/Entegrasyon.Entity/Dtos/Branches/BranchSelectDto.cs
Application/Entegrasyon.Business/Channels/Events/Products/ProductUpdatedEvent.cs
Application/Entegrasyon.Business/Validation/FluentValidation/EditProductValidator.cs
Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor
Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor.cs
Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor
Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor.cs
Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor
Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor.cs
Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor
Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor.cs
Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor
Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor.cs
Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs
Test/Entegrasyon.Test/Business/UpdateProductTests.cs
```

**MODIFY:**
```
Application/Entegrasyon.Entity/Dtos/Product/ProductEditDetailDto.cs          — Season, Year eklenir
Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/ProductVariantEditDetailDto.cs  — ECommercePrice eklenir
Application/Entegrasyon.Business/Abstract/IProductManager.cs                 — yeni metodlar, eski kaldırılır
Application/Entegrasyon.Business/Concrete/ProductManager.cs                  — primary ctor, yeni metodlar
Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs — EditProductValidator kaydı
Application/Entegrasyon.Business/Channels/ChannelExtensions.cs               — ProductUpdatedEvent channel
```

---

## Chunk 1: Entity, DTO ve Event Katmanı

### Task 1: Mevcut DTO'lara eksik alan ekle (breaking change)

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductEditDetailDto.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/ProductVariantEditDetailDto.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs:139-163` (LINQ projection)

- [ ] **Step 1: ProductEditDetailDto'ya Season ve Year ekle**

`Application/Entegrasyon.Entity/Dtos/Product/ProductEditDetailDto.cs` dosyasını tam olarak şu içerikle değiştir:

```csharp
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductEditDetailDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    string Season,
    string Year,
    int BrandId,
    int CategoryId,
    List<ProductVariantEditDetailDto> ProductVariants,
    List<AttributeKeyValueDto> AttributeKeyValues
);
```

- [ ] **Step 1b: EditableImageDto'daki IsCoverImage parametresini IsMain olarak yeniden adlandır**

`Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/EditableImageDto.cs` dosyasını şu içerikle değiştir:

```csharp
namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record EditableImageDto(int Id, string Src, bool IsMain, bool IsDeleted);
```

> `Image` entity'sinde `IsCoverImage` `[Obsolete]` — aktif alan `IsMain`. DTO parametresi bu gerçeği yansıtmalı. Derleme sırasında mevcut `IsCoverImage` referanslarını `IsMain` olarak güncelle.

- [ ] **Step 2: ProductVariantEditDetailDto'ya ECommercePrice ekle**

`Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/ProductVariantEditDetailDto.cs` dosyasını tam olarak şu içerikle değiştir:

```csharp
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record ProductVariantEditDetailDto(
    Guid Id,
    decimal DimensionalWeight,
    string CurrencyType,
    string Barcode,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal VatRate,
    List<EditBranchOfficeStockDto> BranchOfficeStocks,
    List<EditableImageDto> UploadedImages,
    List<VariantAttributeDto> VariantAttributes
);
```

- [ ] **Step 3: ProductManager'daki LINQ projection'ı güncelle**

`ProductManager.cs`'teki `GetProductEditDetailById` metodunda DTO constructor çağrısını güncelle — hem yeni alanları ekle, hem NRE bug'ını düzelt:

```csharp
public async Task<IDataResult<ProductEditDetailDto>> GetProductEditDetailById(Guid id)
{
    var result = await _dbContext.MainProducts
        .Where(p => p.Id == id)
        .Select(p => new ProductEditDetailDto(
            p.Id, p.Title, p.Description, p.StockCode,
            p.Season, p.Year,          // ← YENİ
            p.BrandId!.Value, p.CategoryId,
            p.ProductVariants.Select(pv => new ProductVariantEditDetailDto(
                pv.Id, pv.DimensionalWeight, pv.CurrencyType, pv.Barcode,
                pv.ListPrice, pv.SalePrice, pv.CostPrice,
                pv.ECommercePrice,     // ← YENİ
                pv.VatRate,
                pv.BranchOfficeStocks.Select(bos => new EditBranchOfficeStockDto(bos.BranchOfficeId, bos.FirstTotalStock)).ToList(),
                pv.Images.Select(img => new EditableImageDto(img.Id, img.Src, img.IsMain, img.IsDeleted)).ToList(),
                pv.ProductVariantAttributes
                    .Select(pva => new VariantAttributeDto(pva.CategoryAttributeValueId, pva.CategoryAttributeValue, pva.CustomValue, pva.IsVarianter, pva.IsSlicer))
                    .ToList()
            )).ToList(),
            p.AttributeKeyValues.Select(akv => new AttributeKeyValueDto(
                akv.CategoryAttributeId,
                akv.CategoryAttribute.CategoryAttributeKey,
                akv.AttributeValueId,
                akv.AttributeValue.Name,
                akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId)?.IsRequired ?? false,  // ← NRE FIX
                akv.CustomValue)
            ).ToList()))
        .FirstOrDefaultAsync();
    return new SuccessDataResult<ProductEditDetailDto>(result);
}
```

- [ ] **Step 4: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded` — DTO constructor'larını çağıran başka yer varsa (test dahil) bu adımda hata alırsın; her hatayı düzelt ve tekrar build al.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/ProductEditDetailDto.cs \
        Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/ProductVariantEditDetailDto.cs \
        Application/Entegrasyon.Business/Concrete/ProductManager.cs
git commit -m "fix: Add Season/Year/ECommercePrice to edit DTOs, fix NRE in LINQ projection"
```

---

### Task 2: Write DTOlar — EditProductDto, EditProductVariantDto

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Product/EditProductDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Product/EditProductVariantDto.cs`

- [ ] **Step 1: EditProductDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Product/EditProductDto.cs
using Entegrasyon.Entity.Dtos.Attributes;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record EditProductDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    string Season,
    string Year,
    int BrandId,
    int CategoryId,
    List<EditProductVariantDto> Variants,
    List<AttributeKeyValueDto> AttributeKeyValues,
    List<int> DeletedImageIds
);
```

- [ ] **Step 2: EditProductVariantDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Product/EditProductVariantDto.cs
namespace Entegrasyon.Entity.Dtos.Product;

public sealed record EditProductVariantDto(
    Guid Id,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal DimensionalWeight,
    decimal VatRate,
    string CurrencyType
);
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/EditProductDto.cs \
        Application/Entegrasyon.Entity/Dtos/Product/EditProductVariantDto.cs
git commit -m "feat: Add EditProductDto and EditProductVariantDto write models"
```

---

### Task 3: Read DTOlar — ProductEditPageDto, kategori/şube minimal DTOlar, sync status

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Product/ProductEditPageDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncStatusDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Branches/BranchSelectDto.cs`

- [ ] **Step 1: MarketplaceSyncStatusDto ve enum oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncStatusDto.cs
namespace Entegrasyon.Entity.Dtos.Product;

public enum MarketplaceSyncState
{
    NeverSynced,   // ProductMarketplace kaydı yok
    Waiting,       // Status=Pending, BatchRequestId=null
    Processing,    // Status=Pending, BatchRequestId!=null
    OutOfSync,     // Status=Published, UpdatedAt > LastSyncedAt
    Synced,        // Status=Published, UpdatedAt <= LastSyncedAt
    Failed,
    Rejected
}

public sealed record MarketplaceSyncStatusDto(
    MarketplaceSyncState State,
    DateTimeOffset? LastSyncedAt,
    string? BatchRequestId,
    string? StatusMessage
);
```

- [ ] **Step 2: CategorySelectDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs
namespace Entegrasyon.Entity.Dtos.Category;

public sealed record CategorySelectDto(int Id, string Name);
```

- [ ] **Step 3: BranchSelectDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Branches/BranchSelectDto.cs
namespace Entegrasyon.Entity.Dtos.Branches;

public sealed record BranchSelectDto(int Id, string Name);
```

- [ ] **Step 4: ProductEditPageDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Product/ProductEditPageDto.cs
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductEditPageDto(
    ProductEditDetailDto Product,
    List<BrandListDetailDto> Brands,
    List<CategorySelectDto> LeafCategories,
    List<BranchSelectDto> BranchOffices,
    MarketplaceSyncStatusDto SyncStatus
);
```

- [ ] **Step 5: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/ProductEditPageDto.cs \
        Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncStatusDto.cs \
        Application/Entegrasyon.Entity/Dtos/Category/CategorySelectDto.cs \
        Application/Entegrasyon.Entity/Dtos/Branches/BranchSelectDto.cs
git commit -m "feat: Add ProductEditPageDto, MarketplaceSyncStatusDto, CategorySelectDto, BranchSelectDto"
```

---

### Task 4: ProductUpdatedEvent oluştur

**Files:**
- Create: `Application/Entegrasyon.Business/Channels/Events/Products/ProductUpdatedEvent.cs`
- Modify: `Application/Entegrasyon.Business/Channels/ChannelExtensions.cs`

- [ ] **Step 1: ProductUpdatedEvent sınıfını oluştur**

```csharp
// Application/Entegrasyon.Business/Channels/Events/Products/ProductUpdatedEvent.cs
namespace Entegrasyon.Business.Channels.Events.Products;

public class ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public bool CategoryChanged { get; set; }

    public ProductUpdatedEvent() { }

    public ProductUpdatedEvent(Guid productId, string productTitle, bool categoryChanged)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        CategoryChanged = categoryChanged;
    }
}
```

- [ ] **Step 2: ChannelExtensions.cs'e kayıt ekle**

`Application/Entegrasyon.Business/Channels/ChannelExtensions.cs` dosyasını aç ve `AddEventChannels` metoduna şu satırı ekle:

```csharp
services.AddSingleton<EventChannel<ProductUpdatedEvent>>();
```

Dosyanın tamamı şu hale gelmeli:

```csharp
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Channels.Events.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.Channels;

public static class ChannelExtensions
{
    public static IServiceCollection AddEventChannels(this IServiceCollection services)
    {
        services.AddSingleton<EventChannel<ProductCreatedForMarketplaceEvent>>();
        services.AddSingleton<EventChannel<ProductAddedEvent>>();
        services.AddSingleton<EventChannel<ProductUpdatedEvent>>();   // ← YENİ
        services.AddSingleton<EventChannel<CategoryUpdatedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportRequestedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportCompletedEvent>>();
        services.AddSingleton<EventChannel<NotificationEvent>>();
        return services;
    }
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Channels/Events/Products/ProductUpdatedEvent.cs \
        Application/Entegrasyon.Business/Channels/ChannelExtensions.cs
git commit -m "feat: Add ProductUpdatedEvent and register channel"
```

---

## Chunk 2: Business Layer

### Task 5: EditProductValidator — önce test, sonra implementasyon

**Files:**
- Create: `Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs`
- Create: `Application/Entegrasyon.Business/Validation/FluentValidation/EditProductValidator.cs`
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs`

- [ ] **Step 1: Failing testleri yaz**

```csharp
// Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation.TestHelper;

namespace Entegrasyon.UnitTest.ValidationRules;

public class EditProductValidatorTests
{
    private readonly EditProductValidator _validator = new();

    private static EditProductDto BuildValid() => new(
        Id: Guid.NewGuid(),
        Title: "Test Ürün",
        Description: "Açıklama",
        StockCode: "TST-001",
        Season: "İlkbahar",
        Year: "2026",
        BrandId: 1,
        CategoryId: 1,
        Variants: [new EditProductVariantDto(Guid.NewGuid(), 100, 90, 70, 80, 0.5m, 20, "TRY")],
        AttributeKeyValues: [],
        DeletedImageIds: []
    );

    [Fact]
    public async Task Title_Empty_ShouldFail()
    {
        var dto = BuildValid() with { Title = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task BrandId_Zero_ShouldFail()
    {
        var dto = BuildValid() with { BrandId = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.BrandId);
    }

    [Fact]
    public async Task CategoryId_Zero_ShouldFail()
    {
        var dto = BuildValid() with { CategoryId = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task StockCode_Empty_ShouldFail()
    {
        var dto = BuildValid() with { StockCode = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.StockCode);
    }

    [Fact]
    public async Task Variant_NegativeListPrice_ShouldFail()
    {
        var dto = BuildValid() with
        {
            Variants = [new EditProductVariantDto(Guid.NewGuid(), -1, 90, 70, 80, 0.5m, 20, "TRY")]
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public async Task ValidDto_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(BuildValid());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 2: Testleri çalıştır — başarısız olduklarını doğrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~EditProductValidatorTests" -v minimal
```

Beklenen: Build hatası — `EditProductValidator` henüz yok.

- [ ] **Step 3: EditProductValidator'ı implement et**

```csharp
// Application/Entegrasyon.Business/Validation/FluentValidation/EditProductValidator.cs
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditProductValidator : AbstractValidator<EditProductDto>
{
    public EditProductValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Ürün adı boş geçilemez");

        RuleFor(x => x.StockCode)
            .NotEmpty().WithMessage("Stok kodu boş geçilemez");

        RuleFor(x => x.BrandId)
            .GreaterThan(0).WithMessage("Ürün markası seçilmelidir");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Ürün kategorisi seçilmelidir");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.ListPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Liste fiyatı 0'dan küçük olamaz");
            v.RuleFor(x => x.SalePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı 0'dan küçük olamaz");
            v.RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Maliyet fiyatı 0'dan küçük olamaz");
        });
    }
}
```

- [ ] **Step 4: ServiceDependencyExtension'a kayıt ekle**

`ServiceDependencyExtension.cs`'teki `AddValidators` metoduna şu satırı ekle (`AddProductValidator` kaydının hemen altına):

```csharp
services.AddScoped<IValidator<EditProductDto>, EditProductValidator>();
```

- [ ] **Step 5: Testleri çalıştır — geçtiklerini doğrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~EditProductValidatorTests" -v minimal
```

Beklenen: `6 passed`

- [ ] **Step 6: Commit**

```bash
git add Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs \
        Application/Entegrasyon.Business/Validation/FluentValidation/EditProductValidator.cs \
        Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs
git commit -m "feat: Add EditProductValidator with tests and DI registration"
```

---

### Task 6: IProductService — interface güncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IProductManager.cs`

- [ ] **Step 1: Interface'i güncelle**

`Application/Entegrasyon.Business/Abstract/IProductManager.cs` dosyasını tam olarak şu içerikle değiştir:

```csharp
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos;

namespace Entegrasyon.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<Product>> AddProduct(AddProductDto dto);
    Task<IResult> GetProductByBarcode(string barcode);

    // Edit
    Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id);
    Task<IResult> UpdateProduct(EditProductDto dto);

    // Other
    Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId);
    Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto);
    Task<int> GetProductCountByCategoryId(int categoryId);
    Task<bool> HasSoldProductsInCategory(int categoryId);
}
```

> Not: `GetProductEditDetailById(Guid id)` kaldırıldı — consumer olup olmadığını kontrol et: `grep -r "GetProductEditDetailById" --include="*.cs" .`
> Eğer başka consumer varsa önce orası güncellenmeli. Test dosyaları dahil tüm referansları kaldır.

- [ ] **Step 2: GetProductEditDetailById consumer'larını grep et ve kaldır**

```bash
grep -r "GetProductEditDetailById" --include="*.cs" .
```

Beklenen: Yalnızca `ProductManager.cs` ve `IProductManager.cs`'te görünmeli. Başka consumer varsa önce onları güncelle.

- [ ] **Step 2b: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`. C# orphaned interface olmayan metodun varlığına hata vermez — `ProductManager.cs`'teki `GetProductEditDetailById` artık orphaned dead code. Task 7'de ProductManager tamamen yeniden yazılırken bu metot kaldırılacak.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IProductManager.cs
git commit -m "refactor: Update IProductService — replace GetProductEditDetailById with GetProductEditPageData"
```

---

### Task 7: ProductManager — GetProductEditPageData ve UpdateProduct implementasyonu

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`
- Create: `Test/Entegrasyon.Test/Business/UpdateProductTests.cs`

Bu task en kritik task. EF Core tracked load + atomic save burada.

- [ ] **Step 1: Failing UpdateProduct testlerini yaz**

```csharp
// Test/Entegrasyon.Test/Business/UpdateProductTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using FluentValidation;
using MapsterMapper;

namespace Entegrasyon.UnitTest.Business;

public class UpdateProductTests : BaseTest
{
    private readonly IProductService _productManager;
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager = new();
    private readonly Mock<IAttributeKeyValueManager> _mockAttributeKeyValueManager = new();
    private readonly Mock<IBarcodeService> _mockBarcodeService = new();
    private readonly EventChannel<ProductAddedEvent> _productAddedChannel = new();
    private readonly EventChannel<ProductUpdatedEvent> _productUpdatedChannel = new();

    public UpdateProductTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<EditProductDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _productManager = new ProductManager(
            mockIntegrationDbContext.Object,
            mockApplicationLogger.Object,
            _mockMapper.Object,
            MockValidator.Object,
            _mockOfficeStockManager.Object,
            _mockAttributeKeyValueManager.Object,
            _mockBarcodeService.Object,
            _productAddedChannel,
            _productUpdatedChannel
        );
    }

    private static EditProductDto BuildValidEditDto(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Title: "Güncel Ürün",
        Description: "Açıklama",
        StockCode: "TST-001",
        Season: "Yaz",
        Year: "2026",
        BrandId: 1,
        CategoryId: 1,
        Variants: [],
        AttributeKeyValues: [],
        DeletedImageIds: []
    );

    [Fact]
    public async Task UpdateProduct_ValidationFails_ReturnsError()
    {
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<EditProductDto>()))
            .ThrowsAsync(new ValidationException("Validation failed"));

        var result = await _productManager.UpdateProduct(BuildValidEditDto());

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProduct_StockCodeAlreadyExists_ReturnsError()
    {
        var existingId = Guid.NewGuid();
        var conflictProduct = new Product { Id = Guid.NewGuid(), StockCode = "TST-001" };

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([conflictProduct]);

        var dto = BuildValidEditDto(existingId); // dto.Id != conflictProduct.Id → conflict

        var result = await _productManager.UpdateProduct(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("stok kodu");
    }
}
```

- [ ] **Step 2: Testleri çalıştır — başarısız olduklarını doğrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~UpdateProductTests" -v minimal
```

Beklenen: Compile hatası — `ProductManager` henüz `EventChannel<ProductUpdatedEvent>` almıyor.

- [ ] **Step 3: ProductManager'ı primary constructor'a geçir ve yeni metodları ekle**

`Application/Entegrasyon.Business/Concrete/ProductManager.cs` dosyasının tam içeriği:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos;

namespace Entegrasyon.Business.Concrete;

public class ProductManager(
    IntegrationDbContext dbContext,
    IApplicationLogManager applicationLogManager,
    IMapper mapper,
    IFluentValidator validator,
    IOfficeStockManager officeStockManager,
    IAttributeKeyValueManager attributeKeyValueManager,
    IBarcodeService barcodeService,
    EventChannel<ProductAddedEvent> productAddedChannel,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel) : IProductService
{
    public async Task<IDataResult<Product>> AddProduct(AddProductDto dto)
    {
        await applicationLogManager.AddLog("Ürün ekleme isteği geldi.", LogType.Product, LogAction.Add, dto);
        await validator.ValidateAndThrowAsync(dto);
        var check = LogicRunner.Run(
            officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
        );
        if (check != null)
            return new ErrorDataResult<Product>(null!, check.Message);
        foreach (var productVariantDto in dto.ProductVariants.Where(pv => string.IsNullOrEmpty(pv.Barcode)))
            productVariantDto.Barcode = await barcodeService.GenerateAsync();
        var product = mapper.Map<Product>(dto);
        attributeKeyValueManager.ClearEmptyAttributes(product);
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ürün başarı ile eklendi", LogType.Product, LogAction.Add);
        productAddedChannel.TryPublish(new ProductAddedEvent(product.Id, product.Title));
        return new SuccessDataResult<Product>(product, Messages.ProductAdded);
    }

    public async Task<IResult> GetProductByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return new ErrorResult("Barkod boş olamaz.");
        var product = await dbContext.MainProducts
            .FirstOrDefaultAsync(x => x.ProductVariants.Any(pv => pv.Barcode == barcode));
        if (product == null)
            return new ErrorResult("Barkoda ait ürün bulunamadı.");
        return new SuccessDataResult<ProductsDetailDto>(mapper.Map<ProductsDetailDto>(product));
    }

    public async Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id)
    {
        var product = await dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => new ProductEditDetailDto(
                p.Id, p.Title, p.Description, p.StockCode,
                p.Season, p.Year,
                p.BrandId!.Value, p.CategoryId,
                p.ProductVariants.Select(pv => new ProductVariantEditDetailDto(
                    pv.Id, pv.DimensionalWeight, pv.CurrencyType, pv.Barcode,
                    pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
                    pv.BranchOfficeStocks.Select(bos => new EditBranchOfficeStockDto(bos.BranchOfficeId, bos.FirstTotalStock)).ToList(),
                    pv.Images.Select(img => new EditableImageDto(img.Id, img.Src, img.IsMain, img.IsDeleted)).ToList(),
                    pv.ProductVariantAttributes
                        .Select(pva => new VariantAttributeDto(pva.CategoryAttributeValueId, pva.CategoryAttributeValue, pva.CustomValue, pva.IsVarianter, pva.IsSlicer))
                        .ToList()
                )).ToList(),
                p.AttributeKeyValues.Select(akv => new AttributeKeyValueDto(
                    akv.CategoryAttributeId,
                    akv.CategoryAttribute.CategoryAttributeKey,
                    akv.AttributeValueId,
                    akv.AttributeValue.Name,
                    akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId)?.IsRequired ?? false,
                    akv.CustomValue)
                ).ToList()))
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorDataResult<ProductEditPageDto>(null!, "Ürün bulunamadı.");

        // Lookup verilerini DbSet adlarını IntegrationDbContext'te doğrula:
        var brands = await dbContext.Brands
            .Where(b => !b.IsDeleted)
            .Select(b => new BrandListDetailDto(b.Id, b.Name))
            .ToListAsync();

        var categories = await dbContext.Categories
            .Where(c => !c.IsDeleted && !c.SubCategories.Any())
            .Select(c => new CategorySelectDto(c.Id, c.Name))
            .ToListAsync();

        var branches = await dbContext.BranchOffices
            .Where(b => !b.IsDeleted)
            .Select(b => new BranchSelectDto(b.Id, b.Name))
            .ToListAsync();

        var marketplace = await dbContext.ProductMarketplaces
            .Where(pm => pm.ProductId == id && pm.MarketPlaceId == 1)
            .FirstOrDefaultAsync();

        var updatedAt = await dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        var syncStatus = BuildSyncStatus(marketplace, updatedAt);

        return new SuccessDataResult<ProductEditPageDto>(
            new ProductEditPageDto(product, brands, categories, branches, syncStatus));
    }

    public async Task<IResult> UpdateProduct(EditProductDto dto)
    {
        await applicationLogManager.AddLog("Ürün güncelleme isteği alındı.", LogType.Product, LogAction.Update, dto);

        try { await validator.ValidateAndThrowAsync(dto); }
        catch (Exception ex) { return new ErrorResult(ex.Message); }

        // Business rule: StockCode benzersizliği — LogicRunner async değil, önce sonucu hesapla
        var stockCodeConflict = await dbContext.MainProducts
            .AnyAsync(p => p.StockCode == dto.StockCode && p.Id != dto.Id && !p.IsDeleted);
        IResult stockCodeResult = stockCodeConflict
            ? new ErrorResult("Bu stok kodu başka bir ürün tarafından kullanılıyor.")
            : new SuccessResult();
        var check = LogicRunner.Run(stockCodeResult);
        if (check != null) return new ErrorResult(check.Message);

        // Tracked load — context globally no-tracking, AsTracking() zorunlu
        var product = await dbContext.MainProducts
            .AsTracking()
            .Include(p => p.ProductVariants)
            .Include(p => p.AttributeKeyValues)
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product is null)
            return new ErrorResult("Güncellenecek ürün bulunamadı.");

        // Scalar alanlar
        product.Title = dto.Title;
        product.Description = dto.Description;
        product.StockCode = dto.StockCode;
        product.Season = dto.Season;
        product.Year = dto.Year;
        product.BrandId = dto.BrandId;

        // Kategori değişikliği
        bool categoryChanged = product.CategoryId != dto.CategoryId;
        if (categoryChanged)
        {
            product.CategoryId = dto.CategoryId;
            dbContext.AttributeKeyValues.RemoveRange(product.AttributeKeyValues);
            product.AttributeKeyValues.Clear();
        }

        // AttributeKeyValues — delete all, re-add (composite PK: CategoryAttributeId + ProductId)
        if (!categoryChanged)
        {
            dbContext.AttributeKeyValues.RemoveRange(product.AttributeKeyValues);
            product.AttributeKeyValues.Clear();
        }
        foreach (var akv in dto.AttributeKeyValues.Where(a => a.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(a.CustomValue)))
        {
            product.AttributeKeyValues.Add(new AttributeKeyValue
            {
                CategoryAttributeId = akv.CategoryAttributeId,  // akv.Id değil!
                AttributeValueId = akv.AttributeValueId,         // akv.ValueId değil!
                CustomValue = akv.CustomValue
            });
        }

        // Variant scalar güncelleme (ekle/silme yok — sadece mevcut güncellenir)
        foreach (var variantDto in dto.Variants)
        {
            var variant = product.ProductVariants.FirstOrDefault(pv => pv.Id == variantDto.Id);
            if (variant is null) continue;
            variant.ListPrice = variantDto.ListPrice;
            variant.SalePrice = variantDto.SalePrice;
            variant.CostPrice = variantDto.CostPrice;
            variant.ECommercePrice = variantDto.ECommercePrice;
            variant.DimensionalWeight = variantDto.DimensionalWeight;
            variant.VatRate = variantDto.VatRate;
            variant.CurrencyType = variantDto.CurrencyType;
        }

        // Image soft-delete
        foreach (var imageId in dto.DeletedImageIds)
        {
            var image = product.ProductVariants
                .SelectMany(pv => pv.Images)
                .FirstOrDefault(img => img.Id == imageId);
            if (image is not null)
            {
                image.IsDeleted = true;
                image.DeletedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"'{product.Title}' ürünü güncellendi.", LogType.Product, LogAction.Update);
        if (categoryChanged)
            await applicationLogManager.AddLog("Ürünün kategorisi değiştirildi, mevcut özellikler temizlendi.", LogType.Product, LogAction.Update);

        productUpdatedChannel.TryPublish(new ProductUpdatedEvent(product.Id, product.Title, categoryChanged));

        return new SuccessResult("Ürün güncellendi.");
    }

    public async Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId)
    {
        var result = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailDto(
                p.Id, p.Title, p.Description, p.StockCode, p.Brand.Name, p.Category.Name,
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                p.ProductVariants.Select(pv => new ProductVariantDetailDto(
                    pv.Id, pv.Barcode, pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.VatRate,
                    pv.Images.Select(img => img.Src).ToArray(),
                    pv.BranchOfficeStocks.Select(stck => new StockDetailDto(stck.BranchOffice.Name, stck.CurrentStock, stck.SoldQuantity, stck.FirstTotalStock))
                )),
                p.AttributeKeyValues.Select(kv => new AttributeKeyValueDetailDto(
                    kv.CategoryAttribute.CategoryAttributeKey,
                    kv.AttributeValueId.HasValue ? kv.AttributeValue.Name : kv.CustomValue))
            ))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<ProductDetailDto>(result);
    }

    public async Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto)
    {
        var query = dbContext.MainProducts.AsQueryable();
        if (!string.IsNullOrEmpty(dto.FullTextSearchKey))
            query = query.Where(x =>
                x.SearchVector.Matches(dto.FullTextSearchKey.ToFullTextSearchQuery()) ||
                x.ProductVariants.Any(pv => pv.Barcode.Contains(dto.FullTextSearchKey)));

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .Select(x => new ProductsDetailDto(
                x.Id, x.Title, x.Description, x.StockCode, x.Brand.Name, x.Category.Name,
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                x.ProductVariants.Count()))
            .ToListAsync();

        return new SuccessDataResult<Pageable<ProductsDetailDto>>(new Pageable<ProductsDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }

    public Task<int> GetProductCountByCategoryId(int categoryId) =>
        dbContext.MainProducts.CountAsync(p => p.CategoryId == categoryId);

    public Task<bool> HasSoldProductsInCategory(int categoryId) =>
        dbContext.SaleItems.AnyAsync(si => si.ProductVariant.Product.CategoryId == categoryId);

    private static MarketplaceSyncStatusDto BuildSyncStatus(ProductMarketplace? marketplace, DateTimeOffset? productUpdatedAt)
    {
        if (marketplace is null)
            return new MarketplaceSyncStatusDto(MarketplaceSyncState.NeverSynced, null, null, null);

        var state = marketplace.Status switch
        {
            MarketplaceProductStatus.Pending when marketplace.BatchRequestId is null => MarketplaceSyncState.Waiting,
            MarketplaceProductStatus.Pending => MarketplaceSyncState.Processing,
            MarketplaceProductStatus.Published when productUpdatedAt > marketplace.LastSyncedAt => MarketplaceSyncState.OutOfSync,
            MarketplaceProductStatus.Published => MarketplaceSyncState.Synced,
            MarketplaceProductStatus.Failed => MarketplaceSyncState.Failed,
            MarketplaceProductStatus.Rejected => MarketplaceSyncState.Rejected,
            _ => MarketplaceSyncState.NeverSynced
        };

        return new MarketplaceSyncStatusDto(state, marketplace.LastSyncedAt, marketplace.BatchRequestId, marketplace.StatusMessage);
    }
}
```

> **Önemli:** `dbContext.Brands`, `dbContext.Categories`, `dbContext.BranchOffices`, `dbContext.ProductMarketplaces`, `dbContext.AttributeKeyValues` DbSet isimlerini `IntegrationDbContext.cs`'te doğrula. Farklı isimler varsa düzelt.
>
> `AttributeKeyValueDto`'nun property isimlerini (`akv.Id`, `akv.ValueId`, `akv.CustomValue`) `AttributeKeyValueDto` tanımına göre doğrula.

- [ ] **Step 4: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`. Hata alırsan DbSet isimlerini veya DTO property isimlerini düzelt.

- [ ] **Step 5: UpdateProduct testlerini çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~UpdateProductTests" -v minimal
```

Beklenen: `2 passed`

- [ ] **Step 6: Tüm testleri çalıştır — regresyon yok**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal
```

Beklenen: Tüm testler geçer (60+ test).

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ProductManager.cs \
        Test/Entegrasyon.Test/Business/UpdateProductTests.cs
git commit -m "feat: Implement GetProductEditPageData and UpdateProduct with primary ctor migration"
```

---

## Chunk 3: Blazor UI

### Task 8: ProductEdit ana sayfa — yükle, göster, kaydet

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor.cs`

- [ ] **Step 1: ProductEdit.razor oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor *@
@page "/products/edit/{Id:guid}"
@using Entegrasyon.Entity.Dtos.Product

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    @if (_loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (_pageData is null)
    {
        <MudAlert Severity="Severity.Error">Ürün bulunamadı.</MudAlert>
    }
    else
    {
        <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-3">
            <MudText Typo="Typo.h5">@_pageData.Product.Title</MudText>
            <MudChip T="string" Color="@_syncColor" Size="Size.Small">@_syncLabel</MudChip>
        </MudStack>

        <MudTabs Elevation="1" Rounded="true" ApplyEffectsToContainer="true">
            <MudTabPanel Text="Genel">
                <ProductEditGeneralTab
                    Brands="_pageData.Brands"
                    Categories="_pageData.LeafCategories"
                    Title="@_title"
                    TitleChanged="v => _title = v"
                    Description="@_description"
                    DescriptionChanged="v => _description = v"
                    StockCode="@_stockCode"
                    StockCodeChanged="v => _stockCode = v"
                    Season="@_season"
                    SeasonChanged="v => _season = v"
                    Year="@_year"
                    YearChanged="v => _year = v"
                    BrandId="_brandId"
                    BrandIdChanged="v => _brandId = v"
                    CategoryId="_categoryId"
                    CategoryIdChanged="OnCategoryChanged" />
            </MudTabPanel>
            <MudTabPanel Text="Varyantlar">
                <ProductEditVariantsTab
                    Variants="_variants"
                    BranchOffices="_pageData.BranchOffices" />
            </MudTabPanel>
            <MudTabPanel Text="Görseller">
                <ProductEditImagesTab
                    Images="_images"
                    NewImages="_newImages"
                    NewImagesChanged="v => _newImages = v" />
            </MudTabPanel>
            <MudTabPanel Text="Özellikler">
                <ProductEditAttributesTab
                    CategoryId="_categoryId"
                    AttributeKeyValues="_attributeKeyValues"
                    AttributeKeyValuesChanged="v => _attributeKeyValues = v" />
            </MudTabPanel>
        </MudTabs>

        <MudStack Row="true" Class="mt-4" Spacing="2">
            <MudButton Variant="Variant.Filled" Color="Color.Primary"
                       OnClick="Save" Disabled="_saving">
                @(_saving ? "Kaydediliyor..." : "Kaydet")
            </MudButton>
            <MudButton Variant="Variant.Outlined" OnClick="Cancel">İptal</MudButton>
        </MudStack>
    }
</MudContainer>
```

- [ ] **Step 2: ProductEdit.razor.cs oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEdit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductService ProductManager { get; set; } = null!;
    [Inject] private IImageManager ImageManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;

    private ProductEditPageDto? _pageData;
    private bool _loading = true;
    private bool _saving;

    // Form state
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _stockCode = string.Empty;
    private string _season = string.Empty;
    private string _year = string.Empty;
    private int _brandId;
    private int _categoryId;
    private List<VariantPriceModel> _variants = [];
    private List<ImageEditState> _images = [];
    private List<IBrowserFile> _newImages = [];
    private List<AttributeKeyValueDto> _attributeKeyValues = [];

    private Color _syncColor => _pageData?.SyncStatus.State switch
    {
        MarketplaceSyncState.Synced => Color.Success,
        MarketplaceSyncState.OutOfSync => Color.Warning,
        MarketplaceSyncState.Processing => Color.Info,
        MarketplaceSyncState.Waiting => Color.Warning,
        MarketplaceSyncState.Failed or MarketplaceSyncState.Rejected => Color.Error,
        _ => Color.Default
    };

    private string _syncLabel => _pageData?.SyncStatus.State switch
    {
        MarketplaceSyncState.NeverSynced => "Senkronize Edilmedi",
        MarketplaceSyncState.Waiting => "Bekliyor",
        MarketplaceSyncState.Processing => "Gönderildi — İşlemde",
        MarketplaceSyncState.OutOfSync => "Güncelleme Gerekiyor",
        MarketplaceSyncState.Synced => "Yayında",
        MarketplaceSyncState.Failed => "Başarısız",
        MarketplaceSyncState.Rejected => "Reddedildi",
        _ => string.Empty
    };

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        try
        {
            var result = await ProductManager.GetProductEditPageData(Id);
            if (!result.Success || result.Data is null)
            {
                Snackbar.Add(result.Message ?? "Ürün bulunamadı.", Severity.Error);
                NavigationManager.NavigateTo("/products");
                return;
            }

            _pageData = result.Data;
            var p = _pageData.Product;

            _title = p.Title;
            _description = p.Description;
            _stockCode = p.StockCode;
            _season = p.Season;
            _year = p.Year;
            _brandId = p.BrandId;
            _categoryId = p.CategoryId;

            _variants = p.ProductVariants.Select(pv => new VariantPriceModel
            {
                Id = pv.Id,
                Barcode = pv.Barcode,
                Label = BuildVariantLabel(pv),
                ListPrice = pv.ListPrice,
                SalePrice = pv.SalePrice,
                CostPrice = pv.CostPrice,
                ECommercePrice = pv.ECommercePrice,
                DimensionalWeight = pv.DimensionalWeight,
                VatRate = pv.VatRate,
                CurrencyType = pv.CurrencyType,
                CurrentStocks = pv.BranchOfficeStocks
            }).ToList();

            _images = p.ProductVariants
                .SelectMany(pv => pv.UploadedImages.Select(img => new ImageEditState
                {
                    Id = img.Id,
                    Src = img.Src,
                    IsMain = img.IsMain,
                    IsDeleted = img.IsDeleted,
                    VariantId = pv.Id
                }))
                .ToList();

            _attributeKeyValues = p.AttributeKeyValues.ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Sayfa yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnCategoryChanged(int newCategoryId)
    {
        if (newCategoryId == _categoryId) return;
        _categoryId = newCategoryId;
        _attributeKeyValues = [];
        // Özellikler sekmesi OnParametersSetAsync → LoadAttributes ile yeni kategoriyi yükleyecek
    }

    private async Task Save()
    {
        _saving = true;
        try
        {
            var editDto = new EditProductDto(
                Id: Id,
                Title: _title,
                Description: _description,
                StockCode: _stockCode,
                Season: _season,
                Year: _year,
                BrandId: _brandId,
                CategoryId: _categoryId,
                Variants: _variants.Select(v => new EditProductVariantDto(
                    v.Id, v.ListPrice, v.SalePrice, v.CostPrice, v.ECommercePrice,
                    v.DimensionalWeight, v.VatRate, v.CurrencyType)).ToList(),
                AttributeKeyValues: _attributeKeyValues,
                DeletedImageIds: _images.Where(i => i.IsDeleted).Select(i => i.Id).ToList()
            );

            var result = await ProductManager.UpdateProduct(editDto);
            if (!result.Success)
            {
                Snackbar.Add(result.Message ?? "Güncelleme başarısız.", Severity.Error);
                return;
            }

            // Yeni görseller varsa yükle — tüm seçilenler ilk variant'a eklenir
            if (_newImages.Count > 0 && _variants.Count > 0)
            {
                var firstVariantId = _variants[0].Id;
                var streams = _newImages.Select(f => new VariantImageStream(
                    firstVariantId,
                    f.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024),
                    f.Name,
                    IsMain: false));
                var imgResult = await ImageManager.AddProductImages(Id, streams);
                if (!imgResult.Success)
                    Snackbar.Add($"Görseller yüklenemedi: {imgResult.Message}", Severity.Warning);
            }

            Snackbar.Add("Ürün güncellendi.", Severity.Success);
            NavigationManager.NavigateTo("/products");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => NavigationManager.NavigateTo("/products");

    private static string BuildVariantLabel(ProductVariantEditDetailDto pv)
    {
        var attrs = pv.VariantAttributes.Where(a => a.IsVarianter || a.IsSlicer).ToList();
        return attrs.Count > 0
            ? string.Join(" / ", attrs.Select(a => a.CategoryAttributeValueName ?? a.CustomValue ?? "?"))
            : pv.Barcode;
    }

    // İç model sınıfları
    public class VariantPriceModel
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal ListPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal ECommercePrice { get; set; }
        public decimal DimensionalWeight { get; set; }
        public decimal VatRate { get; set; }
        public string CurrencyType { get; set; } = "TRY";
        public IEnumerable<EditBranchOfficeStockDto> CurrentStocks { get; set; } = [];
    }

    public class ImageEditState
    {
        public int Id { get; set; }
        public string Src { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public bool IsDeleted { get; set; }
        public Guid VariantId { get; set; }
    }
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded` — eksik component'lar için CS0103 hataları gelirse normal, Task 9-12'de ekleniyor.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor \
        Application/Entegrasyon.Blazor/Features/Products/ProductEdit.razor.cs
git commit -m "feat: Add ProductEdit main page with tab layout and save flow"
```

---

### Task 9: ProductEditGeneralTab — temel bilgi formu

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor.cs`

- [ ] **Step 1: ProductEditGeneralTab.razor oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor *@
@using Entegrasyon.Entity.Dtos.Brand
@using Entegrasyon.Entity.Dtos.Category

<MudGrid Class="pa-4">
    <MudItem xs="12" md="8">
        <MudTextField Label="Ürün Adı" Value="@Title" ValueChanged="TitleChanged"
                      Required="true" Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" md="4">
        <MudTextField Label="Stok Kodu" Value="@StockCode" ValueChanged="StockCodeChanged"
                      Required="true" Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12">
        <MudTextField Label="Açıklama" Value="@Description" ValueChanged="DescriptionChanged"
                      Lines="4" Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" md="4">
        <MudTextField Label="Sezon" Value="@Season" ValueChanged="SeasonChanged"
                      Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" md="4">
        <MudTextField Label="Yıl" Value="@Year" ValueChanged="YearChanged"
                      Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" md="6">
        <MudSelect T="int" Label="Marka" Value="BrandId" ValueChanged="BrandIdChanged"
                   Variant="Variant.Outlined">
            @foreach (var brand in Brands)
            {
                <MudSelectItem T="int" Value="@brand.Id">@brand.Name</MudSelectItem>
            }
        </MudSelect>
    </MudItem>
    <MudItem xs="12" md="6">
        <MudSelect T="int" Label="Kategori" Value="CategoryId" ValueChanged="OnCategoryChangedInternal"
                   Variant="Variant.Outlined">
            @foreach (var cat in Categories)
            {
                <MudSelectItem T="int" Value="@cat.Id">@cat.Name</MudSelectItem>
            }
        </MudSelect>
    </MudItem>
</MudGrid>
```

- [ ] **Step 2: ProductEditGeneralTab.razor.cs oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor.cs
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditGeneralTab
{
    [Parameter] public List<BrandListDetailDto> Brands { get; set; } = [];
    [Parameter] public List<CategorySelectDto> Categories { get; set; } = [];

    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TitleChanged { get; set; }

    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> DescriptionChanged { get; set; }

    [Parameter] public string StockCode { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> StockCodeChanged { get; set; }

    [Parameter] public string Season { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SeasonChanged { get; set; }

    [Parameter] public string Year { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> YearChanged { get; set; }

    [Parameter] public int BrandId { get; set; }
    [Parameter] public EventCallback<int> BrandIdChanged { get; set; }

    [Parameter] public int CategoryId { get; set; }
    [Parameter] public EventCallback<int> CategoryIdChanged { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = null!;

    private async Task OnCategoryChangedInternal(int newId)
    {
        if (newId == CategoryId) return;

        var confirm = await DialogService.ShowMessageBox(
            "Kategori Değişikliği",
            "Kategori değiştirilirse mevcut özellikler temizlenecektir. Devam etmek istiyor musunuz?",
            yesText: "Evet, Değiştir",
            cancelText: "İptal");

        if (confirm == true)
            await CategoryIdChanged.InvokeAsync(newId);
        else
            StateHasChanged(); // MudSelect görsel seçimi geri al — parent değeri değişmedi
    }
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor \
        Application/Entegrasyon.Blazor/Features/Products/ProductEditGeneralTab.razor.cs
git commit -m "feat: Add ProductEditGeneralTab with category change warning"
```

---

### Task 10: ProductEditVariantsTab — fiyat/ağırlık düzenleme

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor.cs`

- [ ] **Step 1: ProductEditVariantsTab.razor oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor *@
@using Entegrasyon.Entity.Dtos.Branches
@using static Entegrasyon.Blazor.Features.Products.ProductEdit

<MudExpansionPanels Class="pa-2">
    @foreach (var variant in Variants)
    {
        <MudExpansionPanel Text="@variant.Label">
            <MudGrid Class="pa-2">
                <MudItem xs="12">
                    <MudChip T="string" Variant="Variant.Outlined" Size="Size.Small"
                             title="Barkod düzenlenemez">
                        Barkod: @variant.Barcode
                    </MudChip>
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="Liste Fiyatı" @bind-Value="variant.ListPrice"
                                     Adornment="Adornment.Start" AdornmentText="₺" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="Satış Fiyatı" @bind-Value="variant.SalePrice"
                                     Adornment="Adornment.Start" AdornmentText="₺" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="Maliyet Fiyatı" @bind-Value="variant.CostPrice"
                                     Adornment="Adornment.Start" AdornmentText="₺" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="E-Ticaret Fiyatı" @bind-Value="variant.ECommercePrice"
                                     Adornment="Adornment.Start" AdornmentText="₺" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="Desi" @bind-Value="variant.DimensionalWeight"
                                     Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudNumericField T="decimal" Label="KDV Oranı (%)" @bind-Value="variant.VatRate"
                                     Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="6" md="3">
                    <MudTextField Label="Para Birimi" @bind-Value="variant.CurrencyType"
                                  Variant="Variant.Outlined" />
                </MudItem>

                @* Stok: read-only *@
                <MudItem xs="12">
                    <MudText Typo="Typo.subtitle2" Class="mt-2">Stok Durumu (read-only)</MudText>
                    <MudTable Items="variant.CurrentStocks.AsEnumerable()" Dense="true" Hover="false" Elevation="0">
                        <HeaderContent>
                            <MudTh>Şube</MudTh>
                            <MudTh>Stok</MudTh>
                        </HeaderContent>
                        <RowTemplate>
                            <MudTd>@context.BranchOfficeId</MudTd>
                            <MudTd>@context.FirstTotalStock</MudTd>
                        </RowTemplate>
                    </MudTable>
                    <MudLink Href="/stock-management" Typo="Typo.caption">Stok Yönetimi →</MudLink>
                </MudItem>
            </MudGrid>
        </MudExpansionPanel>
    }
</MudExpansionPanels>
```

- [ ] **Step 2: ProductEditVariantsTab.razor.cs oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor.cs
using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Components;
using static Entegrasyon.Blazor.Features.Products.ProductEdit;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditVariantsTab
{
    [Parameter] public List<VariantPriceModel> Variants { get; set; } = [];
    [Parameter] public List<BranchSelectDto> BranchOffices { get; set; } = [];
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor \
        Application/Entegrasyon.Blazor/Features/Products/ProductEditVariantsTab.razor.cs
git commit -m "feat: Add ProductEditVariantsTab for price/weight editing"
```

---

### Task 11: ProductEditImagesTab — görsel yönetimi

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor.cs`

- [ ] **Step 1: ProductEditImagesTab.razor oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor *@
@using static Entegrasyon.Blazor.Features.Products.ProductEdit

<MudGrid Class="pa-4">
    <MudItem xs="12">
        <MudText Typo="Typo.subtitle1">Mevcut Görseller</MudText>
    </MudItem>

    @foreach (var img in Images.Where(i => !i.IsDeleted))
    {
        <MudItem xs="6" sm="4" md="3">
            <MudCard>
                <MudCardMedia Image="@img.Src" Height="150" />
                <MudCardActions>
                    <MudTooltip Text="@(img.IsMain ? "Kapak görsel" : "Kapak yap")">
                        <MudIconButton Icon="@(img.IsMain ? Icons.Material.Filled.Star : Icons.Material.Outlined.Star)"
                                       Color="@(img.IsMain ? Color.Warning : Color.Default)"
                                       OnClick="() => SetCover(img)" Size="Size.Small" />
                    </MudTooltip>
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error"
                                   OnClick="() => MarkDeleted(img)" Size="Size.Small" />
                </MudCardActions>
            </MudCard>
        </MudItem>
    }

    <MudItem xs="12">
        <MudText Typo="Typo.subtitle1" Class="mt-2">Yeni Görsel Ekle</MudText>
        <MudFileUpload T="IReadOnlyList<IBrowserFile>" FilesChanged="OnFilesChanged"
                       Accept=".jpg,.jpeg,.png,.webp" Multiple="true">
            <ActivatorContent>
                <MudButton Variant="Variant.Outlined" StartIcon="@Icons.Material.Filled.Upload">
                    Görsel Seç
                </MudButton>
            </ActivatorContent>
        </MudFileUpload>
        @if (NewImages.Count > 0)
        {
            <MudText Typo="Typo.caption">@NewImages.Count yeni görsel seçildi</MudText>
        }
    </MudItem>
</MudGrid>
```

- [ ] **Step 2: ProductEditImagesTab.razor.cs oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using static Entegrasyon.Blazor.Features.Products.ProductEdit;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditImagesTab
{
    [Parameter] public List<ImageEditState> Images { get; set; } = [];
    [Parameter] public List<IBrowserFile> NewImages { get; set; } = [];
    [Parameter] public EventCallback<List<IBrowserFile>> NewImagesChanged { get; set; }

    private void SetCover(ImageEditState img)
    {
        foreach (var i in Images) i.IsMain = false;
        img.IsMain = true;
    }

    private void MarkDeleted(ImageEditState img)
    {
        img.IsDeleted = true;
    }

    private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
    {
        var updated = new List<IBrowserFile>(NewImages);
        updated.AddRange(files);
        await NewImagesChanged.InvokeAsync(updated);
    }
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor \
        Application/Entegrasyon.Blazor/Features/Products/ProductEditImagesTab.razor.cs
git commit -m "feat: Add ProductEditImagesTab with soft-delete and cover selection"
```

---

### Task 12: ProductEditAttributesTab — özellik düzenleme

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor.cs`

- [ ] **Step 1: ProductEditAttributesTab.razor oluştur**

```razor
@* Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor *@
@using Entegrasyon.Entity.Dtos.Attributes
@using Entegrasyon.Entity.Dtos.Category

<MudGrid Class="pa-4">
    @if (_loading)
    {
        <MudItem xs="12"><MudProgressCircular Indeterminate="true" Size="Size.Small" /></MudItem>
    }
    else if (_categoryAttributes.Count == 0)
    {
        <MudItem xs="12">
            <MudAlert Severity="Severity.Info">Bu kategoride düzenlenebilir özellik bulunmuyor.</MudAlert>
        </MudItem>
    }
    else
    {
        @foreach (var attr in _categoryAttributes)
        {
            <MudItem xs="12" md="6">
                @if (attr.AllowCustom)
                {
                    <MudTextField Label="@attr.CategoriyAttributeHumanized"
                                  Value="GetCustomValue(attr.Id)"
                                  ValueChanged="(string v) => SetCustomValue(attr.Id, v)"
                                  Required="attr.IsRequired"
                                  Variant="Variant.Outlined" />
                }
                else
                {
                    <MudSelect T="int?" Label="@attr.CategoriyAttributeHumanized"
                               Value="GetValueId(attr.Id)"
                               ValueChanged="(int? v) => SetValueId(attr.Id, v)"
                               Required="attr.IsRequired"
                               Clearable="true"
                               Variant="Variant.Outlined">
                        @foreach (var val in attr.CategoryAttributeValues)
                        {
                            <MudSelectItem T="int?" Value="@((int?)val.Id)">@val.Name</MudSelectItem>
                        }
                    </MudSelect>
                }
            </MudItem>
        }
    }
</MudGrid>
```

- [ ] **Step 2: ProductEditAttributesTab.razor.cs oluştur**

```csharp
// Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditAttributesTab
{
    [Parameter] public int CategoryId { get; set; }
    [Parameter] public List<AttributeKeyValueDto> AttributeKeyValues { get; set; } = [];
    [Parameter] public EventCallback<List<AttributeKeyValueDto>> AttributeKeyValuesChanged { get; set; }

    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;

    private List<CategoryAttributeDto> _categoryAttributes = [];
    private bool _loading;
    private int _loadedCategoryId;

    // Lokal durum — kaydet anında AttributeKeyValues'a dönüştürülür
    private readonly Dictionary<int, int?> _valueIds = new();
    private readonly Dictionary<int, string?> _customValues = new();

    protected override async Task OnParametersSetAsync()
    {
        if (CategoryId != _loadedCategoryId)
        {
            _loadedCategoryId = CategoryId;
            await LoadAttributes();
        }
    }

    private async Task LoadAttributes()
    {
        _loading = true;
        _categoryAttributes = [];
        _valueIds.Clear();
        _customValues.Clear();

        if (CategoryId == 0)
        {
            _loading = false;
            return;
        }

        var result = await AttributeManager.GetCategoryAttributesByCategory(CategoryId);
        if (result.Success && result.Data is not null)
        {
            // Sadece varianter/slicer olmayan regular attributelar
            _categoryAttributes = result.Data
                .Where(a => !a.IsVarianter && !a.IsSlicer)
                .ToList();

            // Mevcut değerleri doldur
            foreach (var akv in AttributeKeyValues)
            {
                _valueIds[akv.CategoryAttributeId] = akv.AttributeValueId;
                _customValues[akv.CategoryAttributeId] = akv.CustomValue;
            }
        }

        _loading = false;
        await PushChanges();
    }

    private int? GetValueId(int attrId) => _valueIds.GetValueOrDefault(attrId);
    private string GetCustomValue(int attrId) => _customValues.GetValueOrDefault(attrId) ?? string.Empty;

    private async Task SetValueId(int attrId, int? value)
    {
        _valueIds[attrId] = value;
        await PushChanges();
    }

    private async Task SetCustomValue(int attrId, string? value)
    {
        _customValues[attrId] = value;
        await PushChanges();
    }

    private async Task PushChanges()
    {
        var updated = _categoryAttributes
            .Select(a => new AttributeKeyValueDto(
                CategoryAttributeId: a.Id,
                CategoryAttributeName: a.CategoriyAttributeHumanized,
                AttributeValueId: _valueIds.GetValueOrDefault(a.Id),
                AttributeValueName: string.Empty,
                IsRequired: a.IsRequired,
                CustomValue: _customValues.GetValueOrDefault(a.Id) ?? string.Empty))
            .Where(akv => akv.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(akv.CustomValue))
            .ToList();

        await AttributeKeyValuesChanged.InvokeAsync(updated);
    }
}
```

> **Not:** `AttributeKeyValueDto`'nun constructor parametrelerini (`Id`, `CategoryAttributeKey`, `ValueId`, `ValueName`, `IsRequired`, `CustomValue`) gerçek DTO tanımına göre doğrula. `CategoryAttributeDto`'daki property isimlerini de (`CategoriyAttributeHumanized`, `CategoryAttributeValues`, `CategoryAttributeKey`, `IsVarianter`, `IsSlicer`, `IsRequired`, `AllowCustom`) doğrula.

- [ ] **Step 3: Build al**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: `Build succeeded`

- [ ] **Step 4: Tüm testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal
```

Beklenen: Tüm testler geçer.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor \
        Application/Entegrasyon.Blazor/Features/Products/ProductEditAttributesTab.razor.cs
git commit -m "feat: Add ProductEditAttributesTab with dynamic attribute loading"
```

---

### Task 13: Manuel test ve son dokunuşlar

- [ ] **Step 1: Uygulamayı çalıştır**

```bash
cd Application/Entegrasyon.Blazor && dotnet run
```

- [ ] **Step 2: Test senaryoları**

Tarayıcıda şu senaryoları dene:

1. `/products` → bir üründe "Düzenle" tıkla → `/products/edit/{id}` açılmalı
2. Genel sekmesinde başlık değiştir → Kaydet → `/products`'a dönmeli, snackbar "Ürün güncellendi" göstermeli
3. Kategori değiştir → dialog uyarısı çıkmalı → İptal tıklayınca kategori değişmemeli
4. Kategori değiştir → Evet tıkla → Özellikler sekmesi sıfırlanmalı
5. Varyantlar sekmesinde fiyat düzenle → Kaydet
6. Görseller sekmesinde bir görseli sil (X butonu) → Kaydet → Görsel gizlenmeli
7. Geçersiz ID ile `/products/edit/00000000-0000-0000-0000-000000000000` → snackbar hata + yönlendirme
8. Marketplace sync badge doğru renk/etiket göstermeli

- [ ] **Step 3: Son commit**

```bash
git add -A
git commit -m "feat: Product edit feature complete — all tabs functional"
```
