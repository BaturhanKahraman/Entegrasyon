using Entegrasyon.Entity.Categories;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Products;

public sealed class ProductVariant :BaseEntity
{
    public Guid Id { get; set; }
    public Guid ProductMainId { get; set; }
    public MainProduct ProductMain { get; set; }
    public string? Barcode { get; set; }
    public decimal? DimensionalWeight { get; set; }
    public string CurrencyType { get; set; } = "TRY";
    [Column(TypeName = "money")]
    public decimal ListPrice { get; set; }
    [Column(TypeName = "money")]
    public decimal SalePrice { get; set; }
    public decimal VatRate { get; set; }
    public int TotalCurrentQuantity => BranchOfficeStocks.Sum(x => x.CurrentStock);
    public int TotalSold => BranchOfficeStocks.Sum(x => x.SoldQuantity);
    public int TotalQuantity => BranchOfficeStocks.Sum(x => x.FirstTotalStock);
    public ICollection<BranchOfficeStock> BranchOfficeStocks { get; set; }
    
    [Column(TypeName = "jsonb")]
    public AttributeKeyValue[] AttributeKeyValues { get; set; }//json

    public ICollection<Image> Images { get; set; }

}