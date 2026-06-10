using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Product.Activity;

/// <summary>
/// Ürün 360° "Stok Hareketleri" sekmesi satırı — variant bazlı StockMovement projeksiyonu.
/// </summary>
public sealed record ProductStockMovementDto(
    Guid ProductVariantId,
    string? VariantLabel,
    StockMovementType Type,
    int Quantity,
    int StockBefore,
    int StockAfter,
    string? ReferenceType,
    string? ReferenceId,
    string? Note,
    DateTimeOffset CreatedAt);
