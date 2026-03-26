namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontTwoFactorRecoveryCode : BaseEntity
{
    public int Id { get; set; }
    public int AuthId { get; set; }
    public StorefrontCustomerAuth Auth { get; set; } = null!;
    public string Code { get; set; } = null!; // hashed
    public bool IsUsed { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
