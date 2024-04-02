using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.User;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales
{
    public sealed class Sale : BaseEntity
    {
        public Guid Id { get; set; }
        public int? DiscountVoucherId { get; set; }
        public DiscountVoucher DiscountVoucher { get; set; }
        public IEnumerable<SaleItem> SaleItems { get; set; }
        public Guid SalePersonId { get; set; }
        public ApplicationUser SalePerson { get; set; }
        public int BranchOfficeId { get; set; }
        public BranchOffice BranchOffice { get; set; }
        public double GeneralDiscount { get; set; }
        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }


    }
}
