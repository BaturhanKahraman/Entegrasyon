using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// T120 / #19 EF no-tracking persist footgun — STATUS POLLING background servisleri.
/// Global default NoTracking (TenantDbContextFactory) yüzünden bu servisler durum entity'sini
/// (ProductMarketplace / EFaturaRecord) LINQ ile (Where/FirstOrDefault, AsTracking YOK) yükleyip
/// mutate edip SaveChangesAsync çağırınca değişiklik SESSİZCE persist ETMEZ → gönderim sonrası
/// durum reconcile edilmez (Trendyol batch COMPLETED olsa bile UI sonsuza "Hatalı"/Pending kalır).
///
/// Her test servisin protected PollForTenantAsync metodunu izole bir IServiceProvider ile (gerçek
/// IntegrationDbContext + Moq'lanmış pazaryeri API servisi) reflection ile tetikler, sonra DB'den
/// tekrar okuyup durumun GERÇEKTEN yazıldığını doğrular. RED-first: .AsTracking() fix'inden ÖNCE
/// kırmızı (no-op → eski durum), fix sonrası yeşil.
///
/// Diğer agent'ın NoTrackingPersistRegressionTests.cs dosyasından AYRI tutulur (dosya çakışması yok).
/// NOT: Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StatusPollingPersistRegressionTests : IntegrationTestBase
{
    public StatusPollingPersistRegressionTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static readonly DateTimeOffset StaleLastPoll = DateTimeOffset.UtcNow.AddDays(-1);

    // ─────────────────────────────────────────────────────────────────────────
    // Trendyol batch — T120'nin tam senaryosu: COMPLETED + 0 hata → Published
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TrendyolBatch_completed_batch_persists_Published_status()
    {
        var (productId, _) = await SeedProductAsync("TY-BATCH");
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol");
        var pmId = await SeedProductMarketplaceAsync(productId, TrendyolMarketPlaceId,
            MarketplaceProductStatus.Pending, batchRequestId: "batch-ty-1");

        var trendyol = new Mock<ITrendyolProductService>();
        trendyol.Setup(s => s.CheckBatchStatusAsync("batch-ty-1"))
            .ReturnsAsync(new DataResult<TrendyolBatchStatusResponse>(
                new TrendyolBatchStatusResponse("batch-ty-1", TrendyolBatchStatus.COMPLETED,
                    Items: [], ItemCount: 1, FailedItemCount: 0,
                    BatchRequestType: null, CreationDate: null, LastModification: null),
                success: true));

        await using var ctx = CreateDbContext();
        var sut = new TrendyolBatchStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<TrendyolBatchStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(ITrendyolProductService), trendyol.Object),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        var pm = await ReadPmAsync(pmId);
        pm.Status.Should().Be(MarketplaceProductStatus.Published,
            "Trendyol batch COMPLETED → ProductMarketplace.Status DB'ye yazılmalı (T120 no-op olmamalı)");
        pm.LastSyncedAt.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Çiçeksepeti / N11 / Hepsiburada / Pazarama / PttAVM / Amazon listing —
    // timeout dalı (yerel mutasyon, dış API çağrısı yok) → Failed persist olmalı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task CiceksepetiBatch_timeout_persists_Failed_status()
    {
        var pmId = await SeedStalePendingAsync(CiceksepetiMarketPlaceId, "CS", batchRequestId: "cs-1");

        await using var ctx = CreateDbContext();
        var sut = new CiceksepetiBatchStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<CiceksepetiBatchStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(ICiceksepetiProductService), Mock.Of<ICiceksepetiProductService>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    [Fact]
    public async Task N11Task_timeout_persists_Failed_status()
    {
        var pmId = await SeedStalePendingAsync(N11MarketPlaceId, "N11", batchRequestId: "777");

        await using var ctx = CreateDbContext();
        var sut = new N11TaskPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<N11TaskPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(IN11RestClient), Mock.Of<IN11RestClient>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    [Fact]
    public async Task PazaramaBatch_timeout_persists_Failed_status()
    {
        var pmId = await SeedStalePendingAsync(PazaramaMarketPlaceId, "PZ", batchRequestId: "pz-1");

        await using var ctx = CreateDbContext();
        var sut = new PazaramaBatchStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<PazaramaBatchStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(IPazaramaProductService), Mock.Of<IPazaramaProductService>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    [Fact]
    public async Task PttavmTracking_timeout_persists_Failed_status()
    {
        var pmId = await SeedStalePendingAsync(PttavmMarketPlaceId, "PTT", batchRequestId: "ptt-1");

        await using var ctx = CreateDbContext();
        var sut = new PttavmProductTrackingPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<PttavmProductTrackingPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(IPttavmProductService), Mock.Of<IPttavmProductService>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    [Fact]
    public async Task HepsiburadaStatus_timeout_persists_Failed_status()
    {
        // Factory-tabanlı servis (kendi context'ini IDbContextFactory'den açar).
        var pmId = await SeedStalePendingAsync(HepsiburadaMarketPlaceId, "HB", batchRequestId: "hb-1");

        var sut = new HepsiburadaStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<HepsiburadaStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IDbContextFactory<IntegrationDbContext>), DbContextFactory()),
            (typeof(IHepsiburadaProductService), Mock.Of<IHepsiburadaProductService>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    [Fact]
    public async Task AmazonListing_timeout_persists_Failed_status()
    {
        // ExternalProductId zorunlu, BatchRequestId null (feed-* OLMAMALI) → listing yolu.
        var pmId = await SeedStalePendingAsync(AmazonMarketPlaceId, "AMZL",
            batchRequestId: null, externalProductId: "SKU-AMZL");

        var sut = new AmazonListingStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<AmazonListingStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IDbContextFactory<IntegrationDbContext>), DbContextFactory()),
            (typeof(IAmazonProductService), Mock.Of<IAmazonProductService>()),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).Status.Should().Be(MarketplaceProductStatus.Failed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Amazon feed — timeout dalı yok; DONE feed durumu → Published persist olmalı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AmazonFeed_done_persists_Published_status()
    {
        var (productId, _) = await SeedProductAsync("AMZF");
        await SeedMarketPlaceAsync(AmazonMarketPlaceId, "Amazon");
        var pmId = await SeedProductMarketplaceAsync(productId, AmazonMarketPlaceId,
            MarketplaceProductStatus.Pending, batchRequestId: "feed-amz-1");

        var feed = new Mock<IAmazonFeedService>();
        feed.Setup(s => s.GetFeedStatusAsync("feed-amz-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataResult<AmazonFeedStatusResponse>(
                new AmazonFeedStatusResponse("feed-amz-1", FeedType: null,
                    ProcessingStatus: AmazonFeedStatus.Done, ResultFeedDocumentId: null),
                success: true));

        var sut = new AmazonFeedStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<AmazonFeedStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IDbContextFactory<IntegrationDbContext>), DbContextFactory()),
            (typeof(IAmazonFeedService), feed.Object),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        var pm = await ReadPmAsync(pmId);
        pm.Status.Should().Be(MarketplaceProductStatus.Published);
        pm.LastSyncedAt.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Trendyol product status sync — timeout dalı yok; Approved=true → IsApproved persist
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TrendyolProductStatusSync_approved_persists_IsApproved()
    {
        const string barcode = "BC-TSS";
        var (productId, _) = await SeedProductAsync("TSS", barcode: barcode);
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol"); // SellerId = "1"
        var pmId = await SeedProductMarketplaceAsync(productId, TrendyolMarketPlaceId,
            MarketplaceProductStatus.Published, batchRequestId: null, isApproved: false);

        // Barkod onaylı listede bulunur → ONAYLI (unapproved endpoint'e düşmez).
        var json = JsonSerializer.Serialize(new TrendyolApprovedProductsResponse(
            TotalElements: 1, TotalPages: 1, Page: 0, Size: 1,
            Content:
            [
                new TrendyolApprovedProduct(
                    ContentId: 123, ProductMainId: null, Title: "t",
                    Variants:
                    [
                        new TrendyolApprovedVariant(Barcode: barcode, Archived: false, StockCode: null)
                    ])
            ]));

        var apiClient = new Mock<ITrendyolApiClient>();
        apiClient.Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/approved"))))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        await using var ctx = CreateDbContext();
        var sut = new TrendyolProductStatusSyncService(
            ScopeFactoryStub(), TenantRegistryStub(),
            NullLogger<TrendyolProductStatusSyncService>.Instance,
            Options.Create(new NotificationFeatureFlags())); // PublishEnabled = false → domain event yok

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(ITrendyolApiClient), apiClient.Object),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        (await ReadPmAsync(pmId)).IsApproved.Should().BeTrue(
            "Trendyol ürün onayı (Approved=true) ProductMarketplace.IsApproved olarak DB'ye yazılmalı");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Trendyol product status sync — onaylı'da yok, onaysız'da rejectReasonDetails dolu →
    // Rejected + StatusMessage persist olmalı (iki adımlı akış: approved boş → unapproved)
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TrendyolProductStatusSync_rejected_persists_status_and_message()
    {
        const string barcode = "BC-TSS-REJ";
        var (productId, _) = await SeedProductAsync("TSSREJ", barcode: barcode);
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol");
        var pmId = await SeedProductMarketplaceAsync(productId, TrendyolMarketPlaceId,
            MarketplaceProductStatus.Published, batchRequestId: null, isApproved: false);

        var approvedJson = JsonSerializer.Serialize(new TrendyolApprovedProductsResponse(
            TotalElements: 0, TotalPages: 0, Page: 0, Size: 0, Content: []));
        var unapprovedJson = JsonSerializer.Serialize(new TrendyolUnapprovedProductsResponse(
            TotalElements: 1, TotalPages: 1, Page: 0, Size: 1,
            Content:
            [
                new TrendyolUnapprovedProduct(
                    Barcode: barcode, ProductMainId: null, StockCode: null, Title: "t",
                    RejectReasonDetails:
                    [
                        new TrendyolRejectReason("Görsel hatası", "Ürün görseli standartlara uymuyor")
                    ])
            ]));

        var apiClient = new Mock<ITrendyolApiClient>();
        apiClient.Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/approved"))))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(approvedJson, Encoding.UTF8, "application/json")
            });
        apiClient.Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("/products/unapproved"))))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(unapprovedJson, Encoding.UTF8, "application/json")
            });

        await using var ctx = CreateDbContext();
        var sut = new TrendyolProductStatusSyncService(
            ScopeFactoryStub(), TenantRegistryStub(),
            NullLogger<TrendyolProductStatusSyncService>.Instance,
            Options.Create(new NotificationFeatureFlags()));

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IntegrationDbContext), ctx),
            (typeof(ITrendyolApiClient), apiClient.Object),
            (typeof(IProductActivityLogger), ActivityLoggerStub())));

        var pm = await ReadPmAsync(pmId);
        pm.Status.Should().Be(MarketplaceProductStatus.Rejected,
            "onaysız listede rejectReasonDetails dolu → Status Rejected DB'ye yazılmalı");
        pm.StatusMessage.Should().Be("Ürün görseli standartlara uymuyor",
            "StatusMessage = rejectReasonDetails[0].detailedReason");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Trendyol e-Fatura — onaylı fatura linki gönderildi → InvoiceLinkSentToMarketplace persist
    // (mutasyon ayrı bir trackContext üzerinde FirstOrDefault no-AsTracking ile yapılıyor)
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TrendyolEFatura_link_sent_persists_flag()
    {
        var orderId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        // Order.MarketPlaceId → MarketPlace FK; Respawn grup koşumunda HasData seed'ini siler,
        // bu yüzden açıkça seed et (izolasyonda migration seed'i vardı, grup'ta yok → FK ihlali).
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol");
        using (var db = CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = "EF-ORDER-1",
                OrderDate = DateTimeOffset.UtcNow,
                MarketplaceOrderStatus = "Shipped",
                MarketPlaceId = TrendyolMarketPlaceId,
                ShipmentPackageId = 9999,
                BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
            });
            db.EFaturaRecords.Add(new EFaturaRecord
            {
                Id = recordId,
                OrderId = orderId,
                Status = EFaturaStatus.Approved,
                InvoiceType = EFaturaType.EArchive,
                InvoiceId = "ABC2025000000001",
                InvoiceLinkSentToMarketplace = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var eFatura = new Mock<ITrendyolEFaturaService>();
        eFatura.Setup(s => s.GetInvoicePdfUrlAsync(recordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataResult<string>("https://pdf.example/inv.pdf", success: true));
        var invoice = new Mock<ITrendyolInvoiceService>();
        invoice.Setup(s => s.SendInvoiceLinkAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<string?>()))
            .ReturnsAsync(new Result(success: true));

        var sut = new TrendyolEFaturaStatusPollingService(
            ScopeFactoryStub(), TenantRegistryStub(), NullLogger<TrendyolEFaturaStatusPollingService>.Instance);

        await InvokePollAsync(sut, BuildProvider(
            (typeof(IDbContextFactory<IntegrationDbContext>), DbContextFactory()),
            (typeof(ITrendyolEFaturaService), eFatura.Object),
            (typeof(ITrendyolInvoiceService), invoice.Object)));

        using var verify = CreateDbContext();
        var record = await verify.EFaturaRecords.AsNoTracking().FirstAsync(r => r.Id == recordId);
        record.InvoiceLinkSentToMarketplace.Should().BeTrue(
            "fatura linki gönderildikten sonra InvoiceLinkSentToMarketplace DB'ye yazılmalı (no-op olmamalı)");
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static IServiceScopeFactory ScopeFactoryStub() => Mock.Of<IServiceScopeFactory>();

    private static ITenantRegistry TenantRegistryStub() => Mock.Of<ITenantRegistry>();

    private static IProductActivityLogger ActivityLoggerStub()
    {
        var m = new Mock<IProductActivityLogger>();
        m.Setup(x => x.LogAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityType>(),
                It.IsAny<string>(), It.IsAny<ProductActivityStatus>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return m.Object;
    }

    private IDbContextFactory<IntegrationDbContext> DbContextFactory()
        => Services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();

    private static IServiceProvider BuildProvider(params (Type Type, object Instance)[] services)
        => new StubServiceProvider(services.ToDictionary(s => s.Type, s => s.Instance));

    private static async Task InvokePollAsync(object service, IServiceProvider provider, int tenantId = 1)
    {
        var method = service.GetType().GetMethod("PollForTenantAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PollForTenantAsync bulunamadı.");
        var task = (Task)method.Invoke(service,
            [provider, tenantId, StaleLastPoll, CancellationToken.None])!;
        await task;
    }

    private async Task<(Guid ProductId, Guid VariantId)> SeedProductAsync(
        string key, string? barcode = null)
    {
        await SeedBasicEntitiesAsync();
        return await SeedProductWithStockAsync(barcode ?? $"BC-{key}-{Guid.NewGuid():N}", stock: 10);
    }

    private async Task<int> SeedProductMarketplaceAsync(
        Guid productId, int marketPlaceId,
        MarketplaceProductStatus status, string? batchRequestId,
        string? externalProductId = null, bool? isApproved = null)
    {
        using var db = CreateDbContext();
        var pm = new ProductMarketplace
        {
            ProductId = productId,
            MarketPlaceId = marketPlaceId,
            Status = status,
            BatchRequestId = batchRequestId,
            ExternalProductId = externalProductId,
            IsApproved = isApproved,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProductMarketplaces.Add(pm);
        await db.SaveChangesAsync();
        return pm.Id;
    }

    /// <summary>
    /// 24 saatten eski (timeout dalını tetikleyen) Pending bir ProductMarketplace seed eder.
    /// SaveChanges audit'i UpdatedAt'i UtcNow'a çektiği için stale değer ExecuteUpdate ile set edilir
    /// (audit'i bypass eden direkt SQL UPDATE).
    /// </summary>
    private async Task<int> SeedStalePendingAsync(
        int marketPlaceId, string key, string? batchRequestId,
        string? externalProductId = null)
    {
        var (productId, _) = await SeedProductAsync(key);
        await SeedMarketPlaceAsync(marketPlaceId, key);
        var pmId = await SeedProductMarketplaceAsync(productId, marketPlaceId,
            MarketplaceProductStatus.Pending, batchRequestId, externalProductId);

        using var db = CreateDbContext();
        await db.ProductMarketplaces
            .Where(pm => pm.Id == pmId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow.AddHours(-25)));
        return pmId;
    }

    private async Task<ProductMarketplace> ReadPmAsync(int id)
    {
        using var db = CreateDbContext();
        return await db.ProductMarketplaces.AsNoTracking().FirstAsync(pm => pm.Id == id);
    }

    private sealed class StubServiceProvider(Dictionary<Type, object> map) : IServiceProvider
    {
        public object? GetService(Type serviceType) => map.GetValueOrDefault(serviceType);
    }
}
