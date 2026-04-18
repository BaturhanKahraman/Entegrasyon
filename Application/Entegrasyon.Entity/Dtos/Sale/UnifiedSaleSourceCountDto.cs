using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleSourceCountDto(
    UnifiedSaleSource? Source,
    string Label,
    int Count
);
