using NpgsqlTypes;

namespace Entegrasyon.Entity.Customers
{
    public class CorporateCustomer:Customer
    {
        public string TaxNumber { get; set; }
        public string CorporateName { get; set; }
        public NpgsqlTsVector CorporateSearchVector { get; set; }

    }
}
