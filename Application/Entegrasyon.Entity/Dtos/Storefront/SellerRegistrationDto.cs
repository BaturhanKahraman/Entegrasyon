namespace Entegrasyon.Entity.Dtos.Storefront;

public record SellerRegistrationDto(
    string StoreName, string? StoreDescription,
    string CompanyName, string TaxNumber, string TaxOffice,
    string? Iban, string ContactPhone, string ContactEmail,
    string Address, string City);
