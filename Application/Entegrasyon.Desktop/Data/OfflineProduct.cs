namespace Entegrasyon.Desktop.Data;

/// <summary>
/// Local SQLite product cache for offline sales.
/// Does NOT inherit BaseEntity — standalone offline entity.
/// </summary>
public class OfflineProduct
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? Size { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public decimal VatRate { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }
}
