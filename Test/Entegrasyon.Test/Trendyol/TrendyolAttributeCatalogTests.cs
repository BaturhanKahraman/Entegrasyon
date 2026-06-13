using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Trendyol;

public class TrendyolAttributeCatalogTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICategoryMatchService> _categoryMatch = new();
    private readonly Mock<IMarketplaceCategoryAttributeProvider> _provider = new();
    private readonly TenantMemoryCache _cache;

    public TrendyolAttributeCatalogTests()
    {
        _cache = new TenantMemoryCache(new MemoryCache(new MemoryCacheOptions()), mockTenantContext.Object);
        _provider.Setup(p => p.MarketPlaceId).Returns(1);
    }

    private TrendyolAttributeCatalog CreateSut() => new(
        _categoryMatch.Object,
        new[] { _provider.Object },
        _cache,
        Mock.Of<ILogger<TrendyolAttributeCatalog>>());

    private void SetupMappedCategories(params int[] trendyolCategoryIds)
    {
        _categoryMatch
            .Setup(s => s.GetAllCategoryMappingsAsync(1))
            .ReturnsAsync(trendyolCategoryIds.Select(id => new CategoryMarketplaceMappingDto
            {
                ApplicationCategoryId = id,
                MarketPlaceId = 1,
                MarketPlaceCategoryId = id
            }).ToList());
    }

    private void SetupCategoryAttributes(int categoryId, params MarketplaceAttributeDto[] attrs)
    {
        _provider
            .Setup(p => p.GetAttributesForCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceAttributeDto>>(attrs.ToList()));
    }

    [Fact]
    public async Task SearchAttributesAsync_AggregatesAndDedupesAcrossMappedCategories()
    {
        // Arrange — iki kategori, ikisinde de "Renk" (348) tekrar ediyor
        SetupMappedCategories(388, 400);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, []),
            new MarketplaceAttributeDto(338, "Beden", true, false, []));
        SetupCategoryAttributes(400,
            new MarketplaceAttributeDto(348, "Renk", true, false, []));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchAttributesAsync("");

        // Assert — Renk + Beden, dedupe sonrası 2
        result.Should().HaveCount(2);
        result.Select(a => a.Id).Should().BeEquivalentTo(new[] { 348, 338 });
    }

    [Fact]
    public async Task SearchAttributesAsync_FiltersByNameCaseInsensitive()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, []),
            new MarketplaceAttributeDto(338, "Beden", true, false, []));

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync("renk");

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Renk");
    }

    [Fact]
    public async Task SearchAttributesAsync_NoMappedCategories_ReturnsEmpty()
    {
        SetupMappedCategories(); // hiç eşli kategori yok

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync("renk");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchValuesAsync_ReturnsValuesForGivenMarketplaceAttribute()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, new()
            {
                new MarketplaceAttributeValueDto(1001, "Mavi"),
                new MarketplaceAttributeValueDto(1002, "Kırmızı")
            }));

        var sut = CreateSut();

        var result = await sut.SearchValuesAsync(348, "kır");

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1002);
        result[0].Name.Should().Be("Kırmızı");
    }

    [Fact]
    public async Task SearchAttributesAsync_UsesCache_ProviderCalledOncePerCategory()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388, new MarketplaceAttributeDto(348, "Renk", true, false, []));

        var sut = CreateSut();

        await sut.SearchAttributesAsync("a");
        await sut.SearchAttributesAsync("b");

        _provider.Verify(p => p.GetAttributesForCategoryAsync(388, It.IsAny<CancellationToken>()), Times.Once);
    }
}
