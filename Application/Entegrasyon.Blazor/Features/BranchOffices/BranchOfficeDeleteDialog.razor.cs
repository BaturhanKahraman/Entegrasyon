using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BranchOffices;

public partial class BranchOfficeDeleteDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public int BranchId { get; set; }

    [Parameter]
    public string BranchName { get; set; } = string.Empty;

    [Inject]
    private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;

    [Inject]
    private IOfficeStockManager OfficeStockManager { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private int _step = 1;
    private bool _loadingImpact;
    private bool _deleting;

    // Impact summary
    private int _stockCount;
    private int _totalStock;
    private int _marketPlaceCount;

    // Step 2 options
    private string _stockOption = "transfer";
    private int _targetBranchId;
    private List<BranchOfficePageListDto> _otherBranches = [];

    // Stocks for transfer/zero
    private List<BranchStockItemDto> _branchStocks = [];

    protected override async Task OnInitializedAsync()
    {
        _loadingImpact = true;

        // Load other branches for transfer option
        var branchResult = await BranchOfficeManager.GetPageBranchListAsync();
        if (branchResult.Success)
            _otherBranches = (branchResult.Data ?? []).Where(b => b.Id != BranchId).ToList();

        // Load stock info
        var stockResult = await BranchOfficeManager.GetBranchStocksAsync(BranchId);
        if (stockResult.Success)
        {
            _branchStocks = stockResult.Data ?? [];
            _stockCount = _branchStocks.Count;
            _totalStock = _branchStocks.Sum(s => s.CurrentStock);
        }

        // Load marketplace info
        var mpResult = await BranchOfficeManager.GetBranchMarketPlacesAsync(BranchId);
        if (mpResult.Success)
            _marketPlaceCount = mpResult.Data?.Count ?? 0;

        _loadingImpact = false;
    }

    private Task HandleStepTwoAsync()
    {
        if (_stockOption == "cancel")
        {
            Cancel();
            return Task.CompletedTask;
        }

        if (_stockOption == "transfer" && _targetBranchId <= 0)
        {
            Snackbar.Add("Lütfen bir hedef depo seçin.", Severity.Warning);
            return Task.CompletedTask;
        }

        _step = 3;
        return Task.CompletedTask;
    }

    private async Task ConfirmDeleteAsync()
    {
        _deleting = true;

        // Step 1: Handle stocks
        if (_totalStock > 0)
        {
            if (_stockOption == "transfer" && _targetBranchId > 0)
            {
                var items = _branchStocks
                    .Where(s => s.CurrentStock > 0)
                    .Select(s => new TransferItemDto(s.ProductVariantId, s.CurrentStock))
                    .ToList();

                var transferResult = await OfficeStockManager.TransferStockAsync(BranchId, _targetBranchId, items);
                if (!transferResult.Success)
                {
                    Snackbar.Add(transferResult.Message ?? "Stok transferi sırasında hata oluştu.", Severity.Error);
                    _deleting = false;
                    return;
                }
            }
            // Zero-out: nothing to do — soft delete handles it
        }

        // Step 2: Soft delete
        var deleteResult = await BranchOfficeManager.Delete(BranchId);
        _deleting = false;

        if (deleteResult.Success)
        {
            Snackbar.Add($"{BranchName} deposu silindi.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(deleteResult.Message ?? "Silme sırasında hata oluştu.", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}
