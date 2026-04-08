namespace Entegrasyon.Entity.ApiKeys;

public sealed class ApiKey : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string Scopes { get; set; } = "[]";
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastUsedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
