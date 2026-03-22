using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Hepsiburada;

public class HepsiburadaConstantsTests
{
    [Fact]
    public void HepsiburadaMarketPlaceId_Should_Be_3()
    {
        MarketPlaceConstants.HepsiburadaMarketPlaceId.Should().Be(3);
    }

    [Fact]
    public void HepsiburadaMarketPlaceId_Should_Differ_From_Other_MarketPlaces()
    {
        MarketPlaceConstants.HepsiburadaMarketPlaceId.Should()
            .NotBe(MarketPlaceConstants.TrendyolMarketPlaceId);
        MarketPlaceConstants.HepsiburadaMarketPlaceId.Should()
            .NotBe(MarketPlaceConstants.N11MarketPlaceId);
    }

    [Fact]
    public void HepsiburadaApi_StringConstant_Should_Be_Defined()
    {
        StringConstants.HepsiburadaApi.Should().Be("HepsiburadaApi");
    }

    [Fact]
    public void ImportSource_Hepsiburada_Should_Be_102()
    {
        ((int)ImportSource.Hepsiburada).Should().Be(102);
    }
}
