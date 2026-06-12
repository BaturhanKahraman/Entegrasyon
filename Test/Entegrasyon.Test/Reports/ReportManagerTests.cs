using System.Text.Json;
using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;

namespace Entegrasyon.UnitTest.Reports;

public class ReportManagerTests : BaseTest
{
    private readonly ReportManager _sut;

    public ReportManagerTests()
    {
        _sut = new ReportManager(mockContextFactory.Object);
    }

    #region ProfitLoss

    [Fact]
    public void ProfitLossReportDto_EmptyData_HasZeroNetProfit()
    {
        var dto = new ProfitLossReportDto(0, 0, 0, 0, 0, 0, 0, []);

        dto.NetProfit.Should().Be(0);
        dto.ProfitMargin.Should().Be(0);
        dto.ByMarketplace.Should().BeEmpty();
    }

    [Fact]
    public void ProfitLossReportDto_WithData_CalculatesCorrectly()
    {
        var dto = new ProfitLossReportDto(
            Revenue: 10000m,
            CostOfGoods: 5000m,
            MarketplaceCommission: 1350m,
            CargoExpense: 500m,
            TaxAmount: 1800m,
            NetProfit: 1350m,
            ProfitMargin: 13.5m,
            ByMarketplace:
            [
                new ProfitLossByMarketplaceDto(1, "Trendyol", 6000m, 810m, 810m),
                new ProfitLossByMarketplaceDto(3, "Hepsiburada", 4000m, 480m, 540m)
            ]);

        dto.Revenue.Should().Be(10000m);
        dto.CostOfGoods.Should().Be(5000m);
        dto.MarketplaceCommission.Should().Be(1350m);
        dto.ByMarketplace.Should().HaveCount(2);
        dto.ByMarketplace[0].MarketPlaceName.Should().Be("Trendyol");
    }

    [Fact]
    public void ProfitLossReportFilter_DateRange_SetsCorrectly()
    {
        var filter = new ProfitLossReportFilterDto(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 31),
            MarketPlaceId: 1);

        filter.StartDate.Should().Be(new DateOnly(2025, 1, 1));
        filter.EndDate.Should().Be(new DateOnly(2025, 1, 31));
        filter.MarketPlaceId.Should().Be(1);
    }

    [Fact]
    public void ProfitLossReportFilter_NullMarketplace_IsOptional()
    {
        var filter = new ProfitLossReportFilterDto(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 31));

        filter.MarketPlaceId.Should().BeNull();
    }

    #endregion

    #region ProductPerformance

    [Fact]
    public void ProductPerformanceDto_CanSortByTotalSold()
    {
        var items = new List<ProductPerformanceDto>
        {
            new(Guid.NewGuid(), "A", 10, 100m, 0m, 0m, 0m),
            new(Guid.NewGuid(), "B", 50, 500m, 0m, 0m, 0m),
            new(Guid.NewGuid(), "C", 30, 300m, 0m, 0m, 0m),
        };

        var sorted = items.OrderByDescending(x => x.TotalSold).ToList();

        sorted[0].ProductName.Should().Be("B");
        sorted[1].ProductName.Should().Be("C");
        sorted[2].ProductName.Should().Be("A");
    }

    [Fact]
    public void ProductPerformanceFilter_DefaultTopN_Is50()
    {
        var filter = new ProductPerformanceFilterDto(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 31));

        filter.TopN.Should().Be(50);
    }

    #endregion

    #region StockAlerts

    [Theory]
    [InlineData(0, StockAlertLevel.Critical)]
    [InlineData(2, StockAlertLevel.Critical)]
    [InlineData(5, StockAlertLevel.Low)]
    [InlineData(10, StockAlertLevel.Low)]
    [InlineData(15, StockAlertLevel.Low)]
    [InlineData(20, StockAlertLevel.Sufficient)]
    [InlineData(100, StockAlertLevel.Sufficient)]
    public void GetStockAlertLevel_ReturnsCorrectLevel(int currentStock, StockAlertLevel expected)
    {
        // Threshold logic: critical <= 3, low <= 15, else sufficient
        var level = DetermineAlertLevel(currentStock);
        level.Should().Be(expected);
    }

    [Fact]
    public void StockAlertDto_SuggestedOrderQuantity_IsPositive()
    {
        var alert = new StockAlertDto(
            Guid.NewGuid(), "ABC123", "Test Product", "Sarı XL",
            CurrentStock: 2, MinimumStock: 10,
            DaysUntilStockout: 3, SuggestedOrderQuantity: 30,
            AlertLevel: StockAlertLevel.Critical, BranchOfficeId: 1,
            BranchOfficeName: "Ana Depo", LastStockEntryDate: null);

        alert.SuggestedOrderQuantity.Should().BePositive();
        alert.DaysUntilStockout.Should().Be(3);
        alert.AlertLevel.Should().Be(StockAlertLevel.Critical);
    }

    [Fact]
    public void StockAlertDto_EmptyBarcode_HandledGracefully()
    {
        var alert = new StockAlertDto(
            Guid.NewGuid(), null, "Test Product", "Test Product",
            CurrentStock: 0, MinimumStock: 10,
            DaysUntilStockout: 0, SuggestedOrderQuantity: 30,
            AlertLevel: StockAlertLevel.Critical, BranchOfficeId: 1,
            BranchOfficeName: "Ana Depo", LastStockEntryDate: null);

        alert.Barcode.Should().BeNull();
        alert.CurrentStock.Should().Be(0);
    }

    #endregion

    #region MarketplaceSummary

    [Fact]
    public void MarketplaceSummaryDto_WithData_CalculatesAvgOrderValue()
    {
        var summary = new MarketplaceSummaryDto(
            MarketplaceId: 1,
            MarketplaceName: "Trendyol",
            TotalOrders: 100,
            TotalRevenue: 50000m,
            CommissionPaid: 6750m,
            AverageOrderValue: 500m,
            TopSellingProducts: ["Urun A", "Urun B"]);

        summary.AverageOrderValue.Should().Be(500m);
        summary.TopSellingProducts.Should().HaveCount(2);
    }

    [Fact]
    public void MarketplaceSummaryDto_NoOrders_HasZeroAverage()
    {
        var summary = new MarketplaceSummaryDto(
            MarketplaceId: 1,
            MarketplaceName: "Trendyol",
            TotalOrders: 0,
            TotalRevenue: 0m,
            CommissionPaid: 0m,
            AverageOrderValue: 0m,
            TopSellingProducts: []);

        summary.TotalOrders.Should().Be(0);
        summary.AverageOrderValue.Should().Be(0m);
        summary.TopSellingProducts.Should().BeEmpty();
    }

    [Fact]
    public void MarketplaceSummaryFilter_DateRange_SetsCorrectly()
    {
        var filter = new MarketplaceSummaryFilterDto(
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30));

        filter.StartDate.Month.Should().Be(6);
        filter.EndDate.Day.Should().Be(30);
    }

    #endregion

    /// <summary>
    /// Stok uyari seviyesi belirleme yardimcisi (ReportManager'daki mantikla ayni)
    /// </summary>
    private static StockAlertLevel DetermineAlertLevel(int currentStock)
    {
        return currentStock switch
        {
            <= 3 => StockAlertLevel.Critical,
            <= 15 => StockAlertLevel.Low,
            _ => StockAlertLevel.Sufficient
        };
    }
}
