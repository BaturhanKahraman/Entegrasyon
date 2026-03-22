using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Pttavm;

public class PttavmConstantsTests
{
    [Fact]
    public void PttavmMarketPlaceId_ShouldBe7()
    {
        MarketPlaceConstants.PttavmMarketPlaceId.Should().Be(7);
    }

    [Fact]
    public void PttavmMarketPlaceId_ShouldNotConflictWithOtherIds()
    {
        var ids = new[]
        {
            MarketPlaceConstants.TrendyolMarketPlaceId,
            MarketPlaceConstants.N11MarketPlaceId,
            MarketPlaceConstants.HepsiburadaMarketPlaceId,
            MarketPlaceConstants.PazaramaMarketPlaceId,
            MarketPlaceConstants.AmazonMarketPlaceId,
            MarketPlaceConstants.PttavmMarketPlaceId
        };

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ImportSource_ShouldContainPttavm()
    {
        Enum.IsDefined(typeof(ImportSource), ImportSource.Pttavm).Should().BeTrue();
    }

    [Fact]
    public void ImportSource_Pttavm_ShouldBe105()
    {
        ((int)ImportSource.Pttavm).Should().Be(105);
    }
}
