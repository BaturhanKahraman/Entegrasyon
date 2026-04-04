using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.StockMovements.ViewModels;

public class StockAdjustVm
{
    [Required]
    public int BranchOfficeId { get; set; }

    [Required]
    public Guid ProductVariantId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public bool IsIncrease { get; set; } = true;

    public string? Note { get; set; }
}
