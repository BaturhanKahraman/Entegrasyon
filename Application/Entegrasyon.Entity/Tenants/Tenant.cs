using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Tenants;

public class Tenant
{
    [Key]
    public int Id { get; set; }
    public string ConnectionString { get; set; } = null!;

    public int? MainCustomerId { get; set; }
    public MainCustomer? MainCustomer { get; set; }

    public ConnectionInfo ConnectionInfo { get; set; } = null!;
}