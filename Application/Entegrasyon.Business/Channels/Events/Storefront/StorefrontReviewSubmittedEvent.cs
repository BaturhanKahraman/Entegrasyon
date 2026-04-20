namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontReviewSubmittedEvent : BaseEvent
{
    public long ReviewId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }

    public StorefrontReviewSubmittedEvent() { }
    public StorefrontReviewSubmittedEvent(long reviewId, Guid productId, int rating)
    {
        ReviewId = reviewId;
        ProductId = productId;
        Rating = rating;
    }
}
