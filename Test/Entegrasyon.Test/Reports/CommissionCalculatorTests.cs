using Entegrasyon.Business.Utilities;

namespace Entegrasyon.UnitTest.Reports;

public class CommissionCalculatorTests
{
    [Fact]
    public void GetDefaultRate_Trendyol_Returns13_5()
    {
        var rate = MarketplaceCommissionEstimator.GetDefaultRate(1);
        rate.Should().Be(13.5m);
    }

    [Fact]
    public void GetDefaultRate_Hepsiburada_Returns12()
    {
        var rate = MarketplaceCommissionEstimator.GetDefaultRate(3);
        rate.Should().Be(12.0m);
    }

    [Fact]
    public void GetDefaultRate_N11_Returns10()
    {
        var rate = MarketplaceCommissionEstimator.GetDefaultRate(2);
        rate.Should().Be(10.0m);
    }

    [Fact]
    public void GetDefaultRate_UnknownMarketplace_ReturnsFallbackRate()
    {
        var rate = MarketplaceCommissionEstimator.GetDefaultRate(999);
        rate.Should().Be(12.0m);
    }

    [Theory]
    [InlineData(1, 100, 13.50)]   // Trendyol
    [InlineData(3, 200, 24.00)]   // Hepsiburada
    [InlineData(2, 500, 50.00)]   // N11
    [InlineData(999, 100, 12.00)] // Unknown
    public void EstimateCommission_CalculatesCorrectly(int marketPlaceId, decimal salePrice, decimal expected)
    {
        var commission = MarketplaceCommissionEstimator.EstimateCommission(salePrice, marketPlaceId);
        commission.Should().Be(expected);
    }

    [Fact]
    public void EstimateCommission_ZeroPrice_ReturnsZero()
    {
        var commission = MarketplaceCommissionEstimator.EstimateCommission(0m, 1);
        commission.Should().Be(0m);
    }

    [Fact]
    public void EstimateCommission_NegativePrice_ReturnsZero()
    {
        var commission = MarketplaceCommissionEstimator.EstimateCommission(-50m, 1);
        commission.Should().Be(0m);
    }

    [Fact]
    public void EstimateNetProfit_CalculatesCorrectly()
    {
        // 100 TL sale - 13.50 TL commission - 50 TL cost = 36.50 TL net profit
        var netProfit = MarketplaceCommissionEstimator.EstimateNetProfit(100m, 50m, 1);
        netProfit.Should().Be(36.50m);
    }

    [Fact]
    public void EstimateNetProfit_ZeroSalePrice_ReturnsNegativeCost()
    {
        var netProfit = MarketplaceCommissionEstimator.EstimateNetProfit(0m, 50m, 1);
        netProfit.Should().Be(-50m);
    }
}
