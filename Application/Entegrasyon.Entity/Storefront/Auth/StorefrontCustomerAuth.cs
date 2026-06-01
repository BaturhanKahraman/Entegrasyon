using Entegrasyon.Entity.Customers;

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontCustomerAuth : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Email { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastPasswordChangedAt { get; set; }
    public bool LoginAlertsEnabled { get; set; } = true;
    public int LoginFailedCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public bool MarketingConsent { get; set; }
    public DateTimeOffset? MarketingConsentDate { get; set; }
    public DateTimeOffset KvkkConsentDate { get; set; }
    public string? ExternalLoginProvider { get; set; }
    public string? ExternalLoginId { get; set; }

    // Two-Factor Authentication
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public ICollection<StorefrontTwoFactorRecoveryCode> RecoveryCodes { get; set; } = new List<StorefrontTwoFactorRecoveryCode>();
}
