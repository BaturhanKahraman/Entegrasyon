using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiProductServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiProductService>> _loggerMock = new();

    private MockCiceksepetiProductService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task PublishProductAsync_Should_Return_Success_With_MockBatchId()
    {
        var sut = CreateSut();
        var result = await sut.PublishProductAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
        result.Data.Should().StartWith("mock-batch-");
    }

    [Fact]
    public async Task UpdateProductAsync_Should_Return_Success_With_MockBatchId()
    {
        var sut = CreateSut();
        var result = await sut.UpdateProductAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
        result.Data.Should().StartWith("mock-batch-");
    }

    [Fact]
    public async Task CheckBatchStatusAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.CheckBatchStatusAsync("test-batch-123");
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.BatchId.Should().Be("test-batch-123");
    }

    [Fact]
    public async Task GetProductsAsync_Should_Return_Success_With_Empty_Products()
    {
        var sut = CreateSut();
        var result = await sut.GetProductsAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Products.Should().BeEmpty();
    }
}
