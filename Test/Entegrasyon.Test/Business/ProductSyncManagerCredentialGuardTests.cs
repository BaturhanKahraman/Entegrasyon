using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// ProductSyncManager credential guard testleri (spec 2026-06-11-marketplace-sync-credential-disabled).
/// API anahtarları eksik bir pazaryeri için sync tetiklenirse backend REDDETMELİ —
/// sadece UI disable yetmez (doğrudan POST ile bypass edilebilir).
/// </summary>
public class ProductSyncManagerCredentialGuardTests : BaseTest
{
    private readonly ProductSyncManager _sut;

    public ProductSyncManagerCredentialGuardTests()
    {
        _sut = new ProductSyncManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            new Mock<IProductActivityLogger>().Object,
            mockTenantContext.Object);
    }

    // Trendyol: ApiKey+ApiSecret dolu ama SellerId boş → credential eksik
    private static MarketPlace IncompleteTrendyol() =>
        new() { Id = 1, Name = "Trendyol", ApiKey = "k", ApiSecret = "s", SellerId = "" };

    [Fact]
    public async Task SyncProductAsync_CredentialEksik_BasarisizDoner()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { IncompleteTrendyol() });

        var result = await _sut.SyncProductAsync(Guid.NewGuid(), 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllPendingAsync_CredentialEksik_BasarisizDoner()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { IncompleteTrendyol() });

        var result = await _sut.SyncAllPendingAsync(1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RetryAllFailedAsync_CredentialEksik_BasarisizDoner()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { IncompleteTrendyol() });

        var result = await _sut.RetryAllFailedAsync(1);

        result.Success.Should().BeFalse();
    }
}
