namespace Entegrasyon.Business.Channels.Events.Products;

public class ProductAddedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;

    public ProductAddedEvent() { }
    public ProductAddedEvent(Guid productId, string productTitle)
    {
        ProductId = productId;
        ProductTitle = productTitle;
    }
}
