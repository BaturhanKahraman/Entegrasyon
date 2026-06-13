using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Pazarama Sipariş import integration testleri — gercek DB ile Pazarama Sipariş import akisi.
/// Advisory lock (key=2005), deduplication, barcode eslestirme, stok dusme ve musteri adi ayrıstırma dogrulanir.
/// </summary>
[Trait("Category", "Integration")]
public class OrderImportPazaramaIntegrationTests : IntegrationTestBase
{
    public OrderImportPazaramaIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // Seed MarketPlace Pazarama (id=5)
        var marketPlace = new MarketPlace
        {
            Id = 5,
            Name = "Pazarama",
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
            MarketPlaceId = 5,
            BranchOfficeId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MarketPlaceWarehouses.Add(warehouse);

        // Seed Brand + Category
        var brand = new Brand { Name = "Pazarama Test Brand", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();

        var category = new Category { Name = "Pazarama Test Kategori", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        // Seed Product + Variant + Stock
        var product = new Product
        {
            Title = "Pazarama Test Urun",
            Description = "Test",
            StockCode = "PZR-STK-001",
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Barcode = "PZR-BC-001",
            ListPrice = 150,
            SalePrice = 120,
            CostPrice = 60,
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

    private static PazaramaOrderDto BuildPazaramaOrder(
        long orderNumber,
        string? barcode = "PZR-BC-001",
        int quantity = 1,
        string? customerName = "Ahmet Yilmaz",
        PazaramaCargoDto? cargo = null)
        => new(
            OrderId: Guid.NewGuid().ToString(),
            OrderNumber: orderNumber,
            OrderDate: DateTimeOffset.UtcNow.ToString("o"),
            OrderAmount: 200,
            ShipmentAmount: 10,
            DiscountAmount: 0,
            DiscountDescription: null,
            Currency: "TRY",
            PaymentType: 1,
            OrderStatus: 1,
            CustomerId: Guid.NewGuid().ToString(),
            CustomerName: customerName,
            CustomerEmail: "test@pazarama.com",
            ShipmentAddress: new PazaramaOrderAddressDto(
                null, null, "Test Musteri", "test@test.com",
                "Istanbul", "Kadikoy", null, "Test Adres", "Test Adres Detay", "5551234567"),
            BillingAddress: new PazaramaOrderBillingAddressDto(
                null, null, "Test Musteri", "test@test.com",
                "Istanbul", "Kadikoy", null, "Test Fatura Adres", "Fatura Detay", "5551234567",
                null, null, null, null, null, null),
            Items:
            [
                new PazaramaOrderItemDto(
                    OrderItemId: Guid.NewGuid().ToString(),
                    OrderItemStatus: 1,
                    ShipmentCode: null,
                    ShipmentCost: null,
                    DeliveryType: 1,
                    DeliveryDetail: null,
                    Quantity: quantity,
                    ListPrice: new PazaramaMoneyDto(200, 20000, "200.00", "TRY"),
                    SalePrice: new PazaramaMoneyDto(180, 18000, "180.00", "TRY"),
                    TaxAmount: null,
                    ShipmentAmount: null,
                    TotalPrice: new PazaramaMoneyDto(180, 18000, "180.00", "TRY"),
                    DiscountAmount: null,
                    DiscountDescription: null,
                    TaxIncluded: true,
                    Cargo: cargo,
                    Product: new PazaramaOrderProductDto(
                        ProductId: "ext-prod-1",
                        Name: "Test Urun",
                        Title: "Test Urun",
                        Url: null,
                        ImageUrl: null,
                        VariantOptionDisplay: null,
                        StockCode: "PZR-STK-001",
                        Code: barcode,
                        VatRate: 20))
            ]);

    [Fact]
    public async Task ImportPazaramaOrders_ShouldCreateOrders()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var orders = new List<PazaramaOrderDto>
        {
            BuildPazaramaOrder(5001),
            BuildPazaramaOrder(5002)
        };

        // Act
        var result = await orderManager.ImportPazaramaOrdersAsync(orders);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var dbOrders = await dbContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.MarketPlaceId == 5)
            .ToListAsync();

        dbOrders.Should().HaveCount(2);
        dbOrders.Should().AllSatisfy(o =>
        {
            o.OrderItems.Should().NotBeEmpty();
            o.MarketPlaceId.Should().Be(5, "Pazarama MarketPlaceId is 5");
        });
    }

    [Fact]
    public async Task ImportPazaramaOrders_ShouldBeIdempotent()
    {
        // Arrange
        var (orderManager1, scope1) = GetScopedService<IOrderManager>();
        using var _1 = scope1;

        var orders = new List<PazaramaOrderDto> { BuildPazaramaOrder(5010) };
        var result1 = await orderManager1.ImportPazaramaOrdersAsync(orders);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni OrderNumber ile tekrar import
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var result2 = await orderManager2.ImportPazaramaOrdersAsync(orders);

        // Assert — duplicate eklenmemeli
        result2.Success.Should().BeTrue(result2.Message);

        using var dbContext = CreateDbContext();
        var count = await dbContext.Orders.CountAsync(o => o.OrderNumber == "5010" && o.MarketPlaceId == 5);
        count.Should().Be(1, "Duplicate import should be skipped (deduplication by OrderNumber)");
    }

    [Fact]
    public async Task ImportPazaramaOrders_ShouldMatchBarcode_AndDecreaseStock()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var orders = new List<PazaramaOrderDto> { BuildPazaramaOrder(5020, "PZR-BC-001", 2) };

        // Act
        var result = await orderManager.ImportPazaramaOrdersAsync(orders);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var orderItem = await dbContext.OrderItems
            .FirstAsync(oi => oi.Order!.OrderNumber == "5020");

        orderItem.ProductId.Should().NotBeNull("barcode should match a product variant");

        // Stok dusmus olmali
        var stock = await dbContext.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == orderItem.ProductId);
        stock.SoldQuantity.Should().BeGreaterThan(0, "Stock should have been decreased after order import");
    }

