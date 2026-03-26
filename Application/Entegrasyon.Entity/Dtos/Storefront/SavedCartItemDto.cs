namespace Entegrasyon.Entity.Dtos.Storefront;

public record SavedCartItemDto(
    Guid ProductVariantId, string ProductTitle, string? ImageUrl,
    decimal OriginalPrice, decimal CurrentPrice, DateTimeOffset SavedAt);
