using Entegrasyon.Entity.Products;
using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Sales;

public class ReturnProduct:ApplicationEntity
{
    public long ProductId { get; set; }
    public ProductVariant Product { get; set; }
    
    
}