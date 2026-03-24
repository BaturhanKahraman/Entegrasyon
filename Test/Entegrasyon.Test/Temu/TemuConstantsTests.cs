using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Test.Temu;

/// <summary>
/// Temu constants and enum tests — verifies MarketPlaceId=9 and ImportSource.Temu=107.
/// </summary>
public class TemuConstantsTests
{
    [Fact]
    public void TemuMarketPlaceId_ShouldBe9()
    {
        MarketPlaceConstants.TemuMarketPlaceId.Should().Be(9);
    }

    [Fact]
    public void ImportSource_Temu_ShouldBe107()
    {
        ((int)ImportSource.Temu).Should().Be(107);
    }

    [Fact]
    public void TemuMarketPlaceId_ShouldNotConflictWithOtherIds()
    {
        var allIds = new[]
        {
            MarketPlaceConstants.TrendyolMarketPlaceId,
            MarketPlaceConstants.N11MarketPlaceId,
            MarketPlaceConstants.HepsiburadaMarketPlaceId,
            MarketPlaceConstants.PazaramaMarketPlaceId,
            MarketPlaceConstants.AmazonMarketPlaceId,
            MarketPlaceConstants.PttavmMarketPlaceId,
            MarketPlaceConstants.CiceksepetiMarketPlaceId,
            MarketPlaceConstants.TemuMarketPlaceId
        };

        allIds.Should().OnlyHaveUniqueItems();
    }
}
