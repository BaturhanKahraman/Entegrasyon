using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Marketplace.Content;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CORE entegrasyon bug'ı (#19 devamı): marketplace sync servisleri ProductMarketplace
/// entity'sini LINQ-load (FirstOrDefault, AsTracking YOK) + mutate (Status/ExternalProductId/
/// BatchRequestId/LastSyncedAt =) + SaveChanges deseniyle global no-tracking altında SESSİZCE
/// persist ETMİYOR. Sonuç: her sync "hiç gönderilmemiş/Hatalı" gibi davranır (T120 kök sebebi).
///
/// Bu testler GERÇEK no-tracking IntegrationDbContext factory'sini (bug'ın kaynağı) kullanır;
/// dış pazaryeri API'leri mock'lanır (bug tamamen EF tracking davranışıyla ilgili). RED-first:
/// .AsTracking() fix'inden ÖNCE kırmızı (re-read eski değeri gösterir), sonra yeşil.
/// NOT: Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MarketplaceSyncPersistRegressionTests : IntegrationTestBase
{
    public MarketplaceSyncPersistRegressionTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IDbContextFactory<IntegrationDbContext> Factory =>
        Services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();

    private async Task<int> SeedProductMarketplaceAsync(
        Guid productId, int marketPlaceId, MarketplaceProductStatus status,
        string? externalProductId = null, string? batchRequestId = null)
    {
        using var db = CreateDbContext();
        var pm = new ProductMarketplace
        {
            ProductId = productId,
            MarketPlaceId = marketPlaceId,
            Status = status,
            ExternalProductId = externalProductId,
            BatchRequestId = batchRequestId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProductMarketplaces.Add(pm);
        await db.SaveChangesAsync();
        return pm.Id;
    }

    private async Task<ProductMarketplace> ReadProductMarketplaceAsync(Guid productId, int marketPlaceId)
    {
        using var db = CreateDbContext();
        return await db.ProductMarketplaces.AsNoTracking()
            .FirstAsync(x => x.ProductId == productId && x.MarketPlaceId == marketPlaceId);
    }

    private static Mock<IProductActivityLogger> ActivityLoggerMock() => new();

    // ─────────────────────────────────────────────────────────────────────
    // TrendyolProductService.DeleteProductAsync — pm.Status = Failed persist
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TrendyolProductService_DeleteProduct_persists_Failed_status()
    {
        const int trendyolId = 1;
        await SeedBasicEntitiesAsync(brandName: "TY Persist Marka", categoryName: "TY Persist Kategori");
        await SeedMarketPlaceAsync(trendyolId, "Trendyol");
        var (productId, _) = await SeedProductWithStockAsync("TY-DEL-001", stock: 5);
        await SeedProductMarketplaceAsync(productId, trendyolId, MarketplaceProductStatus.Published);

        var apiClient = new Mock<ITrendyolApiClient>();
        apiClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new TrendyolProductService(
            Factory,
            apiClient.Object,
            new Mock<ITrendyolProductMapper>().Object,
            new TrendyolMappingValidator(Factory),
            ActivityLoggerMock().Object,
            new Mock<IMarketplaceContentTransformer>().Object,
            new Mock<IMarketplaceContentRuleProvider>().Object,
            NullLogger<TrendyolProductService>.Instance);

        var result = await sut.DeleteProductAsync(productId);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, trendyolId);
        pm.Status.Should().Be(MarketplaceProductStatus.Failed,
            "silme sonrası Status DB'ye yazılmalı (no-tracking sessiz no-op olmamalı)");
        pm.StatusMessage.Should().Be("Trendyol'dan silindi");
    }

    // ─────────────────────────────────────────────────────────────────────
    // N11RestStockPriceService.UpdatePriceAsync — pm.BatchRequestId persist
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task N11RestStockPriceService_UpdatePrice_persists_BatchRequestId()
    {
        const int n11Id = 2;
        await SeedBasicEntitiesAsync(brandName: "N11 Price Marka", categoryName: "N11 Price Kategori");
        await SeedMarketPlaceAsync(n11Id, "N11");
        var (productId, _) = await SeedProductWithStockAsync("N11-PRICE-001", stock: 5);
        await SeedProductMarketplaceAsync(productId, n11Id, MarketplaceProductStatus.Pending);

        var restClient = new Mock<IN11RestClient>();
        restClient.Setup(c => c.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
                It.IsAny<string>(), It.IsAny<N11PriceStockUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new N11TaskResponse(888L, "PRICE_STOCK", "PROCESSING", null));

        var sut = new N11RestStockPriceService(
            restClient.Object, Factory, ActivityLoggerMock().Object,
            NullLogger<N11RestStockPriceService>.Instance);

        var result = await sut.UpdatePriceAsync(productId, 150m);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, n11Id);
        pm.BatchRequestId.Should().Be("888", "task id DB'ye yazılmalı (no-tracking sessiz no-op olmamalı)");
    }

    // ─────────────────────────────────────────────────────────────────────
    // N11RestProductService.UpdateProductBasicAsync — BatchRequestId/Status/LastSyncedAt
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task N11RestProductService_UpdateProductBasic_persists_sync_fields()
    {
        const int n11Id = 2;
        await SeedBasicEntitiesAsync(brandName: "N11 Upd Marka", categoryName: "N11 Upd Kategori");
        await SeedMarketPlaceAsync(n11Id, "N11");
        var (productId, _) = await SeedProductWithStockAsync("N11-UPD-001", stock: 5);
        await SeedProductMarketplaceAsync(productId, n11Id, MarketplaceProductStatus.Failed);

        var restClient = new Mock<IN11RestClient>();
        restClient.Setup(c => c.PostAsync<N11UpdateProductRequest, N11TaskResponse>(
                It.IsAny<string>(), It.IsAny<N11UpdateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new N11TaskResponse(777L, "UPDATE", "PROCESSING", null));

        var sut = new N11RestProductService(
            restClient.Object,
            new Mock<IN11SoapClient>().Object,
            new N11MappingValidator(Factory),
            ActivityLoggerMock().Object,
            Factory,
            new Mock<IMinioFileStorage>().Object,
            NullLogger<N11RestProductService>.Instance);

        var result = await sut.UpdateProductBasicAsync(productId);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, n11Id);
        pm.BatchRequestId.Should().Be("777");
        pm.Status.Should().Be(MarketplaceProductStatus.Pending, "güncelleme sonrası Status Pending'e yazılmalı");
        pm.LastSyncedAt.Should().NotBeNull("LastSyncedAt DB'ye yazılmalı");
    }

    // ─────────────────────────────────────────────────────────────────────
    // N11ProductService (SOAP).StartSellingAsync — pm.Status = Published persist
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task N11ProductService_Soap_StartSelling_persists_Published_status()
    {
        const int n11Id = 2;
        await SeedBasicEntitiesAsync(brandName: "N11 Soap Marka", categoryName: "N11 Soap Kategori");
        await SeedMarketPlaceAsync(n11Id, "N11");
        var (productId, _) = await SeedProductWithStockAsync("N11-SOAP-001", stock: 5);
        await SeedProductMarketplaceAsync(productId, n11Id, MarketplaceProductStatus.Pending,
            externalProductId: "987654321");

        var soapClient = new Mock<IN11SoapClient>();
        soapClient.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(new XElement("StartSellingResponse",
                new XElement("result", new XElement("status", "success"))));

        var sut = new N11ProductService(
            soapClient.Object,
            new Mock<IN11ProductMapper>().Object,
            new N11MappingValidator(Factory),
            ActivityLoggerMock().Object,
            Factory,
            NullLogger<N11ProductService>.Instance);

        var result = await sut.StartSellingAsync(productId);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, n11Id);
        pm.Status.Should().Be(MarketplaceProductStatus.Published,
            "satış başlatma sonrası Status DB'ye yazılmalı");
    }

    // ─────────────────────────────────────────────────────────────────────
    // AmazonProductService.PublishProductAsync — BatchRequestId/ExternalProductId
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AmazonProductService_Publish_persists_external_id()
    {
        const int amazonId = 6;
        await SeedBasicEntitiesAsync(brandName: "AMZ Marka", categoryName: "AMZ Kategori");
        await SeedMarketPlaceAsync(amazonId, "Amazon");
        var (productId, _) = await SeedProductWithStockAsync("AMZ-001", stock: 5, stockCode: "SC-AMZ-001");
        await SeedProductMarketplaceAsync(productId, amazonId, MarketplaceProductStatus.Pending);

        var validator = new Mock<AmazonMappingValidator>(Factory, NullLogger<AmazonMappingValidator>.Instance);
        validator.Setup(v => v.ValidateProductMappingsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SuccessResult());

        var mapper = new Mock<IAmazonProductMapper>();
        mapper.Setup(m => m.MapProductAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingItem>(null!));

        var listing = new Mock<IAmazonListingService>();
        listing.Setup(l => l.PutListingItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AmazonListingItem>(),
                It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingSubmissionResponse>(
                new AmazonListingSubmissionResponse(null, "ACCEPTED", "SUB-AMZ-1", null)));

        var sut = new AmazonProductService(
            Factory, listing.Object, mapper.Object, validator.Object,
            ActivityLoggerMock().Object, NullLogger<AmazonProductService>.Instance);

        var result = await sut.PublishProductAsync(productId);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, amazonId);
        pm.ExternalProductId.Should().Be("SC-AMZ-001",
            "ACCEPTED sonrası ExternalProductId DB'ye yazılmalı");
        pm.BatchRequestId.Should().Be("SUB-AMZ-1");
    }

    // ─────────────────────────────────────────────────────────────────────
    // HepsiburadaProductService.PublishProductAsync — pm.BatchRequestId persist
    // ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HepsiburadaProductService_Publish_persists_tracking_id()
    {
        const int hbId = 3;
        await SeedBasicEntitiesAsync(brandName: "HB Marka", categoryName: "HB Kategori");
        await SeedMarketPlaceAsync(hbId, "Hepsiburada");
        var (productId, _) = await SeedProductWithStockAsync("HB-001", stock: 5);
        await SeedProductMarketplaceAsync(productId, hbId, MarketplaceProductStatus.Pending);

        var validator = new Mock<HepsiburadaMappingValidator>(Factory, NullLogger<HepsiburadaMappingValidator>.Instance);
        validator.Setup(v => v.ValidateProductMappingsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SuccessResult());

        var mapper = new Mock<IHepsiburadaProductMapper>();
        mapper.Setup(m => m.MapProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaProductItem>>(new List<HepsiburadaProductItem>()));

        var apiClient = new Mock<IHepsiburadaApiClient>();
        apiClient.Setup(c => c.PostMultipartJsonFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"code":0,"message":null,"data":{"trackingId":"TRK-HB-1"}}""",
                    Encoding.UTF8, "application/json")
            });

        var sut = new HepsiburadaProductService(
            Factory, apiClient.Object, mapper.Object, validator.Object,
            ActivityLoggerMock().Object, NullLogger<HepsiburadaProductService>.Instance);

        var result = await sut.PublishProductAsync(productId);
        result.Success.Should().BeTrue(result.Message);

        var pm = await ReadProductMarketplaceAsync(productId, hbId);
        pm.BatchRequestId.Should().Be("TRK-HB-1",
            "trackingId DB'ye yazılmalı (no-tracking sessiz no-op olmamalı)");
    }
}
