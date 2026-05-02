using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Stock;

public enum NegativeStockResolution
{
    StockAdded = 1,
    OnlineSaleCancelled = 2
}

public sealed class NegativeStockIncident : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int BranchOfficeId { get; set; }

    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    /// <summary>Pozitif değer — negatife giden miktar (ör. -3 stoğa düşen olay için 3)</summary>
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }

    /// <summary>"OfflinePos", "Trendyol", "Hepsiburada", "Storefront" vb.</summary>
    public string TriggeringSource { get; set; } = string.Empty;

    /// <summary>Tetikleyen satış/sipariş referansı: marketplace orderId, sale uuid, vb.</summary>
    public string? TriggeringReferenceId { get; set; }

    /// <summary>Eğer tetikleyen kayıt iç Sale ise FK</summary>
    public Guid? TriggeringSaleId { get; set; }

    public DateTimeOffset DetectedAt { get; set; }

    public NegativeStockResolution? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? Notes { get; set; }
}
