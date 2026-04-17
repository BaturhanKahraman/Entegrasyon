namespace Entegrasyon.Entity.Dtos.Brand;

public record BrandDetailDto(int Id, DateTimeOffset CreatedAt, string Name, int ProductNumber, string? SeoSlug, string? NormalizedName);