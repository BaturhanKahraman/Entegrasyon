using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales;

public sealed class ReturnProduct: BaseEntity
{
    public int Id { get; set; }
    public long ProductId { get; set; }
    public ProductVariant Product { get; set; }
    
    
}