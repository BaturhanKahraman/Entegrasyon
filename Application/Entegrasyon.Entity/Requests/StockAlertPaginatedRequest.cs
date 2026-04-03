namespace Entegrasyon.Entity;

public record StockAlertPaginatedRequest() : PaginatedRequest()
{
    public int MinimumStockThreshold { get; init; } = 10;
}
