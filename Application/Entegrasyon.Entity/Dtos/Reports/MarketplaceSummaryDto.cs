namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record MarketplaceSummaryFilterDto(
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record MarketplaceSummaryDto(
    int MarketplaceId,
    string MarketplaceName,
    int TotalOrders,
    decimal TotalRevenue,
    decimal CommissionPaid,
    decimal AverageOrderValue,
    List<string> TopSellingProducts);
