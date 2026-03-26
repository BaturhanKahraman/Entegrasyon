using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Storefront;

public sealed class SellerProduct : BaseEntity
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public SellerProductStatus Status { get; set; }
}

public enum SellerProductStatus { Pending, Approved, Rejected }
