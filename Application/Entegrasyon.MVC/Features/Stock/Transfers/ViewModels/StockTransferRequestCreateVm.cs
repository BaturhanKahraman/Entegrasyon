using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Stock.Transfers.ViewModels;

public class StockTransferRequestCreateVm
{
    [Required(ErrorMessage = "Kaynak şube zorunludur.")]
    public int SourceBranchOfficeId { get; set; }

    [Required(ErrorMessage = "Hedef şube zorunludur.")]
    public int TargetBranchOfficeId { get; set; }

    public List<TransferLineVm> Items { get; set; } = new();

    public List<BranchOption> AvailableBranches { get; set; } = new();
}

public class TransferLineVm
{
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

public record BranchOption(int Id, string Name);
