using NpgsqlTypes;

namespace Entegrasyon.Entity.Customers
{
    public class CorporateCustomer:Customer
    {
        public string TaxNumber { get; set; } = null!;
        public string CorporateName { get; set; } = null!;

        public NpgsqlTsVector CorporateSearchVector { get; set; } = null!;

    }
}
