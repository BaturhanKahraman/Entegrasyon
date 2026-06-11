using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Options;
using ProductMapper = Entegrasyon.Business.Mappers.ProductMapper;

namespace Entegrasyon.UnitTest.Business;

public class ProductManagerTests : BaseTest
{
    private readonly IProductService _productManager;
    private readonly ProductMapper _productMapper = new();
    private readonly Mock<IAttributeKeyValueManager> _mockAttributeKeyValueManager = new();
    private readonly Mock<IBarcodeService> _mockBarcodeService = new();
    private readonly Mock<IMinioFileStorage> _mockMinioFileStorage = new();
    private readonly Mock<IOptions<NotificationFeatureFlags>> _mockNotificationFlags = new();
    private readonly Mock<ICurrentUserContext> _mockCurrentUser = new();

    public ProductManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddProductDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(new List<Category>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockNotificationFlags.Setup(x => x.Value).Returns(new NotificationFeatureFlags { PublishEnabled = false });
        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _productManager = new ProductManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _productMapper,
            MockValidator.Object,
            _mockAttributeKeyValueManager.Object,
            _mockBarcodeService.Object,
            _mockMinioFileStorage.Object,
            new VariantNamingService(),
            _mockNotificationFlags.Object,
            _mockCurrentUser.Object
        );
    }

    private static AddProductDto BuildValidDto(string barcode = "1234567890123") => new()
    {
        Title = "Test Ürün",
        Description = "Test açıklama",
        StockCode = "TST-001",
        CategoryId = 1,
        BrandId = 1,
        AttributeKeyValues = [],
        ProductVariants =
        [
            new AddProductVariantDto
            {
                Barcode = barcode,
                ListPrice = 100,
                SalePrice = 90,
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 5 }]
            }
        ]
    };

    [Fact]
    public async Task AddProduct_ShouldSucceed_WhenAllStocksAreZero()
    {
        // Bug #3: Esnaf stoksuz ürün ekleyebilmeli (ön sipariş / yolda / tükenmiş).
        // "En az bir stok gir" iş kuralı kaldırıldı → stoksuz ürün başarıyla kaydedilmeli.
        // Arrange
        var dto = BuildValidDto();
        foreach (var variant in dto.ProductVariants)
            variant.BranchOfficeStocks =
                [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 }];

        // Act
        var result = await _productManager.AddProduct(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.ProductAdded);
    }

    [Fact]
    public async Task AddProduct_ShouldSucceed_WhenNoStockRowsProvided()
    {
        // Bug #3: Hiç stok satırı girilmeden de ürün kaydedilebilmeli (stok sonra girilir).
        // Arrange
        var dto = BuildValidDto();
        foreach (var variant in dto.ProductVariants)
            variant.BranchOfficeStocks = [];

        // Act
        var result = await _productManager.AddProduct(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.ProductAdded);
    }

    [Fact]
    public async Task AddProduct_ShouldSucceed_WithValidDto()
    {
        // Arrange
        var dto = BuildValidDto();
        var mappedProduct = new Product { AttributeKeyValues = [] };

        // Act
        var result = await _productManager.AddProduct(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.ProductAdded);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddProduct_ShouldGenerateBarcode_WhenVariantBarcodeIsEmpty()
    {
        // Arrange
        var dto = BuildValidDto(barcode: string.Empty);

        _mockBarcodeService
            .Setup(b => b.GenerateAsync())
            .ReturnsAsync("9780000000001");

        // Act
        await _productManager.AddProduct(dto);

        // Assert
        _mockBarcodeService.Verify(b => b.GenerateAsync(), Times.Once);
    }

    [Fact]
    public async Task AddProduct_ShouldNotGenerateBarcode_WhenBarcodeAlreadySet()
    {
        // Arrange
        var dto = BuildValidDto(barcode: "9780000000001");

        // Act
        await _productManager.AddProduct(dto);

        // Assert
        _mockBarcodeService.Verify(b => b.GenerateAsync(), Times.Never);
    }

    [Fact]
    public async Task AddProduct_ShouldCallClearEmptyAttributes()
    {
        // Arrange
        var dto = BuildValidDto();

        // Act
        await _productManager.AddProduct(dto);

        // Assert
        _mockAttributeKeyValueManager.Verify(
            a => a.ClearEmptyAttributes(It.IsAny<Product>()),
            Times.Once);
    }

    // SKIPPED: AddProduct_FlagEnabled_AddsDomainEventToOutbox
    // IntegrationDbContext + InMemory provider çakışıyor: NpgsqlTsVector gibi PostgreSQL-spesifik
    // tipler InMemory adapter tarafından desteklenmiyor. Tam uçtan uca doğrulama Phase 2'nin
    // DomainEventPipelineTests entegrasyon testine (Testcontainers + gerçek PostgreSQL) bırakıldı.
    // Task 3.2 unit-test kapsamı: mevcut testler PublishEnabled=false (davranış değişmedi) kanıtlıyor.
}
