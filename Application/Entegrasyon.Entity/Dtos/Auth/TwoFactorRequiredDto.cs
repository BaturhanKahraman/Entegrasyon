namespace Entegrasyon.Entity.Dtos.Auth;

/// <summary>
/// Returned from LoginAsync when the user has 2FA active and needs to complete a second step.
/// </summary>
public sealed record TwoFactorRequiredDto(Guid UserId);
