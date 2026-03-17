using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Products;

public sealed class ProductVariant:BaseEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public List<ProductVariantAttribute> ProductVariantAttributes { get; set; } = []; //color:red,size:xl etc...
    public string Barcode { get; set; } = null!;
    public decimal DimensionalWeight { get; set; }
    public string CurrencyType { get; set; } = "TRY";
    [Column(TypeName = "money")]
    public decimal ListPrice { get; set; }
    [Column(TypeName = "money")]
    public decimal SalePrice { get; set; }
    [Column(TypeName = "money")]
    public decimal CostPrice { get; set; }
    [Column(TypeName = "money")]
    public decimal ECommercePrice { get; set; }
    public decimal VatRate { get; set; }
    public ICollection<BranchOfficeStock> BranchOfficeStocks { get; set; } = [];
    public ICollection<Image> Images { get; set; } = [];
    
}