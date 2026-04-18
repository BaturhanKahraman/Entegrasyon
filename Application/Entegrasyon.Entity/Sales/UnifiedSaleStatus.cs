namespace Entegrasyon.Entity.Sales;

/// <summary>
/// Liste UI'sinde gösterilecek 6 normalize durum.
/// Sale.SaleStatus, Order.StorefrontOrderStatus ve MarketplaceOrderStatus
/// bu enum'a indirgenerek gösterilir.
/// </summary>
public enum UnifiedSaleStatus
{
    Completed     = 1,
    Pending       = 2,
    Shipping      = 3,
    PartialReturn = 4,
    FullReturn    = 5,
    Cancelled     = 6
}
