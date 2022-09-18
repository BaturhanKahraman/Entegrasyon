using Entegrasyon.Entity.Categories;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Products;

public class ProductVariant : LongEntity
{
    [Required, StringLength(maximumLength: 255,MinimumLength = 3)]
    public string Title { get; set; }
    public long ProductMainId { get; set; }
    public MainProduct ProductMain { get; set; }
    public string? StockCode { get; set; }
    public decimal? DimensionalWeight { get; set; }
    public string? Description { get; set; }
    public string CurrencyType { get; set; } = "TRY";
    [Column(TypeName = "money")]
    public decimal ListPrice { get; set; }
    [Column(TypeName = "money")]
    public decimal SalePrice { get; set; }
    public decimal VatRate { get; set; }
    public int Quantity { get; set; }
    public int SoldQuantity { get; set; }
    public int CurrentStockQuantity { get; set; }
    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; }
    [Column(TypeName = "jsonb")]
    public AttributeKeyValue[] AttributeKeyValues { get; set; }//json

    public ICollection<Image> Images { get; set; }

}