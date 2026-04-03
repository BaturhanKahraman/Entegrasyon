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
    Task<Pageable<StockAlertDto>> GetStockAlertsAsync(StockAlertPaginatedRequest request);
    Task<List<MarketplaceSummaryDto>> GetMarketplaceSummaryAsync(MarketplaceSummaryFilterDto filter);
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly startDate, DateOnly endDate, int top = 10);
    Task<List<ProductPerformanceDto>> GetSlowMovingProductsAsync(DateOnly startDate, DateOnly endDate, int top = 10);
}
