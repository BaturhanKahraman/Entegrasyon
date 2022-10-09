using NpgsqlTypes;
using Shared.Entity;

namespace Entegrasyon.Entity;

public class CargoCompany:ApplicationEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string TaxNumber { get; set; }

    public NpgsqlTsVector SearchVector { get; set; }
}