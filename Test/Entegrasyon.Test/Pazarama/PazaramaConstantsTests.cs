using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaConstantsTests
{
    [Fact]
    public void PazaramaMarketPlaceId_ShouldBe5()
    {
        MarketPlaceConstants.PazaramaMarketPlaceId.Should().Be(5);
    }

    [Fact]
    public void PazaramaMarketPlaceId_ShouldNotConflictWithOtherIds()
    {
        var ids = new[]
        {
            MarketPlaceConstants.TrendyolMarketPlaceId,
            MarketPlaceConstants.N11MarketPlaceId,
            MarketPlaceConstants.HepsiburadaMarketPlaceId,
            MarketPlaceConstants.PazaramaMarketPlaceId
        };
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ImportSource_ShouldContainPazarama()
    {
        Enum.IsDefined(typeof(ImportSource), ImportSource.Pazarama).Should().BeTrue();
    }
}
