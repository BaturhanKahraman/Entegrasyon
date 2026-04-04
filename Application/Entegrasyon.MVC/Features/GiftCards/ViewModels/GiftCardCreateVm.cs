using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.GiftCards.ViewModels;

public class GiftCardCreateVm
{
    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Range(1, 100000, ErrorMessage = "Tutar 1 ile 100.000 arasinda olmalidir.")]
    public decimal Amount { get; set; }

    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Message { get; set; }
}
