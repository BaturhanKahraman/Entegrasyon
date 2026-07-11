using System.Net;
using System.Reflection;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolProductStatusSyncService iki adımlı durum sorgusu (products/approved → products/unapproved)
/// davranış testleri. Moq'lanmış IntegrationDbContext + ITrendyolApiClient ile PollForTenantAsync
/// reflection ile tetiklenir; ProductMarketplace mutasyonu (aynı referans üzerinden) doğrulanır.
/// (Persist doğrulaması IntegrationTest'te — burada akış/orkestrasyon davranışı test edilir.)
/// </summary>
public sealed class TrendyolProductStatusSyncServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private static readonly Guid ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string Barcode = "BC-SYNC-1";

    private ProductMarketplace SetupTrackedProduct(
        bool? isApproved = false,
        MarketplaceProductStatus status = MarketplaceProductStatus.Published)
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = "12345" };

        var product = new Product
        {
            Id = ProductId,
            Title = "Sync Test",
            StockCode = "SYNC",
            ProductVariants =
            [
                new ProductVariant { Id = Guid.NewGuid(), Barcode = Barcode }
            ]
        };

        var pm = new ProductMarketplace
        {
            Id = 1,
            ProductId = ProductId,
            MarketPlaceId = TrendyolMarketPlaceId,
            Status = status,
            IsApproved = isApproved,
            Product = product
        };

        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace> { pm });
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return pm;
    }

    private TrendyolProductStatusSyncService CreateSut() => new(
        Mock.Of<IServiceScopeFactory>(),
        Mock.Of<ITenantRegistry>(),
        NullLogger<TrendyolProductStatusSyncService>.Instance,
        Options.Create(new NotificationFeatureFlags())); // PublishEnabled=false → domain event yok

    private void SetupApiResponse(string urlFragment, string json) =>
        _apiClientMock
            .Setup(c => c.GetAsync(It.Is<string>(u => u.Contains(urlFragment))))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

    private async Task InvokePollAsync(TrendyolProductStatusSyncService sut)
    {
        var provider = new StubServiceProvider(new()
        {
            [typeof(IntegrationDbContext)] = mockIntegrationDbContext.Object,
            [typeof(ITrendyolApiClient)] = _apiClientMock.Object,
            [typeof(IProductActivityLogger)] = ActivityLoggerStub()
        });

        var method = typeof(TrendyolProductStatusSyncService).GetMethod(
            "PollForTenantAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task)method.Invoke(sut,
            [provider, 1, DateTimeOffset.UtcNow.AddMinutes(-10), CancellationToken.None])!;
        await task;
    }

    // ── Senaryo 1: Barkod ONAYLI listede → unapproved endpoint HİÇ çağrılmaz ──
    [Fact]
    public async Task Poll_WhenBarcodeInApprovedList_SetsApprovedAndSkipsUnapprovedCall()
    {
        var pm = SetupTrackedProduct(isApproved: false, status: MarketplaceProductStatus.Published);

        SetupApiResponse("/products/approved", $$"""
        {
          "totalElements": 1, "totalPages": 1, "page": 0, "size": 1,
          "content": [
            {
              "contentId": 123, "productMainId": "PM-1", "title": "Sync Test",
              "variants": [ { "barcode": "{{Barcode}}", "archived": true, "stockCode": "SYNC" } ]
            }
          ]
        }
        """);
        // Onaysız endpoint çağrılırsa bilmek için bir stub bırak (çağrılmamalı).
        SetupApiResponse("/products/unapproved", """{ "totalElements":0,"totalPages":0,"page":0,"size":0,"content":[] }""");

        await InvokePollAsync(CreateSut());

        pm.IsApproved.Should().BeTrue("barkod onaylı listede bulundu");
        pm.IsArchived.Should().BeTrue("onaylı variant archived=true");
        pm.ContentId.Should().Be(123);
        pm.Status.Should().Be(MarketplaceProductStatus.Published);

        _apiClientMock.Verify(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/approved"))), Times.Once);
        _apiClientMock.Verify(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/unapproved"))), Times.Never);
    }

    // ── Senaryo 2: Onaylı'da yok, onaysız'da rejectReasonDetails dolu → Rejected + StatusMessage ──
    [Fact]
    public async Task Poll_WhenRejectedInUnapprovedList_SetsRejectedWithStatusMessage()
    {
        var pm = SetupTrackedProduct(isApproved: false, status: MarketplaceProductStatus.Published);

        SetupApiResponse("/products/approved", """{ "totalElements":0,"totalPages":0,"page":0,"size":0,"content":[] }""");
        SetupApiResponse("/products/unapproved", $$"""
        {
          "totalElements": 1, "totalPages": 1, "page": 0, "size": 1,
          "content": [
            {
              "barcode": "{{Barcode}}", "productMainId": "PM-1", "stockCode": "SYNC", "title": "Sync Test",
              "rejectReasonDetails": [
                { "reason": "Görsel hatası", "detailedReason": "Ürün görseli Trendyol standartlarına uymuyor" }
              ]
            }
          ]
        }
        """);

        await InvokePollAsync(CreateSut());

        pm.Status.Should().Be(MarketplaceProductStatus.Rejected);
        pm.StatusMessage.Should().Be("Ürün görseli Trendyol standartlarına uymuyor");
        pm.IsApproved.Should().Be(false);

        _apiClientMock.Verify(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/approved"))), Times.Once);
        _apiClientMock.Verify(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/unapproved"))), Times.Once);
    }

    private static IProductActivityLogger ActivityLoggerStub()
    {
        var m = new Mock<IProductActivityLogger>();
        m.Setup(x => x.LogAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityType>(),
                It.IsAny<string>(), It.IsAny<ProductActivityStatus>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return m.Object;
    }

    private sealed class StubServiceProvider(Dictionary<Type, object> map) : IServiceProvider
    {
        public object? GetService(Type serviceType) => map.GetValueOrDefault(serviceType);
    }
}
