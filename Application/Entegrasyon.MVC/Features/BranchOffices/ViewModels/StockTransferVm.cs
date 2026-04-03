using Entegrasyon.Entity.Dtos.Branches;

namespace Entegrasyon.MVC.Features.BranchOffices.ViewModels;

public class StockTransferVm
{
    public int SourceBranchId { get; set; }
    public string SourceBranchName { get; set; } = null!;
    public List<BranchOfficePageListDto> AvailableBranches { get; set; } = [];
    public List<BranchStockItemDto> SourceStocks { get; set; } = [];
}

public class StockTransferPostVm
{
    public int SourceBranchId { get; set; }
    public int TargetBranchId { get; set; }
    public List<TransferLineVm> Items { get; set; } = [];
}

public class TransferLineVm
{
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}
