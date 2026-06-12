using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolProductMapper unit tests — verifies product-to-TrendyolProductItem mapping,
/// including variant handling, attribute matching, price overrides, and error cases.
/// </summary>
public class TrendyolProductMapperTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IMinioFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<TrendyolProductMapper>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestVariantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public TrendyolProductMapperTests()
    {
        _fileStorageMock
            .Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");
    }

    private TrendyolProductMapper CreateSut() => new(
        mockContextFactory.Object,
        _fileStorageMock.Object,
        _loggerMock.Object);

    private Product CreateTestProduct(
        int categoryId = 1,
        int? brandId = 10,
        string title = "Test Urun",
        string description = "Aciklama",
        List<ProductVariant>? variants = null,
        List<AttributeKeyValue>? attrs = null) => new()
    {
        Id = TestProductId,
        Title = title,
        Description = description ?? "Aciklama",
        CategoryId = categoryId,
        BrandId = brandId,
        ProductVariants = variants ?? new List<ProductVariant>(),
        AttributeKeyValues = attrs ?? new List<AttributeKeyValue>()
    };

    private ProductVariant CreateTestVariant(
        string barcode = "1234567890123",
        decimal listPrice = 200m,
        decimal salePrice = 150m,
        int vatRate = 20,
        int stock = 10,
        bool hasImage = true) => new()
    {
        Id = TestVariantId,
        Barcode = barcode,
        ListPrice = listPrice,
        SalePrice = salePrice,
        VatRate = vatRate,
        DimensionalWeight = 1.5m,
        BranchOfficeStocks = new List<BranchOfficeStock>
        {
            new() { BranchOfficeId = 1, CurrentStock = stock, BranchOffice = new BranchOffice { Id = 1, IsDefaultMarketPlaceStock = true } }
        },
        Images = hasImage
            ? new List<Image> { new() { StorageKey = "products/test.jpg", DisplayOrder = 0 } }
            : new List<Image>()
    };

    private void SetupDbForMapping(
        Product product,
        bool hasBrandMatch = true,
        bool hasCategoryMatch = true,
        List<ProductMarketplace>? productMarketplaces = null)
    {
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(productMarketplaces ?? new List<ProductMarketplace>());

        var brandMatches = hasBrandMatch && product.BrandId.HasValue
            ? new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = product.BrandId.Value, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceBrandId = 500 }
            }
            : new List<BrandMarketPlaceMatch>();
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches).ReturnsDbSet(brandMatches);

        var categoryMatches = hasCategoryMatch
            ? new List<CategoryMarketPlaceMatch>
            {
                new() { ApplicationCategoryId = product.CategoryId, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceCategoryId = 1000 }
            }
            : new List<CategoryMarketPlaceMatch>();
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches).ReturnsDbSet(categoryMatches);

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeValues)
            .ReturnsDbSet(new List<CategoryAttributeValue>());

        mockIntegrationDbContext.Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(new List<MarketPlaceWarehouse>());
        mockIntegrationDbContext.Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice> { new() { Id = 1, IsDefaultMarketPlaceStock = true } });
    }

    // ── Test 1: Product not found ──

    [Fact]
    public async Task MapProductAsync_WhenProductNotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // ── Test 2: Product has no variants ──

    [Fact]
    public async Task MapProductAsync_WhenNoVariants_ReturnsError()
    {
        // Arrange
        var product = CreateTestProduct(variants: new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varyanti yok");
    }

    // ── Test 3: Brand match missing ──

    [Fact]
    public async Task MapProductAsync_WhenBrandMatchMissing_ReturnsError()
    {
        // Arrange
        var variant = CreateTestVariant();
        var product = CreateTestProduct(variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product, hasBrandMatch: false);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Marka");
    }

    // ── Test 4: Category match missing ──

    [Fact]
    public async Task MapProductAsync_WhenCategoryMatchMissing_ReturnsError()
    {
        // Arrange
        var variant = CreateTestVariant();
        var product = CreateTestProduct(variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product, hasCategoryMatch: false);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kategori");
    }

    // ── Test 5: Happy path — maps variant correctly ──

    [Fact]
    public async Task MapProductAsync_HappyPath_MapsVariantToProductItem()
    {
        // Arrange
        var variant = CreateTestVariant(barcode: "BC001", listPrice: 200m, salePrice: 150m);
        var product = CreateTestProduct(
            title: "Test Urun Adi",
            description: "Uzun aciklama",
            variants: new List<ProductVariant> { variant });

        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(1);

        var item = result.Data.Items[0];
        item.Barcode.Should().Be("BC001");
        item.Title.Should().Be("Test Urun Adi");
        item.BrandId.Should().Be(500);
        item.CategoryId.Should().Be(1000);
        item.ListPrice.Should().Be(200m);
        item.SalePrice.Should().Be(150m);
    }

    // ── Test 6: Variant without images is skipped ──

    [Fact]
    public async Task MapProductAsync_VariantWithoutImages_IsSkipped()
    {
        // Arrange
        var variantNoImage = CreateTestVariant(hasImage: false);
        var product = CreateTestProduct(variants: new List<ProductVariant> { variantNoImage });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("gorsel");
    }

    // ── Test 7: SalePrice capped to ListPrice ──

    [Fact]
    public async Task MapProductAsync_SalePriceExceedsListPrice_CappedToListPrice()
    {
        // Arrange
        var variant = CreateTestVariant(listPrice: 100m, salePrice: 200m);
        var product = CreateTestProduct(variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].SalePrice.Should().Be(100m);
    }

    // ── Test 8: Title truncated to 100 chars ──

    [Fact]
    public async Task MapProductAsync_LongTitle_TruncatedTo100Chars()
    {
        // Arrange
        var longTitle = new string('A', 150);
        var variant = CreateTestVariant();
        var product = CreateTestProduct(title: longTitle, variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].Title.Should().HaveLength(100);
    }

    // ── Test 9: Description truncated to 30000 chars ──

    [Fact]
    public async Task MapProductAsync_LongDescription_TruncatedTo30000Chars()
    {
        // Arrange
        var longDesc = new string('B', 35000);
        var variant = CreateTestVariant();
        var product = CreateTestProduct(description: longDesc, variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].Description.Should().HaveLength(30000);
    }

    // ── Test 10: Invalid VatRate defaults to 20 ──

    [Fact]
    public async Task MapProductAsync_InvalidVatRate_DefaultsTo20()
    {
        // Arrange
        var variant = CreateTestVariant(vatRate: 15);
        var product = CreateTestProduct(variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].VatRate.Should().Be(20);
    }

    // ── Test 11: Title/Description override from ProductMarketplace ──

    [Fact]
    public async Task MapProductAsync_UsesOverrides_WhenProductMarketplaceHasOverrides()
    {
        // Arrange
        var variant = CreateTestVariant();
        var product = CreateTestProduct(
            title: "Original Title",
            description: "Original Desc",
            variants: new List<ProductVariant> { variant });

        var pmOverride = new ProductMarketplace
        {
            ProductId = TestProductId,
            MarketPlaceId = TrendyolMarketPlaceId,
            TitleOverride = "Override Title",
            DescriptionOverride = "Override Desc",
            VariantOverrides = new List<ProductVariantMarketplaceOverride>()
        };

        SetupDbForMapping(product, productMarketplaces: new List<ProductMarketplace> { pmOverride });
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].Title.Should().Be("Override Title");
        result.Data.Items[0].Description.Should().Be("Override Desc");
    }

    // ── Test 12: ProductMainId is product GUID ──

    [Fact]
    public async Task MapProductAsync_ProductMainId_IsProductGuid()
    {
        // Arrange
        var variant = CreateTestVariant();
        var product = CreateTestProduct(variants: new List<ProductVariant> { variant });
        SetupDbForMapping(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items[0].ProductMainId.Should().Be(TestProductId.ToString());
    }
}
