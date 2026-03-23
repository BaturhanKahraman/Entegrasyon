using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Invoices;

/// <summary>
/// Trendyol e-Faturam platformu uzerinden kesilen fatura kaydi.
/// Order ile 1-N iliskisi vardir (bir siparise birden fazla fatura kesilebilir — iptal + yeni).
/// </summary>
public sealed class EFaturaRecord : BaseEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Orders.Order Order { get; set; } = null!;

    /// <summary>Trendyol e-Faturam tarafindaki fatura UUID</summary>
    [StringLength(100)]
    public string? InvoiceUuid { get; set; }

    /// <summary>Fatura numarasi — ABC2025000000001 formatinda</summary>
    [StringLength(50)]
    public string? InvoiceId { get; set; }

    /// <summary>GIB zarf UUID (sadece e-fatura icin)</summary>
    [StringLength(100)]
    public string? EnvelopeId { get; set; }

    public EFaturaType InvoiceType { get; set; }
    public EFaturaStatus Status { get; set; }

    /// <summary>Trendyol API status kodu (10, 20, 30, 40, 205, 305, 405)</summary>
    public int? ApiStatusCode { get; set; }

    [StringLength(500)]
    public string? PdfDownloadUrl { get; set; }

    /// <summary>Bizim taraftaki referans ID (ERP-123 gibi)</summary>
    [StringLength(100)]
    public string? LocalReferenceId { get; set; }

    /// <summary>Odenecek tutar — kurus cinsinden (11455 = 114.55 TL)</summary>
    public long PayableAmountKurus { get; set; }

    /// <summary>KDV tutari — kurus cinsinden</summary>
    public long TaxAmountKurus { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTimeOffset? SentToGibAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Fatura linki Trendyol marketplace API'sine gonderildi mi?</summary>
    public bool InvoiceLinkSentToMarketplace { get; set; }
}

public enum EFaturaType
{
    EArchive = 0,
    EInvoice = 1
}

public enum EFaturaStatus
{
    Pending = 0,
    Processing = 10,
    Created = 30,
    Sent = 40,
    Approved = 205,
    Cancelled = 305,
    Error = 405
}
