using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleFilterDto(
    UnifiedSaleSource? Source,
    UnifiedSaleStatus? Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? SearchText,
    int? CustomerId,
    int PageIndex = 0,
    int PageSize = 25
);
