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
        public ICollection<SaleItem> SaleItems { get; set; } = [];
        public Guid SalePersonId { get; set; }
        public ApplicationUser SalePerson { get; set; } = null!;
        public int BranchOfficeId { get; set; }
        public BranchOffice BranchOffice { get; set; } = null!;
        public decimal GeneralDiscount { get; set; }

        public int? GeneralDiscountReasonId { get; set; }
        public DiscountReason? GeneralDiscountReason { get; set; }

        [StringLength(200)]
        public string? GeneralDiscountReasonNote { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [StringLength(50)]
        public string SaleNumber { get; set; } = "";

        [StringLength(16)]
        public string? ReturnCode { get; set; }

        public DateTimeOffset SaleDate { get; set; }
        public SaleSource SaleSource { get; set; }
        public SaleStatus SaleStatus { get; set; } = SaleStatus.Completed;

        /// <summary>Offline POS senkronu için idempotency anahtarı (UUID v4). Online satışlarda null.</summary>
        [StringLength(64)]
        public string? IdempotencyKey { get; set; }

        /// <summary>Satışın gerçekleştiği zaman (offline'da local time, sonra UTC'ye dönüştürülür). Null ise SaleDate kullanılır.</summary>
        public DateTimeOffset? OccurredAt { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public ICollection<SalePayment> Payments { get; set; } = [];
        public ICollection<SaleReturn> Returns { get; set; } = [];
    }
}
