using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol.EFatura;
using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Orders;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolEFaturaService unit testleri.
/// Mukellef sorgulama, fatura olusturma, durum sorgulama, iptal, PDF URL.
/// </summary>
public class TrendyolEFaturaServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolEFaturaApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<TrendyolEFaturaService>> _mockLogger = new();
    private readonly TrendyolEFaturaInvoiceBuilder _invoiceBuilder = new();

    private TrendyolEFaturaService CreateSut() => new(
        mockContextFactory.Object,
        _mockApiClient.Object,
        _invoiceBuilder,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    // ── CheckTaxPayerAsync Tests ─────────────────────────────────────────

    [Fact]
    public async Task CheckTaxPayerAsync_TaxPayerFound_ReturnsTrue()
    {
        // Arrange
        var taxpayers = new[] { new { taxId = "8003199330", alias = "urn:mail:test@test.com", title = "Test Firma", aliasType = "INVOICE" } };
        var json = JsonSerializer.Serialize(taxpayers);
        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("taxpayers/8003199330")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.CheckTaxPayerAsync("8003199330");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task CheckTaxPayerAsync_TaxPayerNotFound_ReturnsFalse()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("taxpayers/")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.CheckTaxPayerAsync("12345678901");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CheckTaxPayerAsync_ApiError_ReturnsFalseGracefully()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateSut();

        // Act
        var result = await sut.CheckTaxPayerAsync("9999999999");

        // Assert
        result.Success.Should().BeTrue(); // Graceful degradation
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CheckTaxPayerAsync_DespatchAlias_ReturnsFalse()
    {
        // Arrange — aliasType = DESPATCH_ADVICE (irsaliye mukellefiyse, fatura degil)
        var taxpayers = new[] { new { taxId = "1111111111", alias = "urn:mail:test@test.com", title = "Test", aliasType = "DESPATCH_ADVICE" } };
        var json = JsonSerializer.Serialize(taxpayers);
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.CheckTaxPayerAsync("1111111111");

        // Assert
        result.Data.Should().BeFalse();
    }

    // ── CancelInvoiceAsync Tests ─────────────────────────────────────────

    [Fact]
    public async Task CancelInvoiceAsync_RecordNotFound_ReturnsError()
    {
        // Arrange — DbSet returns empty
        var records = new List<EFaturaRecord>();
        mockIntegrationDbContext.Setup(x => x.Set<EFaturaRecord>())
            .ReturnsDbSet(records);

        var sut = CreateSut();

        // Act
        var result = await sut.CancelInvoiceAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CancelInvoiceAsync_EInvoiceType_ReturnsError()
    {
        // Arrange — e-fatura tipi (e-arsiv degil)
        var recordId = Guid.NewGuid();
        var records = new List<EFaturaRecord>
        {
            new()
            {
                Id = recordId,
                InvoiceType = EFaturaType.EInvoice,
                InvoiceUuid = "test-uuid",
                Status = EFaturaStatus.Approved
            }
        };
        mockIntegrationDbContext.Setup(x => x.Set<EFaturaRecord>())
            .ReturnsDbSet(records);

        var sut = CreateSut();

        // Act
        var result = await sut.CancelInvoiceAsync(recordId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("e-arsiv");
    }

    // ── MapApiStatus Tests (tested through CheckInvoiceStatusAsync) ──────

    [Theory]
    [InlineData(10, EFaturaStatus.Processing)]
    [InlineData(20, EFaturaStatus.Processing)]
    [InlineData(29, EFaturaStatus.Error)]
    [InlineData(30, EFaturaStatus.Created)]
    [InlineData(40, EFaturaStatus.Sent)]
    [InlineData(205, EFaturaStatus.Approved)]
    [InlineData(305, EFaturaStatus.Cancelled)]
    [InlineData(405, EFaturaStatus.Error)]
    public void MapApiStatus_MapsCorrectly(int apiStatus, EFaturaStatus expectedStatus)
    {
        // MapApiStatus is private, test via reflection
        var method = typeof(TrendyolEFaturaService)
            .GetMethod("MapApiStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("MapApiStatus should exist as a static private method");

        var result = (EFaturaStatus)method!.Invoke(null, [apiStatus])!;
        result.Should().Be(expectedStatus);
    }
}
