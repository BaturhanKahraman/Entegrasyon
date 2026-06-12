using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;

namespace Entegrasyon.Business.Abstract;

public interface IReportManager
{
    Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter);
    Task<InventoryReportDto> GetInventoryReportAsync(InventoryReportFilterDto filter);
    Task<MarketplaceReportDto> GetMarketplaceReportAsync();
    Task<ProfitLossReportDto> GetProfitLossReportAsync(ProfitLossReportFilterDto filter);
    Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(ProductPerformanceFilterDto filter);
    Task<StockAlertReportDto> GetStockAlertReportAsync(StockAlertPaginatedRequest request);
    Task<List<MarketplaceSummaryDto>> GetMarketplaceSummaryAsync(MarketplaceSummaryFilterDto filter);
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly startDate, DateOnly endDate, int top = 10);
    Task<List<ProductPerformanceDto>> GetSlowMovingProductsAsync(DateOnly startDate, DateOnly endDate, int top = 10);
    Task<VatDeclarationDto> GetVatDeclarationAsync(DateOnly startDate, DateOnly endDate);
    Task<InvoiceTypeBreakdownDto> GetInvoiceTypeBreakdownAsync(DateOnly startDate, DateOnly endDate);
    Task<ReturnReasonTrendDto> GetReturnReasonTrendAsync(DateOnly startDate, DateOnly endDate);
    Task<List<ProductReturnRateDto>> GetProductReturnRatesAsync(
        DateOnly startDate, DateOnly endDate, int minSold = 5, double alertThresholdPercent = 10, int top = 20);
    Task<ReturnCostDto> GetReturnCostAsync(
        DateOnly startDate, DateOnly endDate, decimal shippingPerReturn = 50m, decimal processPerReturn = 25m);

    Task<List<ChannelSalesDto>> GetCategoryChannelSalesAsync(DateOnly startDate, DateOnly endDate);
    Task<List<CategorySeasonalDto>> GetCategorySeasonalComparisonAsync(DateOnly startDate, DateOnly endDate);
    Task<List<SlowMovingCategoryDto>> GetSlowMovingCategoriesAsync(int staleDays = 30);
    Task<List<PriceRangeBucketDto>> GetCategoryPriceDistributionAsync(DateOnly startDate, DateOnly endDate);

    Task<List<CargoCompanyPerformanceDto>> GetCargoCompanyPerformanceAsync(DateOnly startDate, DateOnly endDate);
    Task<List<RegionDensityDto>> GetRegionDensityAsync(DateOnly startDate, DateOnly endDate, int top = 20);
    Task<List<DelayTrendPointDto>> GetDelayTrendAsync(DateOnly startDate, DateOnly endDate);
    Task<List<DelayedShipmentDto>> GetDelayedShipmentsAsync(DateOnly startDate, DateOnly endDate);
}
