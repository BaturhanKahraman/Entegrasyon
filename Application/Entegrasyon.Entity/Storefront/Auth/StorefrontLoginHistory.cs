namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontLoginHistory : BaseEntity
{
    public int Id { get; set; }
    public int AuthId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet
    public DateTimeOffset LoginAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}
