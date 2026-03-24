namespace Entegrasyon.Desktop.Data;

/// <summary>
/// Individual line item in an offline sale.
/// </summary>
public class OfflineSaleItem
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int DiscountPercent { get; set; }
    public decimal VatRate { get; set; }

    public OfflineSale Sale { get; set; } = null!;
}
