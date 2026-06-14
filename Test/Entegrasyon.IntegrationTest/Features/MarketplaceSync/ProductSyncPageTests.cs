using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Features.MarketplaceSync;

/// <summary>
/// Integration tests for ProductSyncManager.GetSendPreflightAsync() — preflight validation
/// before sending a product to Trendyol.
/// Tests cover 5 checks: category match, brand match, required attributes, variants, barcodes.
/// </summary>
[Trait("Category", "Integration")]
public class ProductSyncPageTests : IntegrationTestBase
{
    private const int TrendyolMarketPlaceId = 1;

    public ProductSyncPageTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Sync Test Marka", categoryName: "Sync Test Kategori");
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol");
    }

    /// <summary>
    /// All checks pass — Product with category match, brand match, all required attributes matched,
    /// 2 variants with barcodes.
    /// Expected: Success = true, AllPassed = true, all individual checks = true.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_AllChecksPassed_ShouldReturnSuccess()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, variantId1) = await SeedProductWithStockAsync("PREFLIGHT-PASS-001", stock: 10);

        // Add category match
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);

        // Add brand match
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        // Create a required attribute and match it
        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        // Add variant 2
        var variantId2 = await AddProductVariantAsync(productId, "PREFLIGHT-PASS-001-V2", 50);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.CategoryMatched.Should().BeTrue();
        data.BrandMatched.Should().BeTrue();
        data.RequiredAttributesMatched.Should().BeTrue();
        data.MissingAttributes.Should().BeEmpty();
        data.HasVariants.Should().BeTrue();
        data.AllVariantsHaveBarcodes.Should().BeTrue();
        data.AllPassed.Should().BeTrue();
    }

    /// <summary>
    /// Category not matched — Product without CategoryMarketPlaceMatch entry for MarketPlaceId=1.
    /// Expected: Success = true, CategoryMatched = false, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_CategoryNotMatched_ShouldReturnFalse()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("PREFLIGHT-NOCAT-001", stock: 10);

        // Add brand match (but NOT category match)
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        // Add one required attribute match (to focus on category mismatch)
        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.CategoryMatched.Should().BeFalse();
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Brand not matched — Product with category match but no BrandMarketPlaceMatch entry.
    /// Expected: Success = true, BrandMatched = false, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_BrandNotMatched_ShouldReturnFalse()
    {
        // Arrange
        var (categoryId, _) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("PREFLIGHT-NOBRAND-001", stock: 10);

        // Add category match (but NOT brand match)
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);

        // Add one required attribute match
        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.BrandMatched.Should().BeFalse();
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Required attributes missing — Product missing CategoryAttributeMarketPlaceMatch for a required attribute.
    /// Expected: Success = true, RequiredAttributesMatched = false, MissingAttributes contains missing attribute names, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_RequiredAttributeNotMatched_ShouldReturnFalse()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("PREFLIGHT-NOATTR-001", stock: 10);

        // Add category and brand matches
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        // Create TWO required attributes: one matched, one NOT matched
        var matchedAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(matchedAttrId, TrendyolMarketPlaceId, 5001);

        var missingAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Beden");
        // Intentionally DO NOT create marketplace match for Beden (Size)

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.RequiredAttributesMatched.Should().BeFalse();
        data.MissingAttributes.Should().Contain("beden"); // lowercase key
        data.MissingAttributes.Should().NotContain("renk");
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// No variants — Product with all matches but zero variants.
    /// Expected: Success = true, HasVariants = false, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_NoVariants_ShouldReturnFalse()
    {
        // Arrange — create product WITHOUT variants
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var productId = await SeedProductWithoutVariantsAsync("PREFLIGHT-NOVAR-001");

        // Add all matches
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.HasVariants.Should().BeFalse();
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Variant without barcode — Product with matches and 2 variants, but one variant has null Barcode.
    /// Expected: Success = true, AllVariantsHaveBarcodes = false, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_VariantMissingBarcode_ShouldReturnFalse()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, variantId1) = await SeedProductWithStockAsync("PREFLIGHT-NOBC-001", stock: 10);

        // Add all matches
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        // Add variant WITHOUT barcode
        await AddProductVariantWithoutBarcodeAsync(productId);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.AllVariantsHaveBarcodes.Should().BeFalse();
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Multiple failures — Product failing multiple checks: missing category match AND having variant without barcode.
    /// Expected: Success = true, CategoryMatched = false, AllVariantsHaveBarcodes = false, AllPassed = false.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_MultipleFailures_ShouldReturnFalse()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("PREFLIGHT-MULTI-FAIL-001", stock: 10);

        // Add ONLY brand match (skip category match)
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        // Add required attribute match
        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);

        // Add variant WITHOUT barcode
        await AddProductVariantWithoutBarcodeAsync(productId);

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        var data = result.Data!;
        data.CategoryMatched.Should().BeFalse("Category match should fail");
        data.AllVariantsHaveBarcodes.Should().BeFalse("Barcode check should fail");
        data.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Product not found — Requesting preflight for non-existent product.
    /// Expected: Success = false, message contains error about product not found.
    /// </summary>
    [Fact]
    public async Task GetSendPreflightAsync_ProductNotFound_ShouldReturnError()
    {
        // Arrange
        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        var nonExistentProductId = Guid.NewGuid();

        // Act
        var result = await service.GetSendPreflightAsync(nonExistentProductId, TrendyolMarketPlaceId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse("Product not found should return failure");
        result.Message.Should().NotBeNullOrEmpty();
        result.Data.Should().BeNull();
    }

    // ─── Helper Methods ───

    /// <summary>
    /// Get default category and brand IDs from seeded data.
    /// </summary>
    private async Task<(int CategoryId, int BrandId)> GetCategoryAndBrandIdsAsync()
    {
        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories.FirstAsync(c => c.Name == "Sync Test Kategori");
        var brand = await dbContext.Brands.FirstAsync(b => b.Name == "Sync Test Marka");
        return (category.Id, brand.Id);
    }

    /// <summary>
    /// Seed a category mapping in the canonical CategoryMarketplaces table
    /// (the table the mapping wizard writes and preflight/send reads).
    /// </summary>
    private async Task SeedCategoryMarketPlaceMatchAsync(int categoryId, int marketPlaceId, int marketPlaceCategoryId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.CategoryId == categoryId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.CategoryMarketplaces.Add(new CategoryMarketplace
            {
                CategoryId = categoryId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceCategoryId = marketPlaceCategoryId,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Seed a BrandMarketPlaceMatch entry.
    /// </summary>
    private async Task SeedBrandMarketPlaceMatchAsync(int brandId, int marketPlaceId, int marketPlaceBrandId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.BrandMarketPlaceMatches
            .AnyAsync(m => m.ApplicationBrandId == brandId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
            {
                ApplicationBrandId = brandId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = marketPlaceBrandId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Create a required CategoryAttribute for the given category.
    /// </summary>
    private async Task<int> SeedRequiredCategoryAttributeAsync(int categoryId, string attributeName)
    {
        using var dbContext = CreateDbContext();

        // Create the attribute
        var attr = new CategoryAttribute
        {
            CategoryAttributeKey = attributeName.ToLower(),
            CategoryAttributeHumanized = attributeName,
            ImportId = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.CategoryAttributes.Add(attr);
        await dbContext.SaveChangesAsync();

        var attrId = attr.Id;

        // Link it to the category as required
        var catAttrCat = new CategoryAttributeCategory
        {
            CategoryId = categoryId,
            CategoryAttributeId = attrId,
            IsRequired = true,
            IsSlicer = false,
            IsVarianter = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.CategoryAttributeCategories.Add(catAttrCat);
        await dbContext.SaveChangesAsync();

        return attrId;
    }

    /// <summary>
    /// Seed a CategoryAttributeMarketPlaceMatch entry.
    /// </summary>
    private async Task SeedCategoryAttributeMarketPlaceMatchAsync(int categoryAttributeId,
        int marketPlaceId, int marketPlaceCategoryAttributeId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.CategoryAttributeMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryAttributeId == categoryAttributeId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
            {
                ApplicationCategoryAttributeId = categoryAttributeId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceCategoryAttributeId = marketPlaceCategoryAttributeId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Create a product WITHOUT any variants.
    /// </summary>
    private async Task<Guid> SeedProductWithoutVariantsAsync(string stockCode)
    {
        using var dbContext = CreateDbContext();

        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var productId = Guid.NewGuid();

        dbContext.MainProducts.Add(new Product
        {
            Id = productId,
            Title = $"Test Product {stockCode}",
            Description = "Integration test product (no variants)",
            StockCode = stockCode,
            BrandId = brandId,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return productId;
    }

    /// <summary>
    /// Add a variant to an existing product with a given barcode.
    /// </summary>
    private async Task<Guid> AddProductVariantAsync(Guid productId, string barcode, int stock)
    {
        using var dbContext = CreateDbContext();

        var variantId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = barcode,
            ListPrice = 200,
            SalePrice = 180,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ProductVariants.Add(variant);

        // Add stock
        dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = 1,
            ProductVariantId = variantId,
            FirstTotalStock = stock,
            SoldQuantity = 0
        });

        await dbContext.SaveChangesAsync();
        return variantId;
    }

    /// <summary>
    /// Add a variant to a product WITHOUT a barcode (barcode = null).
    /// </summary>
    private async Task<Guid> AddProductVariantWithoutBarcodeAsync(Guid productId)
    {
        using var dbContext = CreateDbContext();

        var variantId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = null, // NO BARCODE
            ListPrice = 200,
            SalePrice = 180,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ProductVariants.Add(variant);

        // Add minimal stock
        dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = 1,
            ProductVariantId = variantId,
            FirstTotalStock = 5,
            SoldQuantity = 0
        });

        await dbContext.SaveChangesAsync();
        return variantId;
    }
}
