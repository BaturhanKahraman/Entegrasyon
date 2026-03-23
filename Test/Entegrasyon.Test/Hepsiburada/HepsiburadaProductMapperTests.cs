using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaProductMapper unit testleri.
/// Urun → HepsiburadaProductItem mapping islemleri test edilir.
/// </summary>
public class HepsiburadaProductMapperTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IMinioFileStorage> _mockFileStorage = new();
    private readonly Mock<ILogger<HepsiburadaProductMapper>> _mockLogger = new();

    private HepsiburadaProductMapper CreateSut() =>
        new(mockContextFactory.Object, _mockFileStorage.Object, _mockLogger.Object);

    private static Product BuildProduct(Guid productId, int categoryId, int? brandId = 1)
    {
        var variantId = Guid.NewGuid();
        return new Product
        {
            Id = productId,
            Title = "Test HB Urun",
            Description = "HB icin test urunu",
            StockCode = "HB-TST-001",
            CategoryId = categoryId,
            BrandId = brandId,
            Brand = brandId.HasValue ? new Brand { Id = brandId.Value, Name = "Test Marka" } : null,
            Category = new Category { Id = categoryId, Name = "Test Kategori" },
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = variantId,
                    ProductId = productId,
                    Barcode = "1234567890123",
                    SalePrice = 149.90m,
                    ListPrice = 199.90m,
                    VatRate = 20m,
                    DimensionalWeight = 2.5m,
                    BranchOfficeStocks = new List<BranchOfficeStock>
                    {
                        new()
                        {
                            BranchOfficeId = 1,
                            CurrentStock = 75,
                            BranchOffice = new BranchOffice { Id = 1, IsDefaultMarketPlaceStock = true }
                        }
                    },
                    Images = new List<Image>
                    {
                        new() { StorageKey = "img/product-1.jpg" }
                    }
                }
            },
            AttributeKeyValues = new List<AttributeKeyValue>()
        };
    }

    private void SetupMarketplace()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>
            {
                new()
                {
                    Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
                    SellerId = "hb-merchant-uuid"
                }
            });
    }

    private void SetupCategoryMatch(int categoryId, int hbCategoryId)
    {
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new()
                {
                    CategoryId = categoryId,
                    MarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId,
                    MarketPlaceCategoryId = hbCategoryId
                }
            });
    }

    private void SetupEmptyAttributeMatches()
    {
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());
    }

    private void SetupEmptyProductMarketplace()
    {
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());
    }

    private void SetupWarehouse()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(new List<MarketPlaceWarehouse>());
        mockIntegrationDbContext.Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice>
            {
                new() { Id = 1, IsDefaultMarketPlaceStock = true }
            });
    }

    // ── Test 1: Valid product maps correctly ────────────────────────────────

    [Fact]
    public async Task MapProductAsync_ValidProduct_ReturnsItems()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);

        var item = result.Data![0];
        item.CategoryId.Should().Be(500);
        item.Merchant.Should().Be("hb-merchant-uuid");
        item.Attributes.Should().ContainKey("UrunAdi");
        item.Attributes["UrunAdi"].Should().Be("Test HB Urun");
    }

    // ── Test 2: Product not found returns error ─────────────────────────────

    [Fact]
    public async Task MapProductAsync_ProductNotFound_ReturnsError()
    {
        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        var sut = CreateSut();
        var result = await sut.MapProductAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Ürün bulunamadı");
    }

    // ── Test 3: Product with no variants returns error ──────────────────────

    [Fact]
    public async Task MapProductAsync_NoVariants_ReturnsError()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Empty Product",
            CategoryId = 10,
            ProductVariants = new List<ProductVariant>(),
            AttributeKeyValues = new List<AttributeKeyValue>()
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varyantı yok");
    }

    // ── Test 4: No marketplace record returns error ─────────────────────────

    [Fact]
    public async Task MapProductAsync_NoMarketplace_ReturnsError()
    {
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>());

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("marketplace kaydı bulunamadı");
    }

    // ── Test 5: No category match returns error ─────────────────────────────

    [Fact]
    public async Task MapProductAsync_NoCategoryMatch_ReturnsError()
    {
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("eşleştirilmemiş");
    }

    // ── Test 6: Price formatted with comma ──────────────────────────────────

    [Fact]
    public async Task MapProductAsync_PriceFormattedWithComma()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        var attrs = result.Data![0].Attributes;
        var price = attrs["price"].ToString();
        price.Should().Contain(","); // HB requires comma-separated decimal
    }

    // ── Test 7: MerchantSku is uppercased ───────────────────────────────────

    [Fact]
    public async Task MapProductAsync_MerchantSkuIsUppercased()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        var merchantSku = result.Data![0].Attributes["merchantSku"].ToString();
        merchantSku.Should().Be(merchantSku!.ToUpperInvariant());
    }

    // ── Test 8: Brand name mapped to Marka attribute ────────────────────────

    [Fact]
    public async Task MapProductAsync_BrandMappedToMarkaAttribute()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        var attrs = result.Data![0].Attributes;
        attrs.Should().ContainKey("Marka");
        attrs["Marka"].Should().Be("Test Marka");
    }

    // ── Test 9: Images mapped with public URLs ──────────────────────────────

    [Fact]
    public async Task MapProductAsync_ImagesMappedCorrectly()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl("img/product-1.jpg"))
            .Returns("https://cdn.example.com/product-1.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        var attrs = result.Data![0].Attributes;
        attrs.Should().ContainKey("Image1");
        attrs["Image1"].Should().Be("https://cdn.example.com/product-1.jpg");
    }

    // ── Test 10: Stock calculated from warehouse branches ───────────────────

    [Fact]
    public async Task MapProductAsync_StockCalculatedFromBranches()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        SetupMarketplace();
        SetupCategoryMatch(categoryId, 500);
        SetupEmptyAttributeMatches();
        SetupEmptyProductMarketplace();
        SetupWarehouse();

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();
        var result = await sut.MapProductAsync(productId);

        result.Success.Should().BeTrue();
        var attrs = result.Data![0].Attributes;
        attrs["stock"].Should().Be("75");
    }
}
