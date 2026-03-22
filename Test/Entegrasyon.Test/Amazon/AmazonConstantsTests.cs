using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Amazon;

public class AmazonConstantsTests
{
    [Fact]
    public void AmazonMarketPlaceId_Should_Be_5()
    {
        MarketPlaceConstants.AmazonMarketPlaceId.Should().Be(5);
    }

    [Fact]
    public void AmazonMarketPlaceId_Should_Differ_From_Others()
    {
        MarketPlaceConstants.AmazonMarketPlaceId.Should()
            .NotBe(MarketPlaceConstants.TrendyolMarketPlaceId)
            .And.NotBe(MarketPlaceConstants.N11MarketPlaceId)
            .And.NotBe(MarketPlaceConstants.HepsiburadaMarketPlaceId)
            .And.NotBe(MarketPlaceConstants.PazaramaMarketPlaceId);
    }

    [Fact]
    public void AmazonApi_StringConstant_Should_Be_Defined()
    {
        StringConstants.AmazonApi.Should().Be("AmazonApi");
    }

    [Fact]
    public void ImportSource_Amazon_Should_Be_104()
    {
        ((int)ImportSource.Amazon).Should().Be(104);
    }
}
