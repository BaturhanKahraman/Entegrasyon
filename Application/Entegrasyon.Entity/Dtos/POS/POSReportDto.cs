using Entegrasyon.Entity.Dtos.Sale;

namespace Entegrasyon.Entity.Dtos.POS;

public record POSReportDto
{
    public long SessionId { get; init; }
    public string CashierName { get; init; } = "";
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public decimal OpeningCash { get; init; }
    public int TransactionCount { get; init; }
    public decimal TotalSales { get; init; }
    public decimal TotalReturns { get; init; }
    public decimal NetSales { get; init; }
    public List<PaymentMethodSummaryDto> PaymentBreakdown { get; init; } = [];
    public List<VatSummaryLineDto> VatBreakdown { get; init; } = [];
    public decimal ExpectedCash { get; init; }
    public decimal? ActualCash { get; init; }
    public decimal? CashDifference { get; init; }
}

public record PaymentMethodSummaryDto(
    string MethodName,
    int Count,
    decimal Total);
