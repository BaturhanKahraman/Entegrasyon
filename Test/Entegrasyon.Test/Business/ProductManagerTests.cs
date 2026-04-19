using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using ProductMapper = Entegrasyon.Business.Mappers.ProductMapper;

namespace Entegrasyon.UnitTest.Business;

public class ProductManagerTests : BaseTest
{
    private readonly IProductService _productManager;
    private readonly ProductMapper _productMapper = new();
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager = new();
    private readonly Mock<IAttributeKeyValueManager> _mockAttributeKeyValueManager = new();
    private readonly Mock<IBarcodeService> _mockBarcodeService = new();
    private readonly EventChannel<ProductAddedEvent> _productAddedChannel = new();
    private readonly EventChannel<ProductUpdatedEvent> _productUpdatedChannel = new();
    private readonly Mock<IMinioFileStorage> _mockMinioFileStorage = new();

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

        _productManager = new ProductManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _productMapper,
            MockValidator.Object,
            _mockOfficeStockManager.Object,
            _mockAttributeKeyValueManager.Object,
            _mockBarcodeService.Object,
            _productAddedChannel,
            _productUpdatedChannel,
            _mockMinioFileStorage.Object,
            mockTenantContext.Object,
            new VariantNamingService()
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
    public async Task AddProduct_ShouldReturnError_WhenAllStocksAreZero()
    {
        // Arrange
        var dto = BuildValidDto();
        _mockOfficeStockManager
            .Setup(s => s.CheckIfProductCountZero(It.IsAny<AddBranchOfficeStockDto[]>()))
            .Returns(new ErrorResult("Lütfen en az bir stok girin."));

        // Act
        var result = await _productManager.AddProduct(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Lütfen en az bir stok girin.");
    }

    [Fact]
    public async Task AddProduct_ShouldSucceed_WithValidDto()
    {
        // Arrange
        var dto = BuildValidDto();
        var mappedProduct = new Product { AttributeKeyValues = [] };

        _mockOfficeStockManager
            .Setup(s => s.CheckIfProductCountZero(It.IsAny<AddBranchOfficeStockDto[]>()))
            .Returns(new SuccessResult());

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

        _mockOfficeStockManager
            .Setup(s => s.CheckIfProductCountZero(It.IsAny<AddBranchOfficeStockDto[]>()))
            .Returns(new SuccessResult());
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
        _mockOfficeStockManager
            .Setup(s => s.CheckIfProductCountZero(It.IsAny<AddBranchOfficeStockDto[]>()))
            .Returns(new SuccessResult());

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

        _mockOfficeStockManager
            .Setup(s => s.CheckIfProductCountZero(It.IsAny<AddBranchOfficeStockDto[]>()))
            .Returns(new SuccessResult());

        // Act
        await _productManager.AddProduct(dto);

        // Assert
        _mockAttributeKeyValueManager.Verify(
            a => a.ClearEmptyAttributes(It.IsAny<Product>()),
            Times.Once);
    }
}
