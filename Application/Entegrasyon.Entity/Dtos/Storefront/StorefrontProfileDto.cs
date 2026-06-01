namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontProfileDto(
    string Name,
    string Surname,
    string? Phone,
    DateOnly? BirthDate = null,
    string? Gender = null,
    bool NewsletterOptIn = false,
    string? AvatarUrl = null);
