namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleSummaryDto(
    decimal TotalRevenue,
    int SaleCount,
    decimal AverageBasket,
    double ReturnRate
);
