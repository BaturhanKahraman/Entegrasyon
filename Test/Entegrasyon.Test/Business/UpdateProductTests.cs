using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using FluentValidation;
using Microsoft.Extensions.Options;
using ProductMapper = Entegrasyon.Business.Mappers.ProductMapper;

namespace Entegrasyon.UnitTest.Business;

public class UpdateProductTests : BaseTest
{
    private readonly IProductService _productManager;
    private readonly ProductMapper _productMapper = new();
    private readonly Mock<IAttributeKeyValueManager> _mockAttributeKeyValueManager = new();
    private readonly Mock<IBarcodeService> _mockBarcodeService = new();
    private readonly Mock<IMinioFileStorage> _mockMinioFileStorage = new();
    private readonly Mock<IOptions<NotificationFeatureFlags>> _mockNotificationFlags = new();
    private readonly Mock<ICurrentUserContext> _mockCurrentUser = new();

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
            _mockCurrentUser.Object,
            BuildTenantMemoryCache()
        );
    }

    private static Entegrasyon.Business.Tenants.TenantMemoryCache BuildTenantMemoryCache()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(x => x.TenantId).Returns(1);
        return new Entegrasyon.Business.Tenants.TenantMemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            tenant.Object);
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
