using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// N11 Sipariş import integration testleri — gercek DB ile N11 Sipariş import akisi.
/// Advisory lock (key=2002), deduplication, barcode eslestirme ve stok dusme dogrulanir.
/// </summary>
[Trait("Category", "Integration")]
public class OrderImportN11IntegrationTests : IntegrationTestBase
{
    public OrderImportN11IntegrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // Seed MarketPlace N11 (id=2)
        var marketPlace = new MarketPlace
        {
            Id = 2,
            Name = "N11",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MarketPlaces.Add(marketPlace);

        // Seed BranchOffice
        var office = new BranchOffice
        {
            Id = 1,
            Name = "Test Depo",
            IsDefaultMarketPlaceStock = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.BranchOffices.Add(office);
        await dbContext.SaveChangesAsync();

        // Seed MarketPlaceWarehouse
        var warehouse = new MarketPlaceWarehouse
        {
            MarketPlaceId = 2,
            BranchOfficeId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MarketPlaceWarehouses.Add(warehouse);

        // Seed Brand + Category
        var brand = new Brand { Name = "N11 Test Brand", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();

        var category = new Category { Name = "N11 Test Kategori", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        // Seed Product + Variant + Stock
        var product = new Product
        {
            Title = "N11 Test Urun",
            Description = "Test",
            StockCode = "N11-STK-001",
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Barcode = "N11-BC-001",
            ListPrice = 100,
            SalePrice = 90,
            CostPrice = 50,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();

        var stock = new BranchOfficeStock
        {
            BranchOfficeId = 1,
            ProductVariantId = variant.Id,
            FirstTotalStock = 20,
            SoldQuantity = 0
        };
        dbContext.BranchOfficeStocks.Add(stock);
        await dbContext.SaveChangesAsync();
    }

    private static N11OrderDto BuildN11Order(long id, string orderNumber, string? barcode = "N11-BC-001", int quantity = 1)
        => new()
        {
            Id = id,
            OrderNumber = orderNumber,
            Status = "New",
            TotalAmount = 200,
            CreateDate = DateTimeOffset.UtcNow,
            Buyer = new N11BuyerDto("Ali", "Veli", "ali@test.com"),
            BillingAddress = new N11AddressDto("Istanbul", "Kadikoy", "Test Adres", "34000"),
            ShippingAddress = new N11AddressDto("Istanbul", "Kadikoy", "Test Adres", "34000"),
            OrderItems =
            [
                new N11OrderItemDto
                {
                    Id = 1000 + id,
                    ProductId = 5000 + id,
                    ProductSellerCode = barcode,
                    ProductName = "Test Urun",
                    Quantity = quantity,
                    Price = 200
                }
            ]
        };

    [Fact]
    public async Task ImportN11Orders_ShouldCreateOrders_WithItems()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var orders = new List<N11OrderDto>
        {
            BuildN11Order(3001, "N11-ORD-001"),
            BuildN11Order(3002, "N11-ORD-002")
        };

        // Act
        var result = await orderManager.ImportN11OrdersAsync(orders);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var dbOrders = await dbContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.MarketPlaceId == 2)
            .ToListAsync();

        dbOrders.Should().HaveCount(2);
        dbOrders.Should().AllSatisfy(o =>
        {
            o.OrderItems.Should().NotBeEmpty();
            o.MarketPlaceId.Should().Be(2, "N11 MarketPlaceId is 2");
        });
    }

    [Fact]
    public async Task ImportN11Orders_ShouldBeIdempotent()
    {
        // Arrange
        var (orderManager1, scope1) = GetScopedService<IOrderManager>();
        using var _1 = scope1;

        var orders = new List<N11OrderDto> { BuildN11Order(3010, "N11-DEDUP-001") };
        var result1 = await orderManager1.ImportN11OrdersAsync(orders);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni OrderNumber ile tekrar import
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var result2 = await orderManager2.ImportN11OrdersAsync(orders);

        // Assert — duplicate eklenmemeli
        result2.Success.Should().BeTrue(result2.Message);

        using var dbContext = CreateDbContext();
        var count = await dbContext.Orders.CountAsync(o => o.OrderNumber == "N11-DEDUP-001" && o.MarketPlaceId == 2);
        count.Should().Be(1, "Duplicate import should be skipped (deduplication by OrderNumber)");
    }

    [Fact]
    public async Task ImportN11Orders_ShouldMatchBarcode_AndDecreaseStock()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var orders = new List<N11OrderDto> { BuildN11Order(3020, "N11-STOCK-001", "N11-BC-001", 3) };

        // Act
        var result = await orderManager.ImportN11OrdersAsync(orders);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var orderItem = await dbContext.OrderItems
            .FirstAsync(oi => oi.Order!.OrderNumber == "N11-STOCK-001");

        orderItem.ProductId.Should().NotBeNull("barcode should match a product variant");

        // Stok dusmus olmali
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == orderItem.ProductId);
        stock.SoldQuantity.Should().BeGreaterThan(0, "Stock should have been decreased after order import");
    }

    [Fact]
    public async Task ImportN11Orders_AdvisoryLock_PreventsConcurrent()
    {
        // Arrange — advisory lock'u manuel olarak al
        using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock({0})", 2002);

        try
        {
            var (orderManager, scope) = GetScopedService<IOrderManager>();
            using var _ = scope;

            var orders = new List<N11OrderDto> { BuildN11Order(3030, "N11-LOCK-001") };

            // Act — lock alinmis durumda import dene
            var result = await orderManager.ImportN11OrdersAsync(orders);

            // Assert — lock nedeniyle basarisiz olmali
            result.Success.Should().BeFalse("Advisory lock should prevent concurrent import");
            result.Message.Should().Contain("zaten devam ediyor");
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", 2002);
        }
    }

    [Fact]
    public async Task ImportN11Orders_UnmatchedBarcode_ShouldStillCreateOrder()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        // Eslesmeyecek barkod
        var orders = new List<N11OrderDto> { BuildN11Order(3040, "N11-NOMATCH-001", "UNKNOWN-BARCODE-999") };

        // Act
        var result = await orderManager.ImportN11OrdersAsync(orders);

        // Assert — Sipariş olusur ama ProductId null olur
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.OrderNumber == "N11-NOMATCH-001");

        order.Should().NotBeNull();
        order.OrderItems.Should().HaveCount(1);
        order.OrderItems.First().ProductId.Should().BeNull("unmatched barcode should result in null ProductId");
    }
}
