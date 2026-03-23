using Entegrasyon.Business.Concrete.Hepsiburada;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Hepsiburada;

public class MockHepsiburadaClaimServiceTests
{
    private readonly Mock<ILogger<MockHepsiburadaClaimService>> _loggerMock = new();

    private MockHepsiburadaClaimService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetClaimsAsync_Should_Return_Success_With_Empty_List()
    {
        var sut = CreateSut();
        var result = await sut.GetClaimsAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptClaimAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.AcceptClaimAsync("CLM-001");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RejectClaimAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.RejectClaimAsync("CLM-001", "Ürün hasarlı değil");
        result.Success.Should().BeTrue();
    }
}
