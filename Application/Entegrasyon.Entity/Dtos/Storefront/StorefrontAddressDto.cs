namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontAddressDto(
    int Id,
    string Label,
    string FullName,
    string? Phone,
    string? City,
    string? District,
    string? Neighborhood,
    string? PostalCode,
    string AddressLine,
    bool IsDefault);
