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

    Error = 999
}