using NpgsqlTypes;

namespace Entegrasyon.Entity.Customers
{
    public class RetailCustomer:Customer
    {
        public string NationalIdentity { get; set; }
        public NpgsqlTsVector SearchVector { get; set; }
    }
}
