namespace Entegrasyon.Entity.Products;

public sealed class BranchOfficeStock
{
    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; }

    public Guid? ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }

    public int CurrentStock { get; set; }
    public int SoldQuantity { get; set; }
    public int FirstTotalStock { get; set; }
}