using NpgsqlTypes;

namespace Entegrasyon.Entity;

public sealed class CargoCompany : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? TaxNumber { get; set; }
    public string? ApiKey { get; set; }
    public string? SecretKey { get; set; }
    public string? CustomerCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIntegrated { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;
}