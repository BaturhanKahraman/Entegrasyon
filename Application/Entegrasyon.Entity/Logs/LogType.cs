namespace Entegrasyon.Entity.Logs;

public enum LogType
{
    User=1,
    Auth,
    Order,
    Product,
    Branch,
    Sale,
    Matching,
    Category,
    Role,
    Brand,
    Customer,
    DiscountVoucher,
    Marketplace,
    StockSync,
    Invoice,
    Settings,
    StockTransfer = 17,
    BranchDeletion = 18,

    Error = 999
}