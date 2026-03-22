using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// PazaramaProductService ve MockPazaramaProductService için birim testleri.
/// Validate → Map → API pipeline'ının doğru çalıştığını ve hata durumlarında
/// beklenen sonuçların döndürüldüğünü doğrular.
/// </summary>
public class PazaramaProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaProductService>> _mockLoggerMock = new();

    private MockPazaramaProductService CreateMockSut() =>
        new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_PublishProductAsync_Should_Return_Success_With_MockBatchId()
    {
        var sut = CreateMockSut();
        var result = await sut.PublishProductAsync(Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().StartWith("mock-pazarama-batch-");
    }

    [Fact]
    public async Task MockService_CheckBatchStatusAsync_Should_Return_Status2_Done()
    {
        var sut = CreateMockSut();
        var batchId = "test-batch-id-123";

        var result = await sut.CheckBatchStatusAsync(batchId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(2);
        result.Data.BatchRequestId.Should().Be(batchId);
    }

    [Fact]
    public async Task MockService_PublishProductAsync_Should_Return_Different_Ids_For_Different_Products()
    {
        var sut = CreateMockSut();

        var result1 = await sut.PublishProductAsync(Guid.NewGuid());
        var result2 = await sut.PublishProductAsync(Guid.NewGuid());

        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result1.Data.Should().NotBe(result2.Data);
    }

    [Fact]
    public async Task MockService_CheckBatchStatusAsync_Should_Reflect_Given_BatchRequestId()
    {
        var sut = CreateMockSut();
        var batchId = "some-specific-batch-id";

        var result = await sut.CheckBatchStatusAsync(batchId);

        result.Data!.BatchRequestId.Should().Be(batchId);
        result.Data.SuccessfulCount.Should().Be(1);
        result.Data.TotalCount.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // Real service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<IPazaramaProductMapper> _productMapperMock = new();
    private readonly Mock<PazaramaMappingValidator> _validatorMock;
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<PazaramaProductService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string TestBatchRequestId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    public PazaramaProductServiceTests()
    {
        _validatorMock = new Mock<PazaramaMappingValidator>(mockContextFactory.Object);

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

    private PazaramaProductService CreateRealSut() => new(
        _apiClientMock.Object,
        _productMapperMock.Object,
        _validatorMock.Object,
        _activityLoggerMock.Object,
        _loggerMock.Object);

    private static HttpResponseMessage BuildSuccessPublishResponse(string batchRequestId = TestBatchRequestId)
    {
        var body = JsonSerializer.Serialize(new
        {
            data = new { batchRequestId },
            success = true
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildErrorHttpResponse(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"success\":false,\"message\":\"hata\"}", Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildBatchStatusResponse(string batchRequestId, int statusCode = 2)
    {
        var body = JsonSerializer.Serialize(new
        {
            data = new
            {
                status = statusCode,
                batchRequestId,
                totalCount = 1,
                successfulCount = 1,
                isExcel = false,
                failedCount = 0
            },
            success = true
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task PublishProductAsync_WhenValidationFails_ShouldReturnError()
    {
        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new ErrorResult("Kategori eşleştirilmemiş."));

        var sut = CreateRealSut();
        var result = await sut.PublishProductAsync(TestProductId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kategori");

        _productMapperMock.Verify(m => m.MapProductAsync(It.IsAny<Guid>()), Times.Never);
        _apiClientMock.Verify(a => a.PostAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task PublishProductAsync_WhenMappingFails_ShouldReturnError()
    {
        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Mapping hatası"));

        var sut = CreateRealSut();
        var result = await sut.PublishProductAsync(TestProductId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Mapping hatası");

        _apiClientMock.Verify(a => a.PostAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task PublishProductAsync_WhenApiCallSucceeds_ShouldReturnBatchRequestId()
    {
        var fakeRequest = new PazaramaCreateProductRequest([]);

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<PazaramaCreateProductRequest>(fakeRequest));

        _apiClientMock
            .Setup(a => a.PostAsync("product/create", fakeRequest))
            .ReturnsAsync(BuildSuccessPublishResponse(TestBatchRequestId));

        var sut = CreateRealSut();
        var result = await sut.PublishProductAsync(TestProductId);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(TestBatchRequestId);
    }

    [Fact]
    public async Task PublishProductAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        var fakeRequest = new PazaramaCreateProductRequest([]);

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<PazaramaCreateProductRequest>(fakeRequest));

        _apiClientMock
            .Setup(a => a.PostAsync("product/create", fakeRequest))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateRealSut();
        var result = await sut.PublishProductAsync(TestProductId);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishProductAsync_ShouldLogActivity_AfterValidation()
    {
        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new ErrorDataResult<PazaramaCreateProductRequest>(null!, "hata"));

        var sut = CreateRealSut();
        await sut.PublishProductAsync(TestProductId);

        _activityLoggerMock.Verify(
            x => x.LogAsync(
                TestProductId,
                ProductActivityType.MappingValidated,
                It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckBatchStatusAsync_WhenApiSucceeds_ShouldReturnBatchStatus()
    {
        _apiClientMock
            .Setup(a => a.GetAsync($"product/getProductBatchResult?BatchRequestId={TestBatchRequestId}"))
            .ReturnsAsync(BuildBatchStatusResponse(TestBatchRequestId, 2));

        var sut = CreateRealSut();
        var result = await sut.CheckBatchStatusAsync(TestBatchRequestId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(2);
        result.Data.BatchRequestId.Should().Be(TestBatchRequestId);
    }

    [Fact]
    public async Task CheckBatchStatusAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.NotFound));

        var sut = CreateRealSut();
        var result = await sut.CheckBatchStatusAsync(TestBatchRequestId);

        result.Success.Should().BeFalse();
    }
}
