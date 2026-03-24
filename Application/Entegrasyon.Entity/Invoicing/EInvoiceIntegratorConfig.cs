using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Invoicing;

/// <summary>
/// E-Fatura entegrator yapilandirmasi — tenant basina bir kayit.
/// </summary>
public sealed class EInvoiceIntegratorConfig : BaseEntity
{
    public int Id { get; set; }

    public IntegratorProvider IntegratorProvider { get; set; }

    [StringLength(500)]
    public string ApiUrl { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ApiKey { get; set; }

    [StringLength(200)]
    public string? ApiSecret { get; set; }

    [StringLength(100)]
    public string? Username { get; set; }

    [StringLength(200)]
    public string? Password { get; set; }

    public bool IsActive { get; set; }
}
