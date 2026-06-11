using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CheckoutManager oversell regression testleri.
/// Kritik senaryo: depo-1=0, depo-2=stoklu → checkout depo-1'den düşmeye çalışırsa
/// affected==0 alır ve sessizce başarısız olurdu (sipariş yine de oluşuyordu — oversell).
/// Bu sınıf o regresyonu sabitler.
/// </summary>
[Trait("Category", "Integration")]
public class CheckoutManagerOversellIntegrationTests : IntegrationTestBase
{
    public CheckoutManagerOversellIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Checkout Test Marka", categoryName: "Checkout Test Kategori");
        // Depo-2 de gerekiyor
        await SeedBranchOfficeAsync(branchOfficeId: 2, name: "Depo 2");
    }

    private async Task SeedBranchOfficeAsync(int branchOfficeId, string name)
    {
        using var dbContext = CreateDbContext();
        if (!await dbContext.BranchOffices.AnyAsync(b => b.Id == branchOfficeId))
        {
            dbContext.BranchOffices.Add(new BranchOffice
            {
                Id = branchOfficeId,
                Name = name,
                IsDefaultMarketPlaceStock = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Kritik oversell senaryo — hiçbir depoda yeterli tek-depo stoku yok.
    /// Depo-1=2, Depo-2=2: toplam=4 >= 5 (eski Sum() pre-check geçerdi),
    /// ama hiçbir tek depoda 5 adet yok → pre-check BLOKLAYAN HATASI dönmeli,
    /// sipariş DB'ye yazılmamalı.
    /// </summary>
    [Fact]
    public async Task CreateOrderFromCartAsync_WhenNoSingleWarehouseHasSufficientStock_ShouldFail()
    {
        // Arrange — depo-1=2, depo-2=2: toplam=4, ama tek depoda 5 yok
        var (_, variantId) = await SeedProductWithStockAsync(
            "CO-OVERSELL-001", stock: 2, branchOfficeId: 1);
        using (var dbContext = CreateDbContext())
        {
            dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
            {
                BranchOfficeId = 2,
                ProductVariantId = variantId,
                FirstTotalStock = 2,
                SoldQuantity = 0
            });
            await dbContext.SaveChangesAsync();
        }

        // Müşteri 5 adet istiyor — hiçbir depoda yetmez
        var (cartId, customerId) = await SeedCartAsync(variantId, quantity: 5, unitPrice: 100m);

        var (sut, scope) = GetScopedService<ICheckoutManager>();
        using var _ = scope;

        // Act
        var result = await sut.CreateOrderFromCartAsync(
            cartId, customerId, tenantId: 1, CheckoutDto("Ahmet Yılmaz"),
            freeShippingThreshold: 500m, flatShippingRate: 29.90m);

        // Assert — hiçbir depoda tek başına 5 adet yok → hata
        result.Success.Should().BeFalse(
            "hiçbir tek depoda yeterli stok yok, toplam stok yeterli olsa bile sipariş oluşturulmamalı");

        // DB'de sipariş yazılmadığını doğrula
        using var verifyCtx = CreateDbContext();
        var order = await verifyCtx.Orders.FirstOrDefaultAsync(o => o.CustomerId == customerId);
        order.Should().BeNull("stoksuz sipariş DB'ye yazılmamalı");
    }

    /// <summary>
    /// Kritik race-condition oversell senaryo (S2 bug özü):
    /// Depo-1=0, Depo-2=5. Eski kod: Sum()=5 geçiyor, hardcoded depo-1'den düşüyor,
    /// affected==0 sessizce görmezden geliniyor → sipariş stoksuz oluşuyor.
    /// Yeni kod: depo-2'yi SEÇİYOR (yeterli stoku olan ilk depo) ve sipariş BAŞARILI oluşuyor.
    /// Kritik kontrol: depo-2 stoğu gerçekten düşülmüş olmali (artık sessiz başarısızlık yok).
    /// </summary>
    [Fact]
    public async Task CreateOrderFromCartAsync_WhenDepo1ZeroDepo2HasStock_SelectsDepo2_StockProperlyDecreased()
    {
        // Arrange — depo-1=0, depo-2=5
        var (_, variantId) = await SeedProductWithStockAsync(
            "CO-OVERSELL-002", stock: 0, branchOfficeId: 1);
        using (var dbContext = CreateDbContext())
        {
            dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
            {
                BranchOfficeId = 2,
                ProductVariantId = variantId,
                FirstTotalStock = 5,
                SoldQuantity = 0
            });
            await dbContext.SaveChangesAsync();
        }

        var (cartId, customerId) = await SeedCartAsync(variantId, quantity: 1, unitPrice: 100m);

        var (sut, scope) = GetScopedService<ICheckoutManager>();
        using var _ = scope;

        // Act
        var result = await sut.CreateOrderFromCartAsync(
            cartId, customerId, tenantId: 1, CheckoutDto("Zeynep Kaya"),
            freeShippingThreshold: 500m, flatShippingRate: 29.90m);

        // Assert — depo-2 seçilmeli, sipariş başarılı oluşmalı
        result.Success.Should().BeTrue(
            "depo-2'de 5 adet var; yeni kod onu seçmeli ve sipariş oluşturmalı");

        // Depo-1 stoku değişmemiş olmalı (eski bug: depo-1'den sıfır düşürdü, etki yoktu)
        using var verifyCtx = CreateDbContext();
        var stock1 = await verifyCtx.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock1.SoldQuantity.Should().Be(0, "depo-1 stoku hiç değişmemeli");

        // Depo-2'den gerçekten düşülmüş olmalı
        var stock2 = await verifyCtx.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 2);
        stock2.SoldQuantity.Should().Be(1, "depo-2'den 1 adet düşülmeli");
    }

    /// <summary>
    /// Depo-1'de yeterli stok varsa sipariş başarıyla oluşmalı.
    /// Regresyon: fix sonrası happy-path bozulmamalı.
    /// </summary>
    [Fact]
    public async Task CreateOrderFromCartAsync_WhenSelectedWarehouseHasSufficientStock_ShouldSucceed()
    {
        // Arrange — depo-1=10
        var (_, variantId) = await SeedProductWithStockAsync(
            "CO-HAPPY-001", stock: 10, branchOfficeId: 1);

        var (cartId, customerId) = await SeedCartAsync(variantId, quantity: 2, unitPrice: 150m);

        var (sut, scope) = GetScopedService<ICheckoutManager>();
        using var _ = scope;

        var dto = CheckoutDto("Mehmet Demir");

        // Act
        var result = await sut.CreateOrderFromCartAsync(
            cartId, customerId, tenantId: 1, dto,
            freeShippingThreshold: 500m, flatShippingRate: 29.90m);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();

        // Stok düştü mü?
        using var verifyCtx = CreateDbContext();
        var stock = await verifyCtx.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(2);
    }

    /// <summary>
    /// Yarış senaryosu: son 1 ürün, eş zamanlı 2 checkout isteği.
    /// Atomik SQL garantisi sayesinde yalnız biri başarılı olmalı.
    /// </summary>
    [Fact]
    public async Task CreateOrderFromCartAsync_ConcurrentCheckout_OnlyOneOrderCreated()
    {
        // Arrange — depo-1=1
        var (_, variantId) = await SeedProductWithStockAsync(
            "CO-RACE-001", stock: 1, branchOfficeId: 1);

        // İki farklı müşteri + sepet
        var (cartId1, customerId1) = await SeedCartAsync(variantId, quantity: 1, unitPrice: 100m, customerSuffix: "A");
        var (cartId2, customerId2) = await SeedCartAsync(variantId, quantity: 1, unitPrice: 100m, customerSuffix: "B");

        var dto1 = CheckoutDto("Müşteri A");
        var dto2 = CheckoutDto("Müşteri B");

        // Act — ayrı scope'larla paralel çalıştır
        var task1 = Task.Run(async () =>
        {
            var scope = Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICheckoutManager>();
            try
            {
                return await svc.CreateOrderFromCartAsync(
                    cartId1, customerId1, 1, dto1, 500m, 29.90m);
            }
            finally { scope.Dispose(); }
        });

        var task2 = Task.Run(async () =>
        {
            var scope = Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICheckoutManager>();
            try
            {
                return await svc.CreateOrderFromCartAsync(
                    cartId2, customerId2, 1, dto2, 500m, 29.90m);
            }
            finally { scope.Dispose(); }
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert — biri başarılı, biri başarısız
        var successCount = results.Count(r => r.Success);
        successCount.Should().Be(1, "son 1 stok için yalnız 1 sipariş oluşturulabilir");

        // DB'de stok eksi gitmesin
        using var verifyCtx = CreateDbContext();
        var stock = await verifyCtx.BranchOfficeStocks
            .FirstAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.SoldQuantity.Should().Be(1);
        stock.CurrentStock.Should().Be(0);
    }

    // ─── Seed Yardımcıları ──────────────────────────────────────────────────

    private async Task<(Guid CartId, int CustomerId)> SeedCartAsync(
        Guid variantId, int quantity, decimal unitPrice, string customerSuffix = "")
    {
        using var dbContext = CreateDbContext();

        // Müşteri
        dbContext.Customers.Add(new Customer
        {
            Name = "Test",
            Surname = $"Müşteri{customerSuffix}",
            FullName = $"Test Müşteri{customerSuffix}",
            CustomerType = "Retail",
            PhoneNumber = "5551234567",
            Address = new Address
            {
                City = "Istanbul",
                Country = "Turkey",
                FullAddress = "Test Adres"
            },
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var customerId = await dbContext.Customers
            .Where(c => c.FullName == $"Test Müşteri{customerSuffix}")
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.Id)
            .FirstAsync();

        // Sepet
        var cartId = Guid.NewGuid();
        dbContext.Carts.Add(new Cart
        {
            Id = cartId,
            TenantId = 1,
            CustomerId = customerId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        dbContext.CartItems.Add(new CartItem
        {
            CartId = cartId,
            ProductVariantId = variantId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
        await dbContext.SaveChangesAsync();

        return (cartId, customerId);
    }

    private static CheckoutRequestDto CheckoutDto(string fullName) => new(
        ShippingFullName: fullName,
        ShippingPhone: "5551234567",
        ShippingCity: "Istanbul",
        ShippingDistrict: "Kadikoy",
        ShippingAddress: "Test Mah. Test Sok. No:1",
        ShippingPostalCode: "34000",
        UseSameAddressForBilling: true,
        BillingFullName: null,
        BillingCity: null,
        BillingAddress: null,
        OrderNote: null);
}
