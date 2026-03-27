using System.Xml.Linq;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11ProductMapper için birim testleri.
/// Ürün verilerinin N11 SaveProduct XML formatına doğru dönüştürüldüğünü doğrular.
/// </summary>
public class N11ProductMapperTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IMinioFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<N11ProductMapper>> _loggerMock = new();

    private N11ProductMapper CreateSut() => new(
        mockContextFactory.Object,
        _fileStorageMock.Object,
        _loggerMock.Object);

    // -----------------------------------------------------------------------
    // Yardımcı veri kurulum metodları
    // -----------------------------------------------------------------------

    private static Product BuildProduct(Guid? id = null)
    {
        var productId = id ?? Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var branchOfficeId = 1;

        return new Product
        {
            Id = productId,
            Title = "Test Ürünü",
            Description = "Test açıklaması",
            StockCode = "TST-001",
            CategoryId = 10,
            AttributeKeyValues =
            [
                new AttributeKeyValue
                {
                    CategoryAttributeId = 100,
                    AttributeValueId = 200,
                    CategoryAttribute = new CategoryAttribute
                    {
                        Id = 100,
                        CategoryAttributeHumanized = "Renk"
                    },
                    AttributeValue = new CategoryAttributeValue
                    {
                        Id = 200,
                        Name = "Kırmızı",
                        CategoryAttributeId = 100
                    }
                }
            ],
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = variantId,
                    Barcode = "1234567890123",
                    ListPrice = 150m,
                    SalePrice = 120m,
                    BranchOfficeStocks =
                    [
                        new BranchOfficeStock
                        {
                            BranchOfficeId = branchOfficeId,
                            CurrentStock = 10
                        }
                    ],
                    Images =
                    [
                        new Image
                        {
                            StorageKey = "products/test/image1.jpg",
                            DisplayOrder = 0
                        }
                    ]
                }
            ]
        };
    }

    private static Product BuildProductWithTwoVariants()
    {
        var productId = Guid.NewGuid();
        var branchOfficeId = 1;

        return new Product
        {
            Id = productId,
            Title = "Çift Varyanatlı Ürün",
            Description = "Açıklama",
            StockCode = "TST-002",
            CategoryId = 10,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    Barcode = "BAR-001",
                    ListPrice = 200m,
                    SalePrice = 180m,
                    BranchOfficeStocks =
                    [
                        new BranchOfficeStock { BranchOfficeId = branchOfficeId, CurrentStock = 5 }
                    ],
                    Images =
                    [
                        new Image { StorageKey = "products/test/img1.jpg", DisplayOrder = 0 }
                    ]
                },
                new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    Barcode = "BAR-002",
                    ListPrice = 200m,
                    SalePrice = 180m,
                    BranchOfficeStocks =
                    [
                        new BranchOfficeStock { BranchOfficeId = branchOfficeId, CurrentStock = 3 }
                    ],
                    Images =
                    [
                        new Image { StorageKey = "products/test/img2.jpg", DisplayOrder = 0 }
                    ]
                }
            ]
        };
    }

    private void SetupDefaultDbSets(Product product)
    {
        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([product]);

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(
            [
                new CategoryMarketPlaceMatch
                {
                    ApplicationCategoryId = product.CategoryId,
                    MarketPlaceId = 2,
                    MarketPlaceCategoryId = 999
                }
            ]);

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(
            [
                new CategoryAttributeMarketPlaceMatch
                {
                    ApplicationCategoryAttributeId = 100,
                    MarketPlaceId = 2,
                    MarketPlaceCategoryAttributeId = 300
                }
            ]);

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(
            [
                new MarketPlaceWarehouse
                {
                    MarketPlaceId = 2,
                    BranchOfficeId = 1
                }
            ]);

        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice>());

        _fileStorageMock
            .Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");
    }

    // -----------------------------------------------------------------------
    // Test 1: Geçerli ürün için XML üretimi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MapProductAsync_ShouldReturnValidXml()
    {
        // Arrange
        var product = BuildProduct();
        SetupDefaultDbSets(product);
        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(product.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var xml = result.Data!;
        xml.Name.LocalName.Should().Be("product");

        // productSellerCode: StockCode ?? Id
        xml.Element("productSellerCode")!.Value.Should().Be(product.StockCode);

        // title
        xml.Element("title")!.Value.Should().Be(product.Title);

        // category id
        xml.Element("category")!.Element("id")!.Value.Should().Be("999");

        // price: ilk varyantın ListPrice
        xml.Element("price")!.Value.Should().Be("150");

        // currencyType = 1
        xml.Element("currencyType")!.Value.Should().Be("1");

        // productCondition = 1
        xml.Element("productCondition")!.Value.Should().Be("1");

        // preparingDay = 3
        xml.Element("preparingDay")!.Value.Should().Be("3");

        // domestic = false
        xml.Element("domestic")!.Value.Should().Be("false");
    }

    // -----------------------------------------------------------------------
    // Test 2: 2 varyant → 2 stockItem
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MapProductAsync_ShouldMapVariantsAsStockItems()
    {
        // Arrange
        var product = BuildProductWithTwoVariants();

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([product]);

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(
            [
                new CategoryMarketPlaceMatch
                {
                    ApplicationCategoryId = product.CategoryId,
                    MarketPlaceId = 2,
                    MarketPlaceCategoryId = 999
                }
            ]);

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(
            [
                new MarketPlaceWarehouse { MarketPlaceId = 2, BranchOfficeId = 1 }
            ]);

        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice>());

        _fileStorageMock
            .Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(product.Id);

        // Assert
        result.Success.Should().BeTrue();
        var xml = result.Data!;

        var stockItems = xml.Element("stockItems")?.Elements("stockItem").ToList();
        stockItems.Should().NotBeNull();
        stockItems!.Count.Should().Be(2);

        stockItems[0].Element("sellerStockCode")!.Value.Should().Be("BAR-001");
        stockItems[0].Element("quantity")!.Value.Should().Be("5");

        stockItems[1].Element("sellerStockCode")!.Value.Should().Be("BAR-002");
        stockItems[1].Element("quantity")!.Value.Should().Be("3");
    }

    // -----------------------------------------------------------------------
    // Test 3: Ürün bulunamadığında hata döner
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MapProductAsync_WhenProductNotFound_ShouldReturnError()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(new List<MarketPlaceWarehouse>());

        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice>());

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // -----------------------------------------------------------------------
    // Test 4: Ürün düzeyindeki özellikler <product><attributes> içinde listelenmeli
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MapProductAsync_ShouldMapProductLevelAttributes()
    {
        // Arrange
        var product = BuildProduct();
        SetupDefaultDbSets(product);

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(product.Id);

        // Assert
        result.Success.Should().BeTrue();
        var xml = result.Data!;

        var attributes = xml.Element("attributes")?.Elements("attribute").ToList();
        attributes.Should().NotBeNull();
        attributes!.Should().NotBeEmpty();

        // İlk özellik: Renk = Kırmızı
        var colorAttr = attributes!.FirstOrDefault(a => a.Element("name")?.Value == "Renk");
        colorAttr.Should().NotBeNull();
        colorAttr!.Element("value")!.Value.Should().Be("Kırmızı");
    }

    // -----------------------------------------------------------------------
    // Test 5: Kategori eşleştirmesi bulunamadığında hata döner
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MapProductAsync_WhenCategoryMatchNotFound_ShouldReturnError()
    {
        // Arrange
        var product = BuildProduct();

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([product]);

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        // Kategori eşleştirmesi yok
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(new List<MarketPlaceWarehouse>());

        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(new List<BranchOffice>());

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(product.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }
}
