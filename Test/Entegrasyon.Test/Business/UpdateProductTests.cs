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
            mockContextFactory.Object,
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

        var dto = BuildValidEditDto(existingId);

        var result = await _productManager.UpdateProduct(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("stok kodu");
    }
}
