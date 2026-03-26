namespace Entegrasyon.Entity.Dtos.Storefront;

public record SellerProfileDto(
    string StoreName, string? StoreDescription, string? LogoUrl,
    string ContactPhone, string ContactEmail, string Address, string City,
    string? Iban);