    [Fact]
    public async Task ImportPazaramaOrders_AdvisoryLock_PreventsConcurrent()
    {
        // Arrange — advisory lock'u manuel olarak al
        using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock({0})", 2005);

        try
        {
            var (orderManager, scope) = GetScopedService<IOrderManager>();
            using var _ = scope;

            var orders = new List<PazaramaOrderDto> { BuildPazaramaOrder(5030) };

            // Act — lock alinmis durumda import dene
            var result = await orderManager.ImportPazaramaOrdersAsync(orders);

            // Assert — lock nedeniyle başarısız olmali
            result.Success.Should().BeFalse("Advisory lock should prevent concurrent import");
            result.Message.Should().Contain("zaten devam ediyor");
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", 2005);
        }
    }

    [Fact]
    public async Task ImportPazaramaOrders_ShouldPersistCargoInfo()
    {
        // Arrange — kargo bilgisi dolu sipariş (T061)
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var cargo = new PazaramaCargoDto(
            CompanyName: "Yurtici Kargo",
            TrackingNumber: "PZR-TRK-5050",
            TrackingUrl: "https://kargo.test/PZR-TRK-5050");
        var orders = new List<PazaramaOrderDto> { BuildPazaramaOrder(5050, cargo: cargo) };

        // Act
        var result = await orderManager.ImportPazaramaOrdersAsync(orders);

        // Assert — DB'den tekrar okuyup persist'i dogrula (no-tracking kurali)
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var order = await dbContext.Orders.FirstAsync(o => o.OrderNumber == "5050" && o.MarketPlaceId == 5);

        order.CargoProviderName.Should().Be("Yurtici Kargo");
        order.CargoTrackingNumber.Should().Be("PZR-TRK-5050");
        order.CargoTrackingLink.Should().Be("https://kargo.test/PZR-TRK-5050");
    }

    [Fact]
    public async Task ImportPazaramaOrders_ShouldParseCustomerName()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var orders = new List<PazaramaOrderDto> { BuildPazaramaOrder(5040, customerName: "Mehmet Can Ozturk") };

        // Act
        var result = await orderManager.ImportPazaramaOrdersAsync(orders);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var order = await dbContext.Orders.FirstAsync(o => o.OrderNumber == "5040");

        // CustomerName "Mehmet Can Ozturk" → firstName="Mehmet", lastName="Can Ozturk"
        order.CustomerFirstName.Should().Be("Mehmet");
        order.CustomerLastName.Should().Be("Can Ozturk");
    }
}
