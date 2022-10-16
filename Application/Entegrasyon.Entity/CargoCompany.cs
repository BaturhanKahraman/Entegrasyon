using NpgsqlTypes;
using Shared.Entity;

namespace Entegrasyon.Entity;

public sealed class CargoCompany : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string TaxNumber { get; set; }

    public NpgsqlTsVector SearchVector { get; set; }
}