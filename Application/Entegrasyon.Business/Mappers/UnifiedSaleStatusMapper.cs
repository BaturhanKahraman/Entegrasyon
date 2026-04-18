using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Mappers;

/// <summary>
/// Sale/Order ham durum kodlarını 6'lı <see cref="UnifiedSaleStatus"/>'a indirger.
/// </summary>
public static class UnifiedSaleStatusMapper
{
    public static UnifiedSaleStatus Map(int rawCode, int entityType)
    {
        if (entityType == 0)
        {
            return (SaleStatus)rawCode switch
            {
                SaleStatus.Completed     => UnifiedSaleStatus.Completed,
                SaleStatus.PartialReturn => UnifiedSaleStatus.PartialReturn,
                SaleStatus.FullReturn    => UnifiedSaleStatus.FullReturn,
                SaleStatus.Cancelled     => UnifiedSaleStatus.Cancelled,
                _                        => UnifiedSaleStatus.Completed
            };
        }

        return rawCode switch
        {
            1 or 2 => UnifiedSaleStatus.Pending,
            3      => UnifiedSaleStatus.Shipping,
            4      => UnifiedSaleStatus.Completed,
            5      => UnifiedSaleStatus.Cancelled,
            _      => UnifiedSaleStatus.Pending
        };
    }
}
