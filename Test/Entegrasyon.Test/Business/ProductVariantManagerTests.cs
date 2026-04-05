using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.UnitTest.Business;

public class ProductVariantManagerTests : BaseTest
{
    private readonly IProductVariantManager _sut;
    private readonly Mock<IBarcodeService> _mockBarcodeService = new();
    private readonly Mock<IImageManager> _mockImageManager = new();
    private readonly EventChannel<ProductUpdatedEvent> _productUpdatedChannel = new();
    private readonly List<ProductVariant> _variants;

    public ProductVariantManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<EditProductVariantDto>()))
            .Returns(Task.CompletedTask);
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddProductVariantDto>()))
            .Returns(Task.CompletedTask);

        _mockImageManager
            .Setup(m => m.SoftDeleteVariantImages(It.IsAny<Guid>()))
            .ReturnsAsync(new SuccessResult("OK"));

        _variants = new List<ProductVariant>
        {
            new()
            {
                Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                ProductId = Guid.Parse("bbbb2222-2222-2222-2222-222222222222"),
                Product = new Product { Id = Guid.Parse("bbbb2222-2222-2222-2222-222222222222"), Title = "Test" },
                Barcode = "TEST001",
                ListPrice = 100m,
                SalePrice = 80m,
                CostPrice = 50m,
                ECommercePrice = 85m,
                VatRate = 20m,
                DimensionalWeight = 1.5m,
                CurrencyType = "TRY",
                IsDeleted = false
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(_variants);
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new ProductVariantManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object,
            _mockBarcodeService.Object,
            _mockImageManager.Object,
            _productUpdatedChannel,
            mockTenantContext.Object
        );
    }

    // ── SoftDeleteVariant ────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteVariant_WhenVariantExists_ShouldSetIsDeletedTrue()
    {
        // Arrange
        var variantId = _variants[0].Id;

        // Act
        var result = await _sut.SoftDeleteVariant(variantId);

        // Assert
        result.Success.Should().BeTrue();
        _variants[0].IsDeleted.Should().BeTrue();
        _variants[0].DeletedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task SoftDeleteVariant_WhenVariantExists_ShouldCallImageCleanup()
    {
        // Arrange
        var variantId = _variants[0].Id;

        // Act
        await _sut.SoftDeleteVariant(variantId);

        // Assert
        _mockImageManager.Verify(m => m.SoftDeleteVariantImages(variantId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteVariant_WhenVariantNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.SoftDeleteVariant(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── UpdateVariant ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateVariant_WhenValid_ShouldUpdateFieldsAndReturnSuccess()
    {
        // Arrange
        var dto = new EditProductVariantDto(
            Id: _variants[0].Id,
            ListPrice: 200m,
            SalePrice: 150m,
            CostPrice: 100m,
            ECommercePrice: 160m,
            DimensionalWeight: 2.0m,
            VatRate: 18m,
            CurrencyType: "TRY"
        );

        // Act
        var result = await _sut.UpdateVariant(dto);

        // Assert
        result.Success.Should().BeTrue();
        _variants[0].ListPrice.Should().Be(200m);
        _variants[0].SalePrice.Should().Be(150m);
        _variants[0].CostPrice.Should().Be(100m);
        _variants[0].VatRate.Should().Be(18m);
    }

    [Fact]
    public async Task UpdateVariant_WhenVariantNotFound_ShouldReturnError()
    {
        // Arrange
        var dto = new EditProductVariantDto(
            Id: Guid.NewGuid(),
            ListPrice: 200m,
            SalePrice: 150m,
            CostPrice: 100m,
            ECommercePrice: 160m,
            DimensionalWeight: 2.0m,
            VatRate: 18m,
            CurrencyType: "TRY"
        );

        // Act
        var result = await _sut.UpdateVariant(dto);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── AddVariant ───────────────────────────────────────────────────

    [Fact]
    public async Task AddVariant_WhenProductNotFound_ShouldReturnError()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        var dto = new AddProductVariantDto
        {
            ListPrice = 100m,
            SalePrice = 80m,
            CostPrice = 50m,
            VatRate = 18m,
            DimensionalWeight = 1m,
            CurrencyType = "TRY",
            BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
        };

        // Act
        var result = await _sut.AddVariant(Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
    }
}
