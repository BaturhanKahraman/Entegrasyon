namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontProductCardDto(
    Guid Id, string Title, string? SeoSlug, string? ImageUrl,
    decimal MinPrice, decimal MaxPrice, decimal? OldPrice,
    int TotalStock, string? BrandName, string CategoryName, bool IsNew);
