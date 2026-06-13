namespace Entegrasyon.Entity.Dtos.Brand;

public sealed record EditBrandDto(
    int Id,
    string Name,
    string? SeoSlug
    );
