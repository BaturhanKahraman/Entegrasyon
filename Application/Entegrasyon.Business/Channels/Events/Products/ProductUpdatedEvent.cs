namespace Entegrasyon.Business.Channels.Events.Products;

public class ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public bool CategoryChanged { get; set; }

    public ProductUpdatedEvent() { }

    public ProductUpdatedEvent(Guid productId, string productTitle, bool categoryChanged)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        CategoryChanged = categoryChanged;
    }
}
