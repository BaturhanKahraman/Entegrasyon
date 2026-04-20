namespace Entegrasyon.Business.Channels.Events.Brands;

public sealed class BrandAddedEvent : BaseEvent
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }

    public BrandAddedEvent() { }
    public BrandAddedEvent(int brandId, string name, Guid createdByUserId)
    {
        BrandId = brandId;
        Name = name;
        CreatedByUserId = createdByUserId;
    }
}
