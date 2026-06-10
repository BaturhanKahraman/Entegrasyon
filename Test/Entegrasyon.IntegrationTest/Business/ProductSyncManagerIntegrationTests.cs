using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// ProductSyncManager integration testleri — sync, retry, advisory lock, batch sync.
/// </summary>
[Trait("Category", "Integration")]
public class ProductSyncManagerIntegrationTests : IntegrationTestBase
{
    private const int TestMarketPlaceId = 1;

    public ProductSyncManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Sync Test Marka", categoryName: "Sync Test Kategori");
        await SeedMarketPlaceAsync(TestMarketPlaceId, "Trendyol");
    }

    [Fact]
    public async Task SyncProductAsync_ShouldCreatePendingRecord()
    {
        // Arrange
        var (productId, _) = await SeedProductWithStockAsync("SYNC-001", stock: 10);
        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.SyncProductAsync(productId, TestMarketPlaceId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TestMarketPlaceId);
        pm.Should().NotBeNull();
        pm!.Status.Should().Be(MarketplaceProductStatus.Pending);
    }

    [Fact]
    public async Task SyncProductAsync_ShouldResetToPending_WhenAlreadyExists()
    {
        // Arrange — once Published olarak seed et
        var (productId, _) = await SeedProductWithStockAsync("SYNC-002", stock: 10);

        using (var dbContext = CreateDbContext())
        {
            dbContext.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = TestMarketPlaceId,
                Status = MarketplaceProductStatus.Published,
                BatchRequestId = "old-batch",
                StatusMessage = "old-message",
                LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
            await dbContext.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.SyncProductAsync(productId, TestMarketPlaceId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var pm = await verifyDb.ProductMarketplaces
            .FirstAsync(x => x.ProductId == productId && x.MarketPlaceId == TestMarketPlaceId);
        pm.Status.Should().Be(MarketplaceProductStatus.Pending);
        pm.BatchRequestId.Should().BeNull();
        pm.StatusMessage.Should().BeNull();
    }

    [Fact]
    public async Task SyncProductAsync_ShouldReturnError_WhenProductNotFound()
    {
        // Arrange
        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.SyncProductAsync(Guid.NewGuid(), TestMarketPlaceId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllPendingAsync_ShouldCreateRecords_ForAllUnsynced()
    {
        // Arrange — 3 urun, 1 zaten synced
        var (productId1, _) = await SeedProductWithStockAsync("SYNCALL-001", stock: 10, stockCode: "SYNCALL-SC-001");
        var (productId2, _) = await SeedProductWithStockAsync("SYNCALL-002", stock: 10, stockCode: "SYNCALL-SC-002");
        var (productId3, _) = await SeedProductWithStockAsync("SYNCALL-003", stock: 10, stockCode: "SYNCALL-SC-003");

        // productId1 zaten synced
        using (var dbContext = CreateDbContext())
        {
            dbContext.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = productId1,
                MarketPlaceId = TestMarketPlaceId,
                Status = MarketplaceProductStatus.Published
            });
            await dbContext.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.SyncAllPendingAsync(TestMarketPlaceId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var pendingCount = await verifyDb.ProductMarketplaces
            .CountAsync(pm => pm.MarketPlaceId == TestMarketPlaceId && pm.Status == MarketplaceProductStatus.Pending);
        pendingCount.Should().Be(2); // productId2, productId3
    }

    [Fact]
    public async Task SyncAllPendingAsync_AdvisoryLock_PreventsConcurrent()
    {
        // Arrange — advisory lock 1001'i manuel al
        await SeedProductWithStockAsync("LOCK-SYNC-001", stock: 10);

        using var lockConnection = new Npgsql.NpgsqlConnection(
            Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
                .GetSection("ConnectionStrings")["Main"]);
        await lockConnection.OpenAsync();

        // Lock 1001 al
        using var lockCmd = lockConnection.CreateCommand();
        lockCmd.CommandText = "SELECT pg_advisory_lock(1001)";
        await lockCmd.ExecuteNonQueryAsync();

        try
        {
            var (service, scope) = GetScopedService<IProductSyncManager>();
            using var _ = scope;

            // Act — lock zaten alinmis, başarısız olmali
            var result = await service.SyncAllPendingAsync(TestMarketPlaceId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("zaten devam ediyor");
        }
        finally
        {
            // Lock'u serbest birak
            using var unlockCmd = lockConnection.CreateCommand();
            unlockCmd.CommandText = "SELECT pg_advisory_unlock(1001)";
            await unlockCmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task RetryFailedAsync_ShouldResetToPending()
    {
        // Arrange — Failed kayit olustur
        var (productId, _) = await SeedProductWithStockAsync("RETRY-001", stock: 10);

        using (var dbContext = CreateDbContext())
        {
            dbContext.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = TestMarketPlaceId,
                Status = MarketplaceProductStatus.Failed,
                StatusMessage = "API error"
            });
            await dbContext.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.RetryFailedAsync(productId, TestMarketPlaceId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var pm = await verifyDb.ProductMarketplaces
            .FirstAsync(x => x.ProductId == productId && x.MarketPlaceId == TestMarketPlaceId);
        pm.Status.Should().Be(MarketplaceProductStatus.Pending);
        pm.StatusMessage.Should().BeNull();
    }

    [Fact]
    public async Task RetryFailedAsync_ShouldReturnError_WhenNotFailed()
    {
        // Arrange — Published kayit olustur
        var (productId, _) = await SeedProductWithStockAsync("RETRY-002", stock: 10);

        using (var dbContext = CreateDbContext())
        {
            dbContext.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = TestMarketPlaceId,
                Status = MarketplaceProductStatus.Published
            });
            await dbContext.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.RetryFailedAsync(productId, TestMarketPlaceId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RetryAllFailedAsync_ShouldResetAllFailedAndRejected()
    {
        // Arrange — 2 Failed + 1 Rejected + 1 Published
        var (pId1, _) = await SeedProductWithStockAsync("RETRYALL-001", stock: 10, stockCode: "RETRYALL-SC-001");
        var (pId2, _) = await SeedProductWithStockAsync("RETRYALL-002", stock: 10, stockCode: "RETRYALL-SC-002");
        var (pId3, _) = await SeedProductWithStockAsync("RETRYALL-003", stock: 10, stockCode: "RETRYALL-SC-003");
        var (pId4, _) = await SeedProductWithStockAsync("RETRYALL-004", stock: 10, stockCode: "RETRYALL-SC-004");

        using (var dbContext = CreateDbContext())
        {
            dbContext.ProductMarketplaces.AddRange(
                new ProductMarketplace { ProductId = pId1, MarketPlaceId = TestMarketPlaceId, Status = MarketplaceProductStatus.Failed, StatusMessage = "err1" },
                new ProductMarketplace { ProductId = pId2, MarketPlaceId = TestMarketPlaceId, Status = MarketplaceProductStatus.Failed, StatusMessage = "err2" },
                new ProductMarketplace { ProductId = pId3, MarketPlaceId = TestMarketPlaceId, Status = MarketplaceProductStatus.Rejected, StatusMessage = "rejected" },
                new ProductMarketplace { ProductId = pId4, MarketPlaceId = TestMarketPlaceId, Status = MarketplaceProductStatus.Published }
            );
            await dbContext.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var result = await service.RetryAllFailedAsync(TestMarketPlaceId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var pendingCount = await verifyDb.ProductMarketplaces
            .CountAsync(pm => pm.MarketPlaceId == TestMarketPlaceId && pm.Status == MarketplaceProductStatus.Pending);
        pendingCount.Should().Be(3); // pId1, pId2, pId3

        // Published kayit degismemeli
        var published = await verifyDb.ProductMarketplaces
            .FirstAsync(pm => pm.ProductId == pId4 && pm.MarketPlaceId == TestMarketPlaceId);
        published.Status.Should().Be(MarketplaceProductStatus.Published);
    }
}
