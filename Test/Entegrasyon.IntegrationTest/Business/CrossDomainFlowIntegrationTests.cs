using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Cross-domain akis testleri — birden fazla servisi zincirleyerek uctan uca is akislarini dogrular.
/// Product → Order → Stock → Sale → Sync gibi servisler arasi etkilesimleri test eder.
/// </summary>
[Trait("Category", "Integration")]
public class CrossDomainFlowIntegrationTests : IntegrationTestBase
{
    private int _brandId;
    private int _categoryId;
    private Guid _userId;
    private int _customerId;

    public CrossDomainFlowIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // BranchOffice
        if (!await dbContext.BranchOffices.AnyAsync(b => b.Id == 1))
        {
            dbContext.BranchOffices.Add(new BranchOffice
            {
                Id = 1,
                Name = "Ana Depo CrossDomain",
                IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Brand
        var brand = new Brand { Name = "CrossDomain Marka", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Brands.Add(brand);

        // Category
        var category = new Category { Name = "CrossDomain Kategori", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Categories.Add(category);

        // MarketPlace (Trendyol=1, N11=2)
        if (!await dbContext.MarketPlaces.AnyAsync(m => m.Id == 1))
        {
            dbContext.MarketPlaces.Add(new MarketPlace
            {
                Id = 1,
                Name = "Trendyol",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.MarketPlaces.AnyAsync(m => m.Id == 2))
        {
            dbContext.MarketPlaces.Add(new MarketPlace
            {
                Id = 2,
                Name = "N11",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();

        // MarketPlaceWarehouse — Trendyol icin depo baglantisi
        if (!await dbContext.MarketPlaceWarehouses.AnyAsync(w => w.MarketPlaceId == 1 && w.BranchOfficeId == 1))
        {
            dbContext.MarketPlaceWarehouses.Add(new MarketPlaceWarehouse
            {
                MarketPlaceId = 1,
                BranchOfficeId = 1,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // User (SaleManager icin gerekli)
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Surname = "User",
            FullName = "Test User",
            Email = "test@crossdomain.com",
            UserName = "testcrossdomain",
            NormalizedUserName = "TESTCROSSDOMAIN",
            NormalizedEmail = "TEST@CROSSDOMAIN.COM",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);

        // Customer
        var customer = new Customer
        {
            Name = "Test",
            Surname = "Musteri",
            FullName = "Test Musteri",
            CustomerType = "Retail",
            PhoneNumber = "5551234567",
            Address = new Address
            {
                City = "Istanbul",
                Country = "TR",
                County = "Kadikoy",
                Street = "",
                ZipCode = "34000",
                FullAddress = "Test adres"
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Set<Customer>().Add(customer);

        await dbContext.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
        _userId = user.Id;
        _customerId = customer.Id;
    }

    private AddProductDto BuildProductDto(string stockCode, string barcode, int stock = 20)
        => new()
        {
            Title = $"CrossDomain Urun {stockCode}",
            Description = "CrossDomain test aciklama",
            StockCode = stockCode,
            CategoryId = _categoryId,
            BrandId = _brandId,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new AddProductVariantDto
                {
                    Barcode = barcode,
                    ListPrice = 200,
                    SalePrice = 180,
                    CurrencyType = "TRY",
                    BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = stock }]
                }
            ]
        };

    private static TrendyolShipmentPackage BuildTrendyolPackage(long packageId, string orderNumber, string barcode, int qty = 1)
        => new(
            ShipmentPackageId: packageId,
            OrderNumber: orderNumber,
            OrderDate: DateTimeOffset.UtcNow.ToString("o"),
            Status: "Created",
            GrossAmount: 200,
            TotalDiscount: 20,
            TotalPrice: 180,
            Micro: false,
            FastDelivery: false,
            EstimatedDeliveryEndDate: null,
            CargoProviderInfo: new TrendyolCargoInfo("Aras Kargo", null, null),
            CustomerInfo: new TrendyolCustomerInfo("Test", "Musteri", "test@test.com"),
            ShipmentAddress: new TrendyolAddressInfo("Istanbul", "Kadikoy", "Test Adres", "34000", "TR"),
            InvoiceAddress: new TrendyolAddressInfo("Istanbul", "Kadikoy", "Test Fatura Adres", "34000", "TR"),
            Lines:
            [
                new TrendyolOrderLine(
                    LineId: 1000 + packageId,
                    Quantity: qty,
                    Price: 200,
                    Discount: 20,
                    Barcode: barcode,
                    MerchantSku: $"SKU-{orderNumber}",
                    ProductName: "Test Urun",
                    ProductColor: "Siyah",
                    ProductSize: "M",
                    MerchantId: 12345)
            ]);

    [Fact]
    public async Task AddProduct_ThenImportOrder_ShouldDecreaseStock()
    {
        // Arrange — urun ekle
        var (productService, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto = BuildProductDto("CD-STOCK-001", "8880000000001", stock: 20);
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        var variantId = addResult.Data!.ProductVariants.First().Id;

        // Act — Trendyol Sipariş import et (barkod Eşleşmeli)
        var (orderManager, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var packages = new List<TrendyolShipmentPackage>
        {
            BuildTrendyolPackage(80001, "CD-ORD-001", "8880000000001", qty: 3)
        };
        var importResult = await orderManager.ImportTrendyolOrdersAsync(packages);
        importResult.Success.Should().BeTrue(importResult.Message);

        // Assert — stok dusmus olmali
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstOrDefaultAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.Should().NotBeNull();
        stock!.SoldQuantity.Should().Be(3);
        stock.CurrentStock.Should().Be(17); // 20 - 3

        // Sipariş DB'de olmali
        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.ShipmentPackageId == 80001);
        order.Should().NotBeNull();
        order!.OrderItems.Should().Contain(oi => oi.ProductId == variantId);
    }

    [Fact]
    public async Task AddProduct_ThenSync_ShouldCreateProductMarketplace()
    {
        // Arrange — urun ekle
        var (productService, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto = BuildProductDto("CD-SYNC-001", "8880000000002");
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        var productId = addResult.Data!.Id;

        // Act — marketplace sync
        var (syncManager, scope2) = GetScopedService<IProductSyncManager>();
        using var _2 = scope2;
        var syncResult = await syncManager.SyncProductAsync(productId, 1);
        syncResult.Success.Should().BeTrue(syncResult.Message);

        // Assert — ProductMarketplace Pending kaydi olusmus olmali
        using var dbContext = CreateDbContext();
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MarketPlaceId == 1);
        pm.Should().NotBeNull();
        pm!.Status.Should().Be(MarketplaceProductStatus.Pending);
    }

    [Fact]
    public async Task AddProduct_Sale_ThenOrder_ForceDecrease()
    {
        // Arrange — stock=5 urun ekle
        var (productService, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto = BuildProductDto("CD-FORCE-001", "8880000000003", stock: 5);
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        var variantId = addResult.Data!.ProductVariants.First().Id;

        // Act 1 — satis yap (4 adet), stok 5 -> 1
        var (saleManager, scope2) = GetScopedService<ISaleManager>();
        using var _2 = scope2;
        var saleDto = new MakeSaleDto(
            SalePersonId: _userId,
            CustomerId: _customerId,
            GeneralDiscount: 0,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: [new SaleItemDto(variantId, 18, 0, 180, 4, "")],
            Payments: []);
        var saleResult = await saleManager.MakeSale(saleDto);
        saleResult.Success.Should().BeTrue(saleResult.Message);

        // Act 2 — Sipariş import (3 adet), stok=1 < 3 → ForceDecrease
        var (orderManager, scope3) = GetScopedService<IOrderManager>();
        using var _3 = scope3;
        var packages = new List<TrendyolShipmentPackage>
        {
            BuildTrendyolPackage(80003, "CD-FORCE-ORD-001", "8880000000003", qty: 3)
        };
        var importResult = await orderManager.ImportTrendyolOrdersAsync(packages);
        importResult.Success.Should().BeTrue(importResult.Message);

        // Assert — stok negatife dustuyse ForceDecrease calismis demek
        using var dbContext = CreateDbContext();
        var stock = await dbContext.BranchOfficeStocks
            .FirstOrDefaultAsync(s => s.ProductVariantId == variantId && s.BranchOfficeId == 1);
        stock.Should().NotBeNull();
        stock!.SoldQuantity.Should().Be(7); // 4 (satis) + 3 (Sipariş)
        stock.CurrentStock.Should().Be(-2); // 5 - 7

        // StockMovement kayitlari kontrol
        var movements = await dbContext.StockMovements
            .Where(m => m.ProductVariantId == variantId)
            .OrderBy(m => m.Id)
            .ToListAsync();
        movements.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CategoryHierarchy_ParentChild_Relationship()
    {
        // Arrange — parent kategori olustur
        var (categoryService, scope1) = GetScopedService<ICategoryService>();
        using var _1 = scope1;

        var parentDto = new AddCategoryDtoStepOne("CD Parent Kategori", null, false);
        var parentResult = await categoryService.AddCategoryStepOne(parentDto);
        parentResult.Success.Should().BeTrue(parentResult.Message);

        using var dbCtx1 = CreateDbContext();
        var parentCategory = await dbCtx1.Categories.FirstAsync(c => c.Name == "CD Parent Kategori");

        // Act — child kategori ekle
        var (categoryService2, scope2) = GetScopedService<ICategoryService>();
        using var _2 = scope2;

        var childDto = new AddCategoryDtoStepOne("CD Child Kategori", parentCategory.Id, false);
        var childResult = await categoryService2.AddCategoryStepOne(childDto);
        childResult.Success.Should().BeTrue(childResult.Message);

        // Assert
        using var dbContext = CreateDbContext();
        var child = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "CD Child Kategori");
        child.Should().NotBeNull();
        child!.SuperCategoryId.Should().Be(parentCategory.Id);
    }

    [Fact]
    public async Task SoftDelete_Product_ShouldNotOrphanOrderItems()
    {
        // Arrange — urun ekle + Sipariş import et
        var (productService, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto = BuildProductDto("CD-DEL-001", "8880000000005", stock: 10);
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        var productId = addResult.Data!.Id;
        var variantId = addResult.Data.ProductVariants.First().Id;

        // Sipariş import et
        var (orderManager, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        await orderManager.ImportTrendyolOrdersAsync([
            BuildTrendyolPackage(80005, "CD-DEL-ORD-001", "8880000000005")
        ]);

        // Act — urun soft delete
        var (productService2, scope3) = GetScopedService<IProductService>();
        using var _3 = scope3;
        var deleteResult = await productService2.SoftDeleteProduct(productId);
        deleteResult.Success.Should().BeTrue(deleteResult.Message);

        // Assert — OrderItems hala var, ProductId referansi korunuyor
        using var dbContext = CreateDbContext();
        var orderItems = await dbContext.Set<Entity.Orders.OrderItem>()
            .Where(oi => oi.ProductId == variantId)
            .ToListAsync();
        orderItems.Should().NotBeEmpty("OrderItems should survive product soft delete");

        // Urun sorgulanamaz (soft delete query filter)
        var product = await dbContext.MainProducts
            .FirstOrDefaultAsync(p => p.Id == productId);
        product.Should().BeNull("Soft deleted product should not appear in normal queries");
    }

    [Fact]
    public async Task MultipleMarketplace_SameProduct_IndependentSync()
    {
        // Arrange — urun ekle
        var (productService, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto = BuildProductDto("CD-MULTI-001", "8880000000006");
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        var productId = addResult.Data!.Id;

        // Act — iki farkli marketplace'e sync
        var (syncManager1, scope2) = GetScopedService<IProductSyncManager>();
        using var _2 = scope2;
        var sync1 = await syncManager1.SyncProductAsync(productId, 1); // Trendyol
        sync1.Success.Should().BeTrue(sync1.Message);

        var (syncManager2, scope3) = GetScopedService<IProductSyncManager>();
        using var _3 = scope3;
        var sync2 = await syncManager2.SyncProductAsync(productId, 2); // N11
        sync2.Success.Should().BeTrue(sync2.Message);

        // Assert — 2 ayri ProductMarketplace kaydi olmali
        using var dbContext = CreateDbContext();
        var marketplaces = await dbContext.ProductMarketplaces
            .Where(pm => pm.ProductId == productId)
            .ToListAsync();

        marketplaces.Should().HaveCount(2);
        marketplaces.Should().Contain(pm => pm.MarketPlaceId == 1);
        marketplaces.Should().Contain(pm => pm.MarketPlaceId == 2);
        marketplaces.Should().AllSatisfy(pm => pm.Status.Should().Be(MarketplaceProductStatus.Pending));
    }
}
