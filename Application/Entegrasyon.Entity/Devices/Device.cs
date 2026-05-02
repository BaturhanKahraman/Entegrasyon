namespace Entegrasyon.Entity.Devices;

public sealed class Device : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public bool IsRevoked { get; set; }
}
