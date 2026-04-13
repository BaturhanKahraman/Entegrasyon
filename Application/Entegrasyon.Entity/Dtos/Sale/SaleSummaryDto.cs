namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleSummaryDto(
    decimal TotalSales,
    int SaleCount,
    decimal AverageBasket,
    decimal TotalReturns);
