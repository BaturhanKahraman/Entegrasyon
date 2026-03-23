using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiProductMapperTests
{
    private readonly Mock<ILogger<MockCiceksepetiProductMapper>> _loggerMock = new();

    private MockCiceksepetiProductMapper CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task MapToCreateRequestAsync_Should_Return_Success_With_Empty_Products()
    {
        var sut = CreateSut();
        var result = await sut.MapToCreateRequestAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task MapToUpdateRequestAsync_Should_Return_Success_With_Empty_Products()
    {
        var sut = CreateSut();
        var result = await sut.MapToUpdateRequestAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Products.Should().BeEmpty();
    }
}
