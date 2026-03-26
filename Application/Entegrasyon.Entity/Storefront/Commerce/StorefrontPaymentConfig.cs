using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontPaymentConfig : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [StringLength(50)]
    public string PaymentProvider { get; set; } = "Iyzico"; // Iyzico, PayTR

    [StringLength(200)]
    public string? ApiKey { get; set; }

    [StringLength(200)]
    public string? SecretKey { get; set; }

    public bool IsLive { get; set; } // false = sandbox

    public bool InstallmentEnabled { get; set; }
    public int MaxInstallmentCount { get; set; } = 12;
    public decimal? MinInstallmentAmount { get; set; }

    public bool IsActive { get; set; }

    [StringLength(500)]
    public string? BaseUrl { get; set; } // sandbox or production URL
}
