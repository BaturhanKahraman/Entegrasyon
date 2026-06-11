using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// ProductManager integration testleri — gercek PostgreSQL ile urun CRUD akislari.
/// </summary>
[Trait("Category", "Integration")]
public class ProductManagerIntegrationTests : IntegrationTestBase
{
    public ProductManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    /// <summary>
    /// Test icin gerekli Category, Brand ve BranchOffice seed eder.
    /// </summary>
    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // BranchOffice (urune stok eklemek icin gerekli)
        if (!await dbContext.BranchOffices.AnyAsync(b => b.Id == 1))
        {
            dbContext.BranchOffices.Add(new BranchOffice
            {
                Id = 1,
                Name = "Ana Depo",
                IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Brand
        if (!await dbContext.Brands.AnyAsync(b => b.Name == "Test Marka"))
        {
            dbContext.Brands.Add(new Brand
            {
                Name = "Test Marka",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Category
        if (!await dbContext.Categories.AnyAsync(c => c.Name == "Test Kategori Urun"))
        {
            dbContext.Categories.Add(new Category
            {
                Name = "Test Kategori Urun",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<(int BrandId, int CategoryId)> GetSeedIdsAsync()
    {
        using var dbContext = CreateDbContext();
        var brand = await dbContext.Brands.FirstAsync(b => b.Name == "Test Marka");
        var category = await dbContext.Categories.FirstAsync(c => c.Name == "Test Kategori Urun");
        return (brand.Id, category.Id);
    }

    private AddProductDto BuildValidDto(int brandId, int categoryId, string stockCode = "TST-INT-001", string barcode = "1234567890123")
        => new()
        {
            Title = "Entegrasyon Test Urun",
            Description = "Entegrasyon test aciklama",
            StockCode = stockCode,
            CategoryId = categoryId,
            BrandId = brandId,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new AddProductVariantDto
                {
                    Barcode = barcode,
                    ListPrice = 200,
                    SalePrice = 180,
                    CurrencyType = "TRY",
                    BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
                }
            ]
        };

    [Fact]
    public async Task AddProduct_ShouldCreateProduct_InDatabase()
    {
        // Arrange
        var (brandId, categoryId) = await GetSeedIdsAsync();
        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var dto = BuildValidDto(brandId, categoryId, "PROD-ADD-001", "9990000000001");

        // Act
        var result = await productService.AddProduct(dto);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Entegrasyon Test Urun");

        // Verify in DB
        using var dbContext = CreateDbContext();
        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.StockCode == "PROD-ADD-001");
        product.Should().NotBeNull();
        product!.ProductVariants.Should().HaveCount(1);
        product.ProductVariants.First().Barcode.Should().Be("9990000000001");
    }

    [Fact]
    public async Task AddProduct_ShouldReturnError_WhenDuplicateStockCode()
    {
        // Arrange
        var (brandId, categoryId) = await GetSeedIdsAsync();

        // Ilk urun
        var (productService1, scope1) = GetScopedService<IProductService>();
        using var _1 = scope1;
        var dto1 = BuildValidDto(brandId, categoryId, "DUP-STOCK-001", "9990000000010");
        var result1 = await productService1.AddProduct(dto1);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni stok kodu ile tekrar
        var (productService2, scope2) = GetScopedService<IProductService>();
        using var _2 = scope2;
        var dto2 = BuildValidDto(brandId, categoryId, "DUP-STOCK-001", "9990000000011");
        var result2 = await productService2.AddProduct(dto2);

        // Assert
        result2.Success.Should().BeFalse("Duplicate stock code should be rejected");
    }

    [Fact]
    public async Task AddProduct_ShouldSucceed_WhenAllStocksAreZero()
    {
        // Bug #3: Esnaf stoksuz ürün ekleyebilmeli (ön sipariş / yolda / tükenmiş).
        // "En az bir stok gir" iş kuralı kaldırıldı → stok 0 ile ürün kaydedilebilmeli.
        // Arrange
        var (brandId, categoryId) = await GetSeedIdsAsync();
        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var dto = new AddProductDto
        {
            Title = "Sifir Stok Urun",
            Description = "Test",
            StockCode = "ZERO-STOCK-001",
            CategoryId = categoryId,
            BrandId = brandId,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new AddProductVariantDto
                {
                    Barcode = "9990000000020",
                    ListPrice = 100,
                    SalePrice = 90,
                    CurrencyType = "TRY",
                    BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 }]
                }
            ]
        };

        // Act
        var result = await productService.AddProduct(dto);

        // Assert
        result.Success.Should().BeTrue("Stoksuz ürün artık kaydedilebilmeli (Bug #3)");
    }

    [Fact]
    public async Task SoftDeleteProduct_ShouldMarkAsDeleted()
    {
        // Arrange — urun ekle
        var (brandId, categoryId) = await GetSeedIdsAsync();
        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var dto = BuildValidDto(brandId, categoryId, "DEL-PROD-001", "9990000000030");
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        // Act — soft delete
        var (productService2, scope2) = GetScopedService<IProductService>();
        using var _2 = scope2;
        var deleteResult = await productService2.SoftDeleteProduct(addResult.Data!.Id);

        // Assert
        deleteResult.Success.Should().BeTrue(deleteResult.Message);

        using var dbContext = CreateDbContext();
        var deleted = await dbContext.MainProducts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == addResult.Data!.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductByBarcode_ShouldReturnProduct()
    {
        // Arrange
        var (brandId, categoryId) = await GetSeedIdsAsync();
        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var dto = BuildValidDto(brandId, categoryId, "BARCODE-TEST-001", "9990000000040");
        var addResult = await productService.AddProduct(dto);
        addResult.Success.Should().BeTrue(addResult.Message);

        // Act
        var (productService2, scope2) = GetScopedService<IProductService>();
        using var _2 = scope2;
        var result = await productService2.GetProductByBarcode("9990000000040");

        // Assert
        result.Success.Should().BeTrue(result.Message);
    }
}
