namespace Entegrasyon.Entity.Devices;

public sealed class DeviceInviteCode : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
    public int? RedeemedByDeviceId { get; set; }
    public Guid CreatedByUserId { get; set; }
}
