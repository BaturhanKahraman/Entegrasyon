namespace Entegrasyon.Business.Channels.Events.Brands;

public sealed class BrandDeletedEvent : BaseEvent
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DeletedByUserId { get; set; }

    public BrandDeletedEvent() { }
    public BrandDeletedEvent(int brandId, string name, Guid deletedByUserId)
    {
        BrandId = brandId;
        Name = name;
        DeletedByUserId = deletedByUserId;
    }
}
