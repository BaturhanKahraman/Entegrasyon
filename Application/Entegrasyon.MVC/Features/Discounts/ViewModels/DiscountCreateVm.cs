using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Discounts.ViewModels;

public class DiscountCreateVm
{
    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan buyuk olmalidir.")]
    public decimal Amount { get; set; }
    public DateTimeOffset? ExpiringDate { get; set; }
    public int? CustomerId { get; set; }
}
