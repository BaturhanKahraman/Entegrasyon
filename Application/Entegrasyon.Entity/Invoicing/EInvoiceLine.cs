using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Invoicing;

/// <summary>
/// E-Fatura kalemi — bir fatura birden fazla satir icerebilir.
/// </summary>
public sealed class EInvoiceLine : BaseEntity
{
    public Guid Id { get; set; }

    public Guid EInvoiceId { get; set; }
    public EInvoice EInvoice { get; set; } = null!;

    [StringLength(300)]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>KDV orani (ornegin 20 = %20)</summary>
    public int TaxRate { get; set; }

    /// <summary>Bu satirin KDV tutari</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Bu satirin toplam tutari (KDV dahil)</summary>
    public decimal LineTotal { get; set; }
}
