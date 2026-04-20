namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontProductQuestionEvent : BaseEvent
{
    public long QuestionId { get; set; }
    public Guid ProductId { get; set; }
    public int CustomerId { get; set; }

    public StorefrontProductQuestionEvent() { }
    public StorefrontProductQuestionEvent(long questionId, Guid productId, int customerId)
    {
        QuestionId = questionId;
        ProductId = productId;
        CustomerId = customerId;
    }
}
