using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.N11;

public class N11ConstantsTests
{
    [Fact]
    public void N11MarketPlaceId_Should_Be_2()
    {
        MarketPlaceConstants.N11MarketPlaceId.Should().Be(2);
    }

    [Fact]
    public void N11MarketPlaceId_Should_Differ_From_Trendyol()
    {
        MarketPlaceConstants.N11MarketPlaceId.Should()
            .NotBe(MarketPlaceConstants.TrendyolMarketPlaceId);
    }

    [Fact]
    public void ImportSource_N11_Should_Be_101()
    {
        ((int)ImportSource.N11).Should().Be(101);
    }
}
