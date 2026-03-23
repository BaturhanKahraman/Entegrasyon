using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiInvoiceServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiInvoiceService>> _loggerMock = new();

    private MockCiceksepetiInvoiceService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task SendInvoiceAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiInvoiceRequest([]);
        var result = await sut.SendInvoiceAsync(request);
        result.Success.Should().BeTrue();
    }
}
