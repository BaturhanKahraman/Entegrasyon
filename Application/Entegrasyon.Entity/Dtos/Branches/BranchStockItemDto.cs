namespace Entegrasyon.Entity.Dtos.Branches;

public record BranchStockItemDto(
    Guid ProductVariantId,
    string ProductName,
    string VariantDisplayName,
    string Barcode,
    int FirstTotalStock,
    int SoldQuantity,
    int CurrentStock);
