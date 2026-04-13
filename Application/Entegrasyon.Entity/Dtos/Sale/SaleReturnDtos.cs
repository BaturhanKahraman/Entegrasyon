using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record CreateSaleReturnDto(
    Guid SaleId,
    Guid ReturnedByUserId,
    string ReturnReason,
    int? RefundPaymentMethodId,
    string? Note,
    List<SaleReturnItemDto> Items);

public sealed record SaleReturnItemDto(
    Guid SaleItemId,
    int Quantity,
    string? Reason);

public record SaleReturnSummaryDto
{
    public long Id { get; init; }
    public DateTimeOffset ReturnDate { get; init; }
    public ReturnStatus ReturnStatus { get; init; }
    public string ReturnReason { get; init; } = "";
    public decimal RefundAmount { get; init; }
    public string ReturnedByName { get; init; } = "";
    public int ItemCount { get; init; }
}
