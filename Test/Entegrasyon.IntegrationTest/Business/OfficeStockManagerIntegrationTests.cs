using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// OfficeStockManager integration testleri — atomic stok dusme, race condition, force decrease.
/// </summary>
[Trait("Category", "Integration")]
public class OfficeStockManagerIntegrationTests : IntegrationTestBase
{
    public OfficeStockManagerIntegrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Stock Test Marka", categoryName: "Stock Test Kategori");
    }

    [Fact]
    public async Task DecreaseStockAtomicAsync_ShouldDecrease_AndCreateMovement()
    {
        // Arrange
        var (_, variantId) = await SeedProductWithStockAsync("ATOMIC-001", stock: 50);
        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        // Act
        var result = await service.DecreaseStockAtomicAsync(
            1, variantId, 5, StockMovementType.Sale, "Sale", "test-ref");

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.Quantity.Should().Be(-5);
        result.Data.StockBefore.Should().Be(50);
        result.Data.StockAfter.Should().Be(45);
        result.Data.ReferenceType.Should().Be("Sale");

        // Verify DB: SoldQuantity increased
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(5);

        // Verify StockMovement in DB
        var movement = await dbContext.StockMovements
            .FirstOrDefaultAsync(m => m.ProductVariantId == variantId);
        movement.Should().NotBeNull();
        movement!.Type.Should().Be(StockMovementType.Sale);
    }

    [Fact]
    public async Task DecreaseStockAtomicAsync_ShouldFail_WhenInsufficientStock()
    {
        // Arrange
        var (_, variantId) = await SeedProductWithStockAsync("ATOMIC-002", stock: 50);
        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        // Act — 50 stoktan 100 dusmeye calis
        var result = await service.DecreaseStockAtomicAsync(
            1, variantId, 100, StockMovementType.Sale);

        // Assert
        result.Success.Should().BeFalse();

        // Verify DB: SoldQuantity unchanged
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(0);
    }

    [Fact]
    public async Task DecreaseStockAtomicAsync_ConcurrentCalls_RaceCondition()
    {
        // Arrange — stock=1, iki paralel azaltma
        var (_, variantId) = await SeedProductWithStockAsync("ATOMIC-RACE-001", stock: 1);

        // Act — ayri scope'lar ile paralel calis (DbContext thread-safe degil)
        var task1 = Task.Run(async () =>
        {
            var scope = Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IOfficeStockManager>();
            try { return await svc.DecreaseStockAtomicAsync(1, variantId, 1, StockMovementType.Sale); }
            finally { scope.Dispose(); }
        });

        var task2 = Task.Run(async () =>
        {
            var scope = Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IOfficeStockManager>();
            try { return await svc.DecreaseStockAtomicAsync(1, variantId, 1, StockMovementType.Sale); }
            finally { scope.Dispose(); }
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert — biri basarili, biri basarisiz olmali
        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);
        successCount.Should().Be(1);
        failCount.Should().Be(1);

        // Verify DB: SoldQuantity = 1 (sadece 1 basarili)
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(1);
    }

    [Fact]
    public async Task ForceDecreaseStockAsync_ShouldSucceed_EvenWhenInsufficient()
    {
        // Arrange — stock=2
        var (_, variantId) = await SeedProductWithStockAsync("FORCE-001", stock: 2);
        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        // Act — 5 dus, negatife gecmeli
        var result = await service.ForceDecreaseStockAsync(
            1, variantId, 5, StockMovementType.MarketplaceSale, "TrendyolOrder");

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.StockAfter.Should().Be(-3);
        result.Data.Note.Should().Contain("Negatif");

        // Verify DB
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(5);
    }

    [Fact]
    public async Task DecreaseProductsStock_Batch_ShouldDecreaseAll()
    {
        // Arrange — 2 variant
        var (_, variantId1) = await SeedProductWithStockAsync("BATCH-001", stock: 20, stockCode: "BATCH-SC-001");
        var (_, variantId2) = await SeedProductWithStockAsync("BATCH-002", stock: 30, stockCode: "BATCH-SC-002");

        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        var dtos = new List<DecreaseStockDto>
        {
            new(variantId1, 1, 5),
            new(variantId2, 1, 10)
        };

        // Act
        var result = await service.DecreaseProductsStock(dtos);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var stock1 = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId1);
        var stock2 = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId2);
        stock1.SoldQuantity.Should().Be(5);
        stock2.SoldQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CheckIfProductCountZero_ShouldReturnError_WhenAllZero()
    {
        // Arrange
        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        var stocks = new[]
        {
            new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 },
            new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 }
        };

        // Act
        var result = service.CheckIfProductCountZero(stocks);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddOfficeStocks_ShouldFail_WhenOfficeNotFound()
    {
        // Arrange
        var (_, variantId) = await SeedProductWithStockAsync("OFFICE-ERR-001", stock: 10);
        var (service, scope) = GetScopedService<IOfficeStockManager>();
        using var _ = scope;

        var stocks = new[]
        {
            new BranchOfficeStock
            {
                BranchOfficeId = 99999, // non-existent
                ProductVariantId = variantId,
                FirstTotalStock = 10,
                SoldQuantity = 0
            }
        };

        // Act
        var result = await service.AddOfficeStocks(stocks);

        // Assert
        result.Success.Should().BeFalse();
    }
}
