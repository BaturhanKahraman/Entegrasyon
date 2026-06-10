using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Ürün 360° veri katmanı integration testleri — gerçek PostgreSQL ile EF sorgularının doğruluğu.
/// NOT: Roles-race infra bug fix'i (backlog) olmadan integration suite koşmuyor; bu testler
/// fix sonrası koşulacak. Feature-gating davranışı unit testlerde (ProductActivityPageManagerTests)
/// kanıtlandı; burada gate'siz sorguların (timeline filtre, sipariş, stok) doğruluğu test edilir.
/// </summary>
[Trait("Category", "Integration")]
public class ProductActivityPageIntegrationTests : IntegrationTestBase
{
    private Guid _productId;
    private Guid _variantId;

    public ProductActivityPageIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync();

        using var db = CreateDbContext();
        if (!await db.MarketPlaces.AnyAsync(m => m.Id == 1))
            db.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });

        var categoryId = (await db.Categories.FirstAsync()).Id;

        _productId = Guid.NewGuid();
        _variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = _productId,
            Title = "360 Test Ürün",
            StockCode = "P360-001",
            Description = "",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = _variantId,
                    Barcode = "P360V1",
                    Name = "Kırmızı / 42",
                    SalePrice = 1m, CostPrice = 1m, ListPrice = 1m, ECommercePrice = 1m,
                    CurrencyType = "TRY",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });

        await db.SaveChangesAsync();
    }

    // ── Timeline filter (IProductActivityLogger — gate'siz) ───────────────────────────

    [Fact]
    public async Task GetTimelineAsync_filters_by_marketplace_status_and_paginates_by_cursor()
    {
        var t0 = DateTimeOffset.UtcNow;
        using (var db = CreateDbContext())
        {
            db.ProductActivityLogs.AddRange(
                new ProductActivityLog { ProductId = _productId, Message = "TY onay", ActivityType = ProductActivityType.Approved, Status = ProductActivityStatus.Success, MarketplaceName = "Trendyol", CreatedAt = t0.AddMinutes(-1) },
                new ProductActivityLog { ProductId = _productId, Message = "N11 hata", ActivityType = ProductActivityType.BatchFailed, Status = ProductActivityStatus.Error, MarketplaceName = "N11", CreatedAt = t0.AddMinutes(-2) },
                new ProductActivityLog { ProductId = _productId, Message = "TY hata", ActivityType = ProductActivityType.BatchFailed, Status = ProductActivityStatus.Error, MarketplaceName = "Trendyol", CreatedAt = t0.AddMinutes(-3) },
                new ProductActivityLog { ProductId = _productId, Message = "TY gönderim", ActivityType = ProductActivityType.PublishSent, Status = ProductActivityStatus.Info, MarketplaceName = "Trendyol", CreatedAt = t0.AddMinutes(-4) });
            await db.SaveChangesAsync();
        }

        var (logger, scope) = GetScopedService<IProductActivityLogger>();
        using var _ = scope;

        // Sadece Trendyol + Error
        var filtered = await logger.GetTimelineAsync(_productId,
            new ProductActivityTimelineFilter(
                MarketplaceNames: ["Trendyol"],
                Statuses: [ProductActivityStatus.Error]),
            pageSize: 20);

        filtered.Success.Should().BeTrue();
        filtered.Data.Should().ContainSingle()
            .Which.Message.Should().Be("TY hata");

        // Cursor pagination: tüm Trendyol kayıtları, pageSize=2 → en yeni 2
        var page1 = await logger.GetTimelineAsync(_productId,
            new ProductActivityTimelineFilter(MarketplaceNames: ["Trendyol"]), pageSize: 2);
        page1.Data.Should().HaveCount(2);
        page1.Data![0].Message.Should().Be("TY onay");
        page1.Data![1].Message.Should().Be("TY hata");

        var cursor = page1.Data[^1].CreatedAt;
        var page2 = await logger.GetTimelineAsync(_productId,
            new ProductActivityTimelineFilter(MarketplaceNames: ["Trendyol"], Cursor: cursor), pageSize: 2);
        page2.Data.Should().ContainSingle()
            .Which.Message.Should().Be("TY gönderim");
    }

    [Fact]
    public async Task GetTimelineAsync_keyset_does_not_drop_rows_sharing_same_createdAt()
    {
        // Aynı CreatedAt'i paylaşan 4 log (tek batch'te yazılmış gibi) — salt `CreatedAt < cursor`
        // sayfa sınırındaki aynı-timestamp satırları düşürürdü. Keyset (CreatedAt, Id) bunu önler.
        var sharedTs = DateTimeOffset.UtcNow.AddMinutes(-5);
        using (var db = CreateDbContext())
        {
            for (var i = 0; i < 4; i++)
                db.ProductActivityLogs.Add(new ProductActivityLog
                {
                    ProductId = _productId,
                    Message = $"batch-{i}",
                    ActivityType = ProductActivityType.PublishSent,
                    Status = ProductActivityStatus.Info,
                    MarketplaceName = "Trendyol",
                    CreatedAt = sharedTs
                });
            await db.SaveChangesAsync();
        }

        var (logger, scope) = GetScopedService<IProductActivityLogger>();
        using var _ = scope;

        var seen = new List<long>();
        DateTimeOffset? cursor = null;
        long? cursorId = null;

        // 2'şerli sayfalarla tüm kayıtları topla
        for (var page = 0; page < 3; page++)
        {
            var result = await logger.GetTimelineAsync(_productId,
                new ProductActivityTimelineFilter(MarketplaceNames: ["Trendyol"], Cursor: cursor, CursorId: cursorId),
                pageSize: 2);
            if (result.Data!.Count == 0) break;
            seen.AddRange(result.Data.Select(l => l.Id));
            cursor = result.Data[^1].CreatedAt;
            cursorId = result.Data[^1].Id;
        }

        seen.Should().HaveCount(4, "aynı CreatedAt'li 4 satırın hiçbiri keyset pagination'da düşmemeli");
        seen.Should().OnlyHaveUniqueItems("hiçbir satır iki kez gelmemeli");
    }

    // ── Orders (IProductActivityPageManager.GetOrdersAsync — gate'siz) ─────────────────

    [Fact]
    public async Task GetOrdersAsync_returns_orders_containing_product_variant_with_platform_and_label()
    {
        using (var db = CreateDbContext())
        {
            var marketplaceOrderId = Guid.NewGuid();
            var storeOrderId = Guid.NewGuid();

            db.Orders.AddRange(
                new Order
                {
                    Id = marketplaceOrderId,
                    OrderNumber = "TY-1",
                    OrderDate = DateTimeOffset.UtcNow.AddMinutes(-10),
                    MarketPlaceId = 1,
                    BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                    ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
                },
                new Order
                {
                    Id = storeOrderId,
                    OrderNumber = "POS-1",
                    OrderDate = DateTimeOffset.UtcNow.AddMinutes(-20),
                    MarketPlaceId = null,
                    BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                    ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
                });

            db.OrderItems.AddRange(
                new OrderItem { OrderId = marketplaceOrderId, ProductId = _variantId, Quantity = 2, UnitPrice = 10m, ProductColor = "Kırmızı", ProductSize = "42" },
                new OrderItem { OrderId = storeOrderId, ProductId = _variantId, Quantity = 3, UnitPrice = 10m });

            await db.SaveChangesAsync();
        }

        var (manager, scope) = GetScopedService<IProductActivityPageManager>();
        using var _ = scope;

        var result = await manager.GetOrdersAsync(_productId);

        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().HaveCount(2);
        result.Data!.Should().ContainSingle(o => o.OrderNumber == "TY-1")
            .Which.Platform.Should().Be("Trendyol");
        result.Data!.Single(o => o.OrderNumber == "TY-1").VariantLabel.Should().Be("Kırmızı / 42");
        result.Data!.Single(o => o.OrderNumber == "POS-1").Platform.Should().Be("Mağaza");
        // En yeni sipariş önce
        result.Data![0].OrderNumber.Should().Be("TY-1");
    }

    // ── Stock movements (IProductActivityPageManager.GetStockMovementsAsync — gate'siz) ─

    [Fact]
    public async Task GetStockMovementsAsync_returns_variant_movements_newest_first()
    {
        using (var db = CreateDbContext())
        {
            db.StockMovements.AddRange(
                new StockMovement { BranchOfficeId = 1, ProductVariantId = _variantId, Type = StockMovementType.ManualAdjustment, Quantity = 10, StockBefore = 0, StockAfter = 10, ReferenceType = "ManualAdjustment", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) },
                new StockMovement { BranchOfficeId = 1, ProductVariantId = _variantId, Type = StockMovementType.Sale, Quantity = -2, StockBefore = 10, StockAfter = 8, ReferenceType = "Sale", ReferenceId = "POS-1", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
            await db.SaveChangesAsync();
        }

        var (manager, scope) = GetScopedService<IProductActivityPageManager>();
        using var _ = scope;

        var result = await manager.GetStockMovementsAsync(_productId);

        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().HaveCount(2);
        result.Data![0].Type.Should().Be(StockMovementType.Sale, "en yeni hareket önce gelmeli");
        result.Data![0].StockAfter.Should().Be(8);
        result.Data!.Should().OnlyContain(m => m.VariantLabel == "Kırmızı / 42");
    }
}
