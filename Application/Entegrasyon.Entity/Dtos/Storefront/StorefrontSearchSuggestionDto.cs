namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontSearchSuggestionDto(
    string Text, string Url, string Type, string? ImageUrl);
