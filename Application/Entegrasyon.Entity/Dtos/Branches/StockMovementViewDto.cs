using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Branches;

public record StockMovementViewDto(
    long Id,
    DateTimeOffset MovedAt,
    string ProductName,
    string VariantDisplayName,
    string Barcode,
    StockMovementType MovementType,
    int Quantity,
    int StockBefore,
    int StockAfter,
    string? ReferenceType,
    string? ReferenceId);
