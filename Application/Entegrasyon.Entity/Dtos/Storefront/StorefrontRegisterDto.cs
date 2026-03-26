namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontRegisterDto(
    int TenantId,
    string Name,
    string Surname,
    string Email,
    string? Phone,
    string Password,
    string ConfirmPassword,
    bool KvkkConsent,
    bool MarketingConsent);
