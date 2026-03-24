namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record ProductPerformanceFilterDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int? TopN = 50);

public sealed record ProductPerformanceDto(
    Guid ProductId,
    string ProductName,
    int TotalSold,
    decimal TotalRevenue,
    decimal ReturnRate,
    decimal AverageRating,
    decimal StockTurnoverRate);
