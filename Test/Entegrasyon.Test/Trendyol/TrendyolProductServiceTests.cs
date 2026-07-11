using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolProductService unit tests — verifies publish, batch status check,
/// unapproved update, and approved content update flows.
/// TrendyolMappingValidator is sealed so we use a real instance with mocked DB data.
/// </summary>
public class TrendyolProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private readonly Mock<ITrendyolProductMapper> _productMapperMock = new();
    private readonly TrendyolMappingValidator _validator;
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<TrendyolProductService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TrendyolProductServiceTests()
    {
        _validator = new TrendyolMappingValidator(mockContextFactory.Object);

        _activityLoggerMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<ProductActivityType>(),
                It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private TrendyolProductService CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _productMapperMock.Object,
        _validator,
        _activityLoggerMock.Object,
        new Entegrasyon.Business.Marketplace.Content.MarketplaceContentTransformer(),
        new Entegrasyon.Business.Marketplace.Content.MarketplaceContentRuleProvider(),
        _loggerMock.Object);

    private void SetupMarketPlace(string? sellerId = "12345")
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
    }

    private void SetupProductMarketplaces(List<ProductMarketplace>? pmList = null)
    {
        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(pmList ?? new List<ProductMarketplace>());
    }

    /// <summary>
    /// Sets up DB data so that the real TrendyolMappingValidator passes validation.
    /// </summary>
    private void SetupValidatorToPass()
    {
        var product = new Product
        {
            Id = TestProductId,
            Title = "Test Urun",
            CategoryId = 1,
            BrandId = 10
        };
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceCategoryId = 100, IsActive = true }
        });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches).ReturnsDbSet(new List<BrandMarketPlaceMatch>
        {
            new() { ApplicationBrandId = 10, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceBrandId = 200 }
        });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());
    }

    /// <summary>
    /// Sets up DB data so that the real TrendyolMappingValidator fails validation (no category match).
    /// </summary>
    private void SetupValidatorToFail()
    {
        var product = new Product
        {
            Id = TestProductId,
            Title = "Test Urun",
            CategoryId = 1,
            BrandId = 10
        };
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        // No category match -> validation fails
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches).ReturnsDbSet(new List<BrandMarketPlaceMatch>
        {
            new() { ApplicationBrandId = 10, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceBrandId = 200 }
        });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());
    }

    private static TrendyolCreateProductRequest CreateDummyRequest() => new(
    [
        new TrendyolProductItem("BARCODE1", "Test Product", "main-1", 100, 200, 100m, 80m, 20, "BARCODE1", 1m, "desc", 10,
            [new TrendyolProductImage("https://img.example.com/1.jpg")],
            [new TrendyolProductAttribute(1, 2, null)])
    ]);

    // ── Test 1: PublishProductAsync — happy path ──

    [Fact]
    public async Task PublishProductAsync_HappyPath_ReturnsBatchRequestId()
    {
        // Arrange
        SetupValidatorToPass();

        var request = CreateDummyRequest();
        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<TrendyolCreateProductRequest>(request));

        SetupMarketPlace("12345");
        SetupProductMarketplaces();

        var batchResponse = new TrendyolBatchResponse("batch-123");
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(batchResponse)
            });

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("batch-123");
    }

    // ── Test 2: PublishProductAsync — validation fails ──

    [Fact]
    public async Task PublishProductAsync_WhenValidationFails_ReturnsError()
    {
        // Arrange
        SetupValidatorToFail();
        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategorisi");
        _productMapperMock.Verify(m => m.MapProductAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ── Test 3: PublishProductAsync — mapper fails ──

    [Fact]
    public async Task PublishProductAsync_WhenMapperFails_ReturnsError()
    {
        // Arrange
        SetupValidatorToPass();

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Marka eslestirmesi yok"));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Marka eslestirmesi yok");
    }

    // ── Test 4: PublishProductAsync — SellerId missing ──

    [Fact]
    public async Task PublishProductAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupValidatorToPass();

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<TrendyolCreateProductRequest>(CreateDummyRequest()));

        SetupMarketPlace(sellerId: null);

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    // ── Test 5: PublishProductAsync — API error response ──

    [Fact]
    public async Task PublishProductAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupValidatorToPass();

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<TrendyolCreateProductRequest>(CreateDummyRequest()));

        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad request body")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("BadRequest");
    }

    // ── Test 6: CheckBatchStatusAsync — happy path ──

    [Fact]
    public async Task CheckBatchStatusAsync_HappyPath_ReturnsStatusResponse()
    {
        // Arrange
        SetupMarketPlace("12345");

        var statusResponse = new TrendyolBatchStatusResponse("batch-123", TrendyolBatchStatus.COMPLETED, null, 1, 0, null, null, null);
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(statusResponse)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.CheckBatchStatusAsync("batch-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.BatchRequestId.Should().Be("batch-123");
        result.Data.Status.Should().Be(TrendyolBatchStatus.COMPLETED);
    }

    // ── Test 7: CheckBatchStatusAsync — SellerId missing ──

    [Fact]
    public async Task CheckBatchStatusAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.CheckBatchStatusAsync("batch-123");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    // ── Test 8: CheckBatchStatusAsync — API error ──

    [Fact]
    public async Task CheckBatchStatusAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.CheckBatchStatusAsync("batch-123");

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── Test 9: UpdateUnapprovedProductAsync — happy path ──

    [Fact]
    public async Task UpdateUnapprovedProductAsync_HappyPath_UsesPostToUnapprovedBulkUpdate()
    {
        // Arrange
        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<TrendyolCreateProductRequest>(CreateDummyRequest()));

        SetupMarketPlace("12345");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((u, _) => capturedUrl = u)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateUnapprovedProductAsync(TestProductId);

        // Assert — Trendyol resmi dokümanı unapproved-bulk-update için POST ister (PUT değil).
        result.Success.Should().BeTrue();
        capturedUrl.Should().Contain("unapproved-bulk-update");
        _apiClientMock.Verify(c => c.PostAsync(
            It.Is<string>(u => u.Contains("unapproved-bulk-update")), It.IsAny<object>()), Times.Once);
        _apiClientMock.Verify(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    // ── Test 10: UpdateUnapprovedProductAsync — mapper fails ──

    [Fact]
    public async Task UpdateUnapprovedProductAsync_WhenMapperFails_ReturnsError()
    {
        // Arrange
        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Mapping hatasi"));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateUnapprovedProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── Test 11: UpdateApprovedContentAsync — happy path ──

    [Fact]
    public async Task UpdateApprovedContentAsync_HappyPath_UsesPostToContentBulkUpdate()
    {
        // Arrange
        SetupMarketPlace("12345");
        SetupProductMarketplaces(
        [
            new ProductMarketplace
            {
                ProductId = TestProductId,
                MarketPlaceId = TrendyolMarketPlaceId,
                ContentId = 99999
            }
        ]);

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<TrendyolCreateProductRequest>(CreateDummyRequest()));

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((u, _) => capturedUrl = u)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateApprovedContentAsync(TestProductId);

        // Assert — Trendyol resmi dokümanı content-bulk-update için POST ister (PUT değil).
        result.Success.Should().BeTrue();
        capturedUrl.Should().Contain("content-bulk-update");
        _apiClientMock.Verify(c => c.PostAsync(
            It.Is<string>(u => u.Contains("content-bulk-update")), It.IsAny<object>()), Times.Once);
        _apiClientMock.Verify(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    // ── Test 12: UpdateApprovedContentAsync — ContentId missing ──

    [Fact]
    public async Task UpdateApprovedContentAsync_WhenContentIdMissing_ReturnsError()
    {
        // Arrange
        SetupProductMarketplaces(
        [
            new ProductMarketplace
            {
                ProductId = TestProductId,
                MarketPlaceId = TrendyolMarketPlaceId,
                ContentId = null
            }
        ]);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateApprovedContentAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ContentId");
    }

    // ── Test 13: UpdateApprovedContentAsync — no ProductMarketplace record ──

    [Fact]
    public async Task UpdateApprovedContentAsync_WhenNoProductMarketplace_ReturnsError()
    {
        // Arrange
        SetupProductMarketplaces(new List<ProductMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateApprovedContentAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
    }
}
