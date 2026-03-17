using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.Entity.Customers
{
    public class Customer : BaseEntity
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string CustomerType { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public Address Address { get; set; } = null!;
        public IEnumerable<Sale> Sales { get; set; } = new List<Sale>();
        public IEnumerable<DiscountVoucher> DiscountVouchers { get; set; } = new List<DiscountVoucher>();
    }
}
