using NpgsqlTypes;

namespace Entegrasyon.Entity.Customers
{
    public class RetailCustomer:Customer
    {
        public string NationalIdentity { get; set; }

        public NpgsqlTsVector RetailSearchVector { get; set; }
    }
}
