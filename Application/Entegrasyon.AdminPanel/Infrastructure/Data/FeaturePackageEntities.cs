namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public class FeaturePackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FeaturePackagePermission> Permissions { get; set; } = [];
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
}

public class FeaturePackagePermission
{
    public int Id { get; set; }
    public int FeaturePackageId { get; set; }
    public FeaturePackage Package { get; set; } = null!;
    public string PermissionKey { get; set; } = string.Empty;
}

public class TenantSubscription : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int FeaturePackageId { get; set; }
    public FeaturePackage Package { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
