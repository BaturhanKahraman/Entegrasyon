using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// BrandMatchService integration testleri — gercek DB ile brand mapping CRUD akislari.
/// Mapping olusturma, duplicate kontrolu, unmapped brands, silme ve summary dogrulanir.
/// </summary>
[Trait("Category", "Integration")]
public class BrandMatchServiceIntegrationTests : IntegrationTestBase
{
    private int _brand1Id;
    private int _brand2Id;
    private int _brand3Id;

    public BrandMatchServiceIntegrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // Seed MarketPlace Trendyol (id=1)
        var marketPlace = new MarketPlace
        {
            Id = 1,
            Name = "Trendyol",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MarketPlaces.Add(marketPlace);

        // Seed 3 Brand
        var brand1 = new Brand { Name = "Nike", CreatedAt = DateTimeOffset.UtcNow };
        var brand2 = new Brand { Name = "Adidas", CreatedAt = DateTimeOffset.UtcNow };
        var brand3 = new Brand { Name = "Puma", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Brands.AddRange(brand1, brand2, brand3);
        await dbContext.SaveChangesAsync();

        _brand1Id = brand1.Id;
        _brand2Id = brand2.Id;
        _brand3Id = brand3.Id;
    }

    [Fact]
    public async Task CreateBrandMapping_ShouldCreateRecord()
    {
        // Arrange
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;

        var dto = new CreateBrandMarketPlaceMatchDto
        {
            ApplicationBrandId = _brand1Id,
            MarketPlaceId = 1,
            MarketPlaceBrandId = 9001
        };

        // Act
        var result = await service.CreateBrandMappingAsync(dto);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var mapping = await dbContext.BrandMarketPlaceMatches
            .FirstOrDefaultAsync(m => m.ApplicationBrandId == _brand1Id && m.MarketPlaceId == 1);
        mapping.Should().NotBeNull();
        mapping!.MarketPlaceBrandId.Should().Be(9001);
    }

    [Fact]
    public async Task CreateBrandMapping_ShouldFail_WhenDuplicate()
    {
        // Arrange — ilk mapping'i olustur
        var (service1, scope1) = GetScopedService<IBrandMatchService>();
        using var _1 = scope1;

        var dto = new CreateBrandMarketPlaceMatchDto
        {
            ApplicationBrandId = _brand2Id,
            MarketPlaceId = 1,
            MarketPlaceBrandId = 9002
        };
        var result1 = await service1.CreateBrandMappingAsync(dto);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni brand + marketplace icin tekrar dene
        var (service2, scope2) = GetScopedService<IBrandMatchService>();
        using var _2 = scope2;
        var result2 = await service2.CreateBrandMappingAsync(dto);

        // Assert
        result2.Success.Should().BeFalse("Duplicate mapping should be rejected");
        result2.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task CreateBrandMapping_ShouldFail_WhenBrandNotFound()
    {
        // Arrange
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;

        var dto = new CreateBrandMarketPlaceMatchDto
        {
            ApplicationBrandId = 999999, // Olmayan brand
            MarketPlaceId = 1,
            MarketPlaceBrandId = 9099
        };

        // Act
        var result = await service.CreateBrandMappingAsync(dto);

        // Assert
        result.Success.Should().BeFalse("Non-existent brand should cause failure");
    }

    [Fact]
    public async Task GetUnmappedBrands_ShouldReturnOnlyUnmapped()
    {
        // Arrange — brand1'e mapping olustur
        using var dbContext = CreateDbContext();
        dbContext.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
        {
            ApplicationBrandId = _brand1Id,
            MarketPlaceId = 1,
            MarketPlaceBrandId = 8001
        });
        await dbContext.SaveChangesAsync();

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var unmapped = await service.GetUnmappedBrandsAsync(1);

        // Assert — brand1 mapped, brand2 ve brand3 unmapped
        unmapped.Should().HaveCount(2, "Only unmapped brands should be returned");
        unmapped.Select(b => b.Id).Should().Contain(_brand2Id);
        unmapped.Select(b => b.Id).Should().Contain(_brand3Id);
        unmapped.Select(b => b.Id).Should().NotContain(_brand1Id, "Mapped brand should not appear");
    }

    [Fact]
    public async Task RemoveBrandMapping_ShouldDelete()
    {
        // Arrange — mapping olustur
        using var dbContext = CreateDbContext();
        dbContext.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
        {
            ApplicationBrandId = _brand3Id,
            MarketPlaceId = 1,
            MarketPlaceBrandId = 8003
        });
        await dbContext.SaveChangesAsync();

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var result = await service.RemoveBrandMappingAsync(_brand3Id, 1);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyContext = CreateDbContext();
        var exists = await verifyContext.BrandMarketPlaceMatches
            .AnyAsync(m => m.ApplicationBrandId == _brand3Id && m.MarketPlaceId == 1);
        exists.Should().BeFalse("Mapping should be deleted from DB");
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_ShouldReturnAllMappingsForBrand()
    {
        // Arrange — brand1'e iki farkli marketplace'te mapping olustur
        using var dbContext = CreateDbContext();

        // Ikinci marketplace ekle
        dbContext.MarketPlaces.Add(new MarketPlace
        {
            Id = 2,
            Name = "Hepsiburada",
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.BrandMarketPlaceMatches.AddRange(
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand1Id, MarketPlaceId = 1, MarketPlaceBrandId = 6001 },
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand1Id, MarketPlaceId = 2, MarketPlaceBrandId = 6002 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var result = await service.GetBrandMappingsByBrandIdAsync(_brand1Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(m => m.ApplicationBrandId == _brand1Id);
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_ShouldReturnEmpty_WhenNoMappingsExist()
    {
        // Arrange — brand3 icin hicbir mapping yok

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var result = await service.GetBrandMappingsByBrandIdAsync(_brand3Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_ShouldNotReturnOtherBrandMappings()
    {
        // Arrange — brand1 ve brand2'ye mapping olustur, sadece brand1'inkiler donmeli
        using var dbContext = CreateDbContext();
        dbContext.BrandMarketPlaceMatches.AddRange(
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand1Id, MarketPlaceId = 1, MarketPlaceBrandId = 5001 },
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand2Id, MarketPlaceId = 1, MarketPlaceBrandId = 5002 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var result = await service.GetBrandMappingsByBrandIdAsync(_brand1Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data.First().ApplicationBrandId.Should().Be(_brand1Id);
    }

    [Fact]
    public async Task GetBrandMappingsSummary_ShouldReturnCorrectCounts()
    {
        // Arrange — brand1 ve brand2'ye mapping olustur (summary yalnizca Trendyol=1 icin)
        using var dbContext = CreateDbContext();
        dbContext.BrandMarketPlaceMatches.AddRange(
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand1Id, MarketPlaceId = 1, MarketPlaceBrandId = 7001 },
            new BrandMarketPlaceMatch { ApplicationBrandId = _brand2Id, MarketPlaceId = 1, MarketPlaceBrandId = 7002 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var (service, scope) = GetScopedService<IBrandMatchService>();
        using var _ = scope;
        var summary = await service.GetBrandMappingsSummaryAsync();

        // Assert
        summary.TotalBrands.Should().Be(3, "3 brand seeded");
        summary.MappedBrands.Should().Be(2, "2 brands mapped to Trendyol");
        summary.UnmappedBrands.Should().Be(1, "1 brand unmapped");
    }
}
