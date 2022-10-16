using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Categories;
using NpgsqlTypes;
using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public sealed class MainProduct : BaseEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? StockCode { get; set; }
    //public string StockCode { get; set; }
    public int? BrandId { get; set; }
    public Brand Brand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public int TotalQuantity => ProductVariants.Sum(x => x.TotalQuantity);
    public int TotalSoldQuantity => ProductVariants.Sum(x => x.TotalSold);
    public int TotalCurrentStock => TotalQuantity - TotalSoldQuantity;
    public ICollection<ProductVariant> ProductVariants { get; set; }

    public NpgsqlTsVector SearchVector { get; set; }


}