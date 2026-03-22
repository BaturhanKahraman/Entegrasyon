using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Hepsiburada;

public class MockHepsiburadaProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaProductMapper> _mapperMock = new();
    private readonly Mock<HepsiburadaMappingValidator> _validatorMock;
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<MockHepsiburadaProductService>> _loggerMock = new();

    public MockHepsiburadaProductServiceTests()
    {
        _validatorMock = new Mock<HepsiburadaMappingValidator>(
            mockContextFactory.Object,
            new Mock<ILogger<HepsiburadaMappingValidator>>().Object);
    }

    private MockHepsiburadaProductService CreateSut() => new(
        mockContextFactory.Object,
        _mapperMock.Object,
        _validatorMock.Object,
        _activityLoggerMock.Object,
        _loggerMock.Object);

    [Fact]
    public async Task PublishProductAsync_Should_Return_MockTrackingId_On_Success()
    {
        var productId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mapperMock
            .Setup(m => m.MapProductAsync(productId))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaProductItem>>(
                new List<HepsiburadaProductItem>
                {
                    new(123, "merchant-uuid", new Dictionary<string, object> { ["merchantSku"] = "TEST-SKU" })
                }));

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeTrue();
        result.Data.Should().StartWith("mock-tracking-");
    }

    [Fact]
    public async Task PublishProductAsync_Should_Return_Error_When_Validation_Fails()
    {
        var productId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new ErrorResult("Kategori eşleşmesi yok"));

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kategori eşleşmesi yok");
    }

    [Fact]
    public async Task CheckProductStatusAsync_Should_Return_CREATED_Status()
    {
        var sut = CreateSut();
        var result = await sut.CheckProductStatusAsync("mock-tracking-12345");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].ProductStatus.Should().Be("CREATED");
        result.Data[0].ImportStatus.Should().Be("SUCCESS");
    }

    [Fact]
    public async Task ApprovePreMatchAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.ApprovePreMatchAsync("TEST-SKU");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("MOCK");
    }
}
