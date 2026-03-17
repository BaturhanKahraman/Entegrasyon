using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Entity.Products;

/// <summary>
/// mv_product_stock_summary materialized view'ının EF Core entity karşılığı.
/// Dashboard düşük stok hesaplamasında tüm ProductVariants + BranchOfficeStocks taraması yerine kullanılır.
/// </summary>
[Keyless]
public class ProductStockSummaryView
{
    public Guid ProductId { get; set; }
    public int TotalStock { get; set; }
}
