using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.MVC.Features.Discounts.ViewModels;

public class DiscountCreateVm
{
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Tutar 0'dan buyuk olmalidir.")]
    public decimal Amount { get; set; }

    [Range(0.01, 100, ErrorMessage = "Yuzde 0-100 arasinda olmalidir.")]
    public double Percentage { get; set; }

    public DateTimeOffset? ExpiringDate { get; set; }
    public int? CustomerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Kullanim limiti en az 1 olmalidir.")]
    public int? MaxUsageCount { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Minimum sepet tutari 0'dan buyuk olmalidir.")]
    public decimal? MinimumCartAmount { get; set; }
}
