using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Sales
{
    public sealed class Sale : BaseEntity
    {
        public Guid Id { get; set; }
        public int? DiscountVoucherId { get; set; }
        public DiscountVoucher? DiscountVoucher { get; set; }
        public IEnumerable<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public Guid SalePersonId { get; set; }
        public ApplicationUser SalePerson { get; set; } = null!;
        public int BranchOfficeId { get; set; }
        public BranchOffice BranchOffice { get; set; } = null!;
        public double GeneralDiscount { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [StringLength(50)]
        public string SaleNumber { get; set; } = "";

        public DateTimeOffset SaleDate { get; set; }
        public SaleSource SaleSource { get; set; }
        public SaleStatus SaleStatus { get; set; } = SaleStatus.Completed;

        [StringLength(500)]
        public string? Note { get; set; }

        public ICollection<SalePayment> Payments { get; set; } = [];
        public ICollection<SaleReturn> Returns { get; set; } = [];
    }
}
