namespace Entegrasyon.Desktop.Data;

/// <summary>
/// Local offline sale record. Synced to server when online.
/// </summary>
public class OfflineSale
{
    public Guid Id { get; set; }
    public DateTimeOffset SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string PaymentMethod { get; set; } = "Nakit";
    public string? CustomerInfo { get; set; }
    public bool IsSynced { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<OfflineSaleItem> Items { get; set; } = [];
}
