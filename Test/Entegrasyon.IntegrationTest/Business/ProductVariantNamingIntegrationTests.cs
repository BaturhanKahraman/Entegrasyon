using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Business;

[Trait("Category", "Integration")]
public class ProductVariantNamingIntegrationTests : IntegrationTestBase
{
    public ProductVariantNamingIntegrationTests(PostgreSqlFixture pg, WireMockFixture wm) : base(pg, wm) { }

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();
        if (!await db.BranchOffices.AnyAsync(b => b.Id == 1))
            db.BranchOffices.Add(new BranchOffice
            {
                Id = 1,
                Name = "Ana Depo",
                IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

        if (!await db.Brands.AnyAsync(b => b.Name == "Naming Marka"))
            db.Brands.Add(new Brand { Name = "Naming Marka", CreatedAt = DateTimeOffset.UtcNow });

        if (!await db.Categories.AnyAsync(c => c.Name == "Naming Kategori"))
            db.Categories.Add(new Category { Name = "Naming Kategori", CreatedAt = DateTimeOffset.UtcNow });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddProduct_ComputesVariantName_WithVarianterAndSlicer()
    {
        using var db = CreateDbContext();
        var brandId = (await db.Brands.FirstAsync(b => b.Name == "Naming Marka")).Id;
        var categoryId = (await db.Categories.FirstAsync(c => c.Name == "Naming Kategori")).Id;

        var dto = new AddProductDto
        {
            Title = "Naming Test Tshirt",
            Description = "Test",
            StockCode = "NAME-TST-001",
            BrandId = brandId,
            CategoryId = categoryId,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new AddProductVariantDto
                {
                    Barcode = "NAME0001",
                    SalePrice = 100m,
                    CostPrice = 50m,
                    VatRate = 20m,
                    CurrencyType = "TRY",
                    ProductVariantAttributes =
                    [
                        new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true },
                        new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true }
                    ],
                    BranchOfficeStocks =
                    [
                        new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }
                    ]
                }
            ]
        };

        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var result = await productService.AddProduct(dto);
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.ProductVariants.AsNoTracking()
            .FirstAsync(v => v.Barcode == "NAME0001");

        saved.Name.Should().Be("Sarı XL");
    }

    [Fact]
    public async Task AddProduct_VariantWithoutVarianterOrSlicer_FallsBackToProductTitle()
    {
        using var db = CreateDbContext();
        var brandId = (await db.Brands.FirstAsync(b => b.Name == "Naming Marka")).Id;
        var categoryId = (await db.Categories.FirstAsync(c => c.Name == "Naming Kategori")).Id;

        var dto = new AddProductDto
        {
            Title = "Sade Urun",
            Description = "",
            StockCode = "NAME-SADE-001",
            BrandId = brandId,
            CategoryId = categoryId,
            AttributeKeyValues = [],
            ProductVariants =
            [
                new AddProductVariantDto
                {
                    Barcode = "NAME0002",
                    SalePrice = 50m,
                    CostPrice = 25m,
                    VatRate = 20m,
                    CurrencyType = "TRY",
                    ProductVariantAttributes = [],
                    BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 5 }]
                }
            ]
        };

        var (productService, scope) = GetScopedService<IProductService>();
        using var _ = scope;

        var result = await productService.AddProduct(dto);
        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.ProductVariants.AsNoTracking()
            .FirstAsync(v => v.Barcode == "NAME0002");

        saved.Name.Should().Be("Sade Urun");
    }

    [Fact]
    public async Task Migration_AllowsNullName_OnLegacyVariant()
    {
        using var db = CreateDbContext();
        var brandId = (await db.Brands.FirstAsync(b => b.Name == "Naming Marka")).Id;
        var categoryId = (await db.Categories.FirstAsync(c => c.Name == "Naming Kategori")).Id;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Legacy Urun",
            StockCode = "LEG-001",
            Description = "",
            BrandId = brandId,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    Barcode = "LEG001",
                    SalePrice = 1m,
                    CostPrice = 1m,
                    ListPrice = 1m,
                    ECommercePrice = 1m,
                    Name = null,
                    CurrencyType = "TRY",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };
        db.MainProducts.Add(product);
        await db.SaveChangesAsync();

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.ProductVariants.AsNoTracking()
            .FirstAsync(v => v.Barcode == "LEG001");

        saved.Name.Should().BeNull();
    }
}
