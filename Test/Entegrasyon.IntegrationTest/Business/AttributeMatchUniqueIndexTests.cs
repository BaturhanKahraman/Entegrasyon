using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

public class AttributeMatchUniqueIndexTests : IntegrationTestBase
{
    private int _attr1Id;
    private int _attr2Id;

    public AttributeMatchUniqueIndexTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        dbContext.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });

        var attr1 = new CategoryAttribute { CategoryAttributeKey = "color", CategoryAttributeHumanized = "Renk", CreatedAt = DateTimeOffset.UtcNow };
        var attr2 = new CategoryAttribute { CategoryAttributeKey = "maincolor", CategoryAttributeHumanized = "Ana Renk", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.CategoryAttributes.AddRange(attr1, attr2);
        await dbContext.SaveChangesAsync();

        _attr1Id = attr1.Id;
        _attr2Id = attr2.Id;
    }

    [Fact]
    public async Task SameTrendyolAttribute_MappedToTwoOfOurAttributes_Throws()
    {
        // Arrange — aynı Trendyol attribute (348) bizim attr1'e eşli
        using (var ctx = CreateDbContext())
        {
            ctx.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
            {
                ApplicationCategoryAttributeId = _attr1Id, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 348
            });
            await ctx.SaveChangesAsync();
        }

        // Act + Assert — aynı (mp=1, 348) bizim attr2'ye → unique index ihlali
        using var ctx2 = CreateDbContext();
        ctx2.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = _attr2Id, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 348
        });

        var act = async () => await ctx2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
