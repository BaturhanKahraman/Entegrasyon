using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.Entity.Customers
{
    public class Customer : BaseEntity
    {
        public int Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string CustomerType { get; set; } = null!;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }
        public Address Address { get; set; } = null!;
        public IEnumerable<Sale> Sales { get; set; } = new List<Sale>();
        public IEnumerable<DiscountVoucher> DiscountVouchers { get; set; } = new List<DiscountVoucher>();
    }
}
