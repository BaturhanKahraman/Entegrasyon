namespace Entegrasyon.Entity.Dtos.POS;

public sealed record POSSummaryDto(
    long SessionId,
    decimal OpeningCash,
    decimal TotalSales,
    decimal TotalCash,
    decimal TotalCard,
    int TransactionCount,
    decimal ExpectedCash,
    decimal? ClosingCash,
    decimal? Difference);
