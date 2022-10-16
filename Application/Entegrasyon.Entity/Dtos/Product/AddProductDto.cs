namespace Entegrasyon.Entity.Dtos.Product;

public sealed class AddProductDto
{
   
    public string Title { get; set; }
    public string Description { get; set; }
    public string StockCode { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public decimal ListPrice { get; set; }
    public List<AddProductVariantDto> ProductVariants { get; set; }
}
/*
 *     public string Title { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
 */