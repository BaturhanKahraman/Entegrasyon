namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceQuestionAskedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public long QuestionId { get; set; }
    public Guid ProductId { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    public MarketplaceQuestionAskedEvent() { }
    public MarketplaceQuestionAskedEvent(int marketPlaceId, long questionId, Guid productId, string questionText)
    {
        MarketPlaceId = marketPlaceId;
        QuestionId = questionId;
        ProductId = productId;
        QuestionText = questionText;
    }
}
