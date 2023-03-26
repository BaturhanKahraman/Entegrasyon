namespace Entegrasyon.Entity.Tenants;

public class MainCustomer
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public Tenant Tenant { get; set; }
    public string Info { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ValidUntil { get; set; }

}