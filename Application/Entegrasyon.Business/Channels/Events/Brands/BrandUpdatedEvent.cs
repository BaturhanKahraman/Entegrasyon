namespace Entegrasyon.Business.Channels.Events.Brands;

public sealed class BrandUpdatedEvent : BaseEvent
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] ChangedFields { get; set; } = [];
    public Guid UpdatedByUserId { get; set; }

    public BrandUpdatedEvent() { }
    public BrandUpdatedEvent(int brandId, string name, string[] changedFields, Guid updatedByUserId)
    {
        BrandId = brandId;
        Name = name;
        ChangedFields = changedFields;
        UpdatedByUserId = updatedByUserId;
    }
}
