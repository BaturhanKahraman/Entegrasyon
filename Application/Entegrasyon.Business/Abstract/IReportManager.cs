using Entegrasyon.Entity.Dtos.Reports;

namespace Entegrasyon.Business.Abstract;

public interface IReportManager
{
    Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter);
    Task<InventoryReportDto> GetInventoryReportAsync(InventoryReportFilterDto filter);
    Task<MarketplaceReportDto> GetMarketplaceReportAsync();
}
