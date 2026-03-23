using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaClaimService unit testleri.
/// Iade/degisim talebi listeleme, kabul ve red islemleri test edilir.
/// </summary>
public class HepsiburadaClaimServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<HepsiburadaClaimService>> _mockLogger = new();

    public HepsiburadaClaimServiceTests()
    {
        var marketplace = new Entity.MarketPlace
        {
            Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
            SellerId = "test-merchant-id"
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace> { marketplace });
    }

    private HepsiburadaClaimService CreateSut() => new(
        mockContextFactory.Object,
        _mockApiClient.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1: GetClaimsAsync — success without filter ─────────────────────

    [Fact]
    public async Task GetClaimsAsync_NoFilter_ReturnsClaims()
    {
        var claims = new List<HepsiburadaClaimDto>
        {
            new("CLM-001", "ORD-001", "WAITING", "RETURN", "Defective", null),
            new("CLM-002", "ORD-002", "APPROVED", "EXCHANGE", "Wrong size", null)
        };

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("/claims/merchantId/"))))
            .ReturnsAsync(CreateJsonResponse(claims));

        var sut = CreateSut();
        var result = await sut.GetClaimsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].ClaimNumber.Should().Be("CLM-001");
    }

    // ── Test 2: GetClaimsAsync — with status filter ─────────────────────────

    [Fact]
    public async Task GetClaimsAsync_WithStatusFilter_BuildsCorrectUrl()
    {
        string? capturedUrl = null;

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new List<HepsiburadaClaimDto>()));

        var sut = CreateSut();
        await sut.GetClaimsAsync(status: "WAITING");

        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("/status/WAITING");
    }

    // ── Test 3: GetClaimsAsync — API error ──────────────────────────────────

    [Fact]
    public async Task GetClaimsAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.GetClaimsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Claim API hatası");
    }

    // ── Test 4: AcceptClaimAsync — success ──────────────────────────────────

    [Fact]
    public async Task AcceptClaimAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/claims/number/CLM-001/accept")),
                It.IsAny<object>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.AcceptClaimAsync("CLM-001");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("kabul edildi");
    }

    // ── Test 5: AcceptClaimAsync — API error ────────────────────────────────

    [Fact]
    public async Task AcceptClaimAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.AcceptClaimAsync("CLM-999");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Talep kabul hatası");
    }

    // ── Test 6: RejectClaimAsync — success ──────────────────────────────────

    [Fact]
    public async Task RejectClaimAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/claims/number/CLM-001/reject")),
                It.IsAny<object>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.RejectClaimAsync("CLM-001", "Urun hasarsiz");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reddedildi");
    }

    // ── Test 7: RejectClaimAsync — API error ────────────────────────────────

    [Fact]
    public async Task RejectClaimAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.RejectClaimAsync("CLM-999", "Reason");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Talep red hatası");
    }

    // ── Test 8: GetClaimsAsync — exception handling ─────────────────────────

    [Fact]
    public async Task GetClaimsAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var sut = CreateSut();
        var result = await sut.GetClaimsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Timeout");
    }

    // ── Test 9: AcceptClaimAsync — exception handling ───────────────────────

    [Fact]
    public async Task AcceptClaimAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var result = await sut.AcceptClaimAsync("CLM-001");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }
}
