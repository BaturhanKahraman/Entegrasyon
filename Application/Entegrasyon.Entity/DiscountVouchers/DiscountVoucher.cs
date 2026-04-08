using Entegrasyon.Entity.Customers;

namespace Entegrasyon.Entity.DiscountVouchers;

public sealed class DiscountVoucher : BaseEntity
{
    public int Id { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public double Percentage { get; set; }
    public decimal Amount { get; set; }
    public string? Code { get; set; }
    public DateTimeOffset? ExpiringDate { get; set; }
    public bool IsActive { get; set; }
    public int? MaxUsageCount { get; set; }
    public int CurrentUsageCount { get; set; }
    public decimal? MinimumCartAmount { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}

public enum DiscountType
{
    FixedAmount = 0,
    Percentage = 1
}