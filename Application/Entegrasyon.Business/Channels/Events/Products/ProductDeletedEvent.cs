namespace Entegrasyon.Business.Channels.Events.Products;

public sealed class ProductDeletedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public Guid DeletedByUserId { get; set; }

    public ProductDeletedEvent() { }
    public ProductDeletedEvent(Guid productId, string productTitle, Guid deletedByUserId)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        DeletedByUserId = deletedByUserId;
    }
}
