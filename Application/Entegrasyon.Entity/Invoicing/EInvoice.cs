using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Invoicing;

/// <summary>
/// Genel amacli e-Fatura / e-Arsiv kaydi.
/// Trendyol'a ozgu EFaturaRecord ile karistirilmamalidir.
/// </summary>
public sealed class EInvoice : BaseEntity
{
    public Guid Id { get; set; }

    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public EInvoiceType InvoiceType { get; set; }
    public EInvoiceStatus Status { get; set; }

    [StringLength(20)]
    public string CustomerTaxId { get; set; } = string.Empty;

    [StringLength(300)]
    public string CustomerTitle { get; set; } = string.Empty;

    public DateTimeOffset IssueDate { get; set; }

    /// <summary>Ara toplam (KDV haric)</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Toplam KDV tutari</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Genel toplam (KDV dahil)</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>GIB tarafindan atanan UUID</summary>
    [StringLength(100)]
    public string? GibUuid { get; set; }

    /// <summary>UBL-TR XML icerigi</summary>
    public string? XmlContent { get; set; }

    [StringLength(500)]
    public string? PdfUrl { get; set; }

    /// <summary>Yerel satis kaydina FK (opsiyonel)</summary>
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    /// <summary>Pazaryeri siparis kaydina FK (opsiyonel)</summary>
    public Guid? MarketplaceOrderId { get; set; }

    public IntegratorProvider IntegratorProvider { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<EInvoiceLine> Lines { get; set; } = new List<EInvoiceLine>();
}

public enum EInvoiceType
{
    EFatura = 0,
    EArsiv = 1
}

public enum EInvoiceStatus
{
    Draft = 0,
    Sent = 10,
    Accepted = 20,
    Rejected = 30,
    Cancelled = 40
}

public enum IntegratorProvider
{
    Custom = 0,
    Parasut = 1,
    ForIba = 2,
    Logo = 3
}
