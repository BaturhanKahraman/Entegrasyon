namespace Entegrasyon.Entity.Products;

public sealed class StockMovement : BaseEntity
{
    public long Id { get; set; }

    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public StockMovementType Type { get; set; }

    /// <summary>Negatif = azalış, pozitif = artış</summary>
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }

    /// <summary>"Sale", "TrendyolOrder", "ManualAdjustment" vb.</summary>
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Note { get; set; }
}

public enum StockMovementType
{
    InitialStock = 1,
    Sale = 2,
    MarketplaceSale = 3,
    Return = 4,
    ManualAdjustment = 5,
    Transfer = 6
}
