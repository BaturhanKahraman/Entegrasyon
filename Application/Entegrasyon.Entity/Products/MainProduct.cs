using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Categories;
using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public class MainProduct : LongEntity
{
    [Required]
    [MinLength(3)]
    public string? Header { get; set; }
    public string? Barcode { get; set; }
    //public string StockCode { get; set; }
    public int? BrandId { get; set; }
    public Brand Brand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public int TotalQuantity => ProductVariants.Sum(x => x.Quantity);
    public int TotalSoldQuantity => ProductVariants.Sum(x => x.SoldQuantity);
    public int TotalCurrentStock => TotalQuantity - TotalSoldQuantity;



    public List<ProductVariant> ProductVariants { get; set; }

}