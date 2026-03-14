namespace Entegrasyon.Business.Channels.Events.Products;

/// <summary>
/// Bir ürün varyantının stok veya fiyat bilgisi değiştiğinde yayınlanır.
/// BackgroundService bu event'i tüketip Trendyol'a push eder.
/// </summary>
public class StockPriceChangedEvent : BaseEvent
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }

    public StockPriceChangedEvent() { }
    public StockPriceChangedEvent(Guid productVariantId, Guid productId)
    {
        ProductVariantId = productVariantId;
        ProductId = productId;
    }
}
