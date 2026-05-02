using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public sealed record OfflineSaleItemDto(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitPrice,
    double DiscountPercent);

public sealed record OfflineSaleDto(
    string IdempotencyKey,
    DateTimeOffset OccurredAt,
    int BranchOfficeId,
    Guid SalePersonId,
    int? CustomerId,
    decimal GeneralDiscount,
    string PaymentMethod,
    List<OfflineSaleItemDto> Items);

public sealed record OfflineSaleSyncResult(
    string IdempotencyKey,
    string Status,           // "ok" | "oversold" | "duplicate"
    Guid? SaleId,
    int? IncidentId,
    string? Message);

public interface IOfflineSaleSyncManager
{
    Task<IDataResult<OfflineSaleSyncResult>> SyncOneAsync(int tenantId, OfflineSaleDto dto);
}
