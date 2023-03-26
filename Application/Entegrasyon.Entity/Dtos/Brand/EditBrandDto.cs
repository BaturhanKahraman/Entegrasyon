namespace Entegrasyon.Entity.Dtos.Brand;

public sealed record EditBrandDto(
    int Id,
    uint RowVersion,
    string Name
    );