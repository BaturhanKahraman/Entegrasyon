namespace Entegrasyon.Entity;

/// <summary>
/// Stok transfer talebinin bir satırı: hangi ürün varyantından ne kadar aktarılacak.
/// </summary>
public sealed class StockTransferRequestItem
{
    public int Id { get; set; }

    public int TransferRequestId { get; set; }
    public StockTransferRequest TransferRequest { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}
