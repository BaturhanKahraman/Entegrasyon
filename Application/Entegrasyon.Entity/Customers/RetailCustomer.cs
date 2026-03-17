using NpgsqlTypes;

namespace Entegrasyon.Entity.Customers
{
    public class RetailCustomer:Customer
    {
        public string NationalIdentity { get; set; } = null!;

        public NpgsqlTsVector RetailSearchVector { get; set; } = null!;
    }
}
