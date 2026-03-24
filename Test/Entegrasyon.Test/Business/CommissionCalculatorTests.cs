using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Marketplace;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class CommissionCalculatorTests : BaseTest
{
    private readonly ICommissionCalculator _calculator;
    private readonly Mock<ILogger<CommissionCalculator>> _mockLogger;

    public CommissionCalculatorTests()
    {
        _mockLogger = new Mock<ILogger<CommissionCalculator>>();
        MockValidator = new Mock<IFluentValidator>();
        _calculator = new CommissionCalculator(
            mockContextFactory.Object,
            MockValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateAsync_WithDefaultRate_ShouldReturnCorrectResult()
    {
        // Arrange
        var rates = new List<MarketplaceCommissionRate>
        {
            new()
            {
                Id = 1,
                MarketPlaceId = 1,
                CategoryId = null,
                CommissionPercent = 10m,
                ServiceFeePercent = 2m,
                TransactionFeeFixed = 3m,
                IsDefault = true
            }
        };

        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates).ReturnsDbSet(rates);

        // Act
        var result = await _calculator.CalculateAsync(1, null, 100m, 50m);

        // Assert
        result.Success.Should().BeTrue();
        var calc = result.Data;
        calc.SalePrice.Should().Be(100m);
        calc.CommissionAmount.Should().Be(10m);     // 100 * 10%
        calc.ServiceFeeAmount.Should().Be(2m);       // 100 * 2%
        calc.TransactionFee.Should().Be(3m);
        calc.TotalDeductions.Should().Be(15m);       // 10 + 2 + 3
        calc.NetRevenue.Should().Be(85m);            // 100 - 15
        calc.CostPrice.Should().Be(50m);
        calc.NetProfit.Should().Be(35m);             // 85 - 50
        calc.ProfitMarginPercent.Should().Be(35m);   // 35 / 100 * 100
    }

    [Fact]
    public async Task CalculateAsync_WithCategoryRate_ShouldPreferCategoryOverDefault()
    {
        // Arrange
        var rates = new List<MarketplaceCommissionRate>
        {
            new()
            {
                Id = 1,
                MarketPlaceId = 1,
                CategoryId = null,
                CommissionPercent = 10m,
                IsDefault = true
            },
            new()
            {
                Id = 2,
                MarketPlaceId = 1,
                CategoryId = 5,
                CommissionPercent = 15m,
                ServiceFeePercent = 3m,
                IsDefault = false
            }
        };

        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates).ReturnsDbSet(rates);

        // Act
        var result = await _calculator.CalculateAsync(1, 5, 200m, 100m);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.CommissionAmount.Should().Be(30m);    // 200 * 15%
        result.Data.ServiceFeeAmount.Should().Be(6m);     // 200 * 3%
    }

    [Fact]
    public async Task CalculateAsync_WithNoRate_ShouldReturnError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates)
            .ReturnsDbSet(new List<MarketplaceCommissionRate>());

        // Act
        var result = await _calculator.CalculateAsync(1, null, 100m, 50m);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateAsync_WithNoServiceFee_ShouldBeZero()
    {
        // Arrange
        var rates = new List<MarketplaceCommissionRate>
        {
            new()
            {
                Id = 1,
                MarketPlaceId = 1,
                CommissionPercent = 8m,
                IsDefault = true
            }
        };

        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates).ReturnsDbSet(rates);

        // Act
        var result = await _calculator.CalculateAsync(1, null, 150m, 80m);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.CommissionAmount.Should().Be(12m);    // 150 * 8%
        result.Data.ServiceFeeAmount.Should().Be(0m);
        result.Data.TransactionFee.Should().Be(0m);
        result.Data.TotalDeductions.Should().Be(12m);
        result.Data.NetRevenue.Should().Be(138m);
        result.Data.NetProfit.Should().Be(58m);           // 138 - 80
    }

    [Fact]
    public async Task CalculateAsync_WithZeroCostPrice_ShouldCalculateCorrectly()
    {
        // Arrange
        var rates = new List<MarketplaceCommissionRate>
        {
            new()
            {
                Id = 1,
                MarketPlaceId = 1,
                CommissionPercent = 10m,
                IsDefault = true
            }
        };

        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates).ReturnsDbSet(rates);

        // Act
        var result = await _calculator.CalculateAsync(1, null, 100m, 0m);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.NetProfit.Should().Be(90m);
        result.Data.ProfitMarginPercent.Should().Be(90m);
    }

    [Fact]
    public async Task GetCommissionRatesAsync_ShouldReturnRatesForMarketplace()
    {
        // Arrange
        var rates = new List<MarketplaceCommissionRate>
        {
            new()
            {
                Id = 1,
                MarketPlaceId = 1,
                CommissionPercent = 10m,
                IsDefault = true,
                MarketPlace = new MarketPlace { Id = 1, Name = "Trendyol" }
            },
            new()
            {
                Id = 2,
                MarketPlaceId = 2,
                CommissionPercent = 12m,
                IsDefault = true,
                MarketPlace = new MarketPlace { Id = 2, Name = "Hepsiburada" }
            }
        };

        mockIntegrationDbContext.Setup(x => x.MarketplaceCommissionRates).ReturnsDbSet(rates);

        // Act
        var result = await _calculator.GetCommissionRatesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].CommissionPercent.Should().Be(10m);
        result.Data[0].MarketPlaceName.Should().Be("Trendyol");
    }
}
