namespace Entegrasyon.Entity.Dtos.Trendyol;

/// <summary>
/// POST /integration/inventory/sellers/{sellerId}/products/price-and-inventory
/// Trendyol unlimited rate endpoint — stok ve fiyat güncellemesi.
/// </summary>
public sealed record TrendyolPriceAndInventoryRequest(List<TrendyolPriceInventoryItem> Items);

public sealed record TrendyolPriceInventoryItem(
    string Barcode,
    int Quantity,
    decimal SalePrice,
    decimal ListPrice);
