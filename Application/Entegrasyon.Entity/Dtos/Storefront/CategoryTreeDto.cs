namespace Entegrasyon.Entity.Dtos.Storefront;

public record CategoryTreeDto(
    int Id, string Name, string? SeoSlug, int ProductCount,
    List<CategoryTreeDto> Children);
