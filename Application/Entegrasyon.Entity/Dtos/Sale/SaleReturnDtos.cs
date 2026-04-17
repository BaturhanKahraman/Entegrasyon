using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record CreateSaleReturnDto(
    Guid? SaleId,
    Guid? OrderId,
    Guid? ReturnedByUserId,
    ReturnSource Source,
    int? ReturnReasonId,
    string? CustomReason,
    int? RefundPaymentMethodId,
    string? Note,
    List<SaleReturnItemDto> Items,
    bool SubmitImmediately = true);

public sealed record UpdateSaleReturnDto(
    long Id,
    Guid UpdatedByUserId,
    ReturnSource Source,
    int? ReturnReasonId,
    string? CustomReason,
    int? RefundPaymentMethodId,
    string? Note,
    List<SaleReturnItemDto> Items);

public sealed record CompleteSaleReturnDto(
    long ReturnId,
    Guid CompletedByUserId,
    int BranchOfficeId,
    List<long> ItemIdsToRestore);

public sealed record CancelSaleReturnDto(
    long ReturnId,
    Guid CancelledByUserId,
    string Reason);

public sealed record SaleReturnItemDto(
    Guid? SaleItemId,
    long? OrderItemId,
    int Quantity,
    string? Reason);

public record SaleReturnSummaryDto
{
    public long Id { get; init; }
    public DateTimeOffset ReturnDate { get; init; }
    public ReturnStatus ReturnStatus { get; init; }
    public ReturnSource Source { get; init; }
    public string ReturnReason { get; init; } = "";
    public decimal RefundAmount { get; init; }
    public string ReturnedByName { get; init; } = "";
    public int ItemCount { get; init; }
}
