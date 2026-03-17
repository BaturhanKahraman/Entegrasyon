using NpgsqlTypes;

namespace Entegrasyon.Entity;

public sealed class CargoCompany : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? TaxNumber { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;
}