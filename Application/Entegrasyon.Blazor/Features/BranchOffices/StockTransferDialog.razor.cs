using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BranchOffices;

public partial class StockTransferDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public int SourceBranchId { get; set; }

    [Parameter]
    public List<BranchStockItemDto> SelectedItems { get; set; } = [];

    [Inject]
    private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;

    [Inject]
    private IOfficeStockManager OfficeStockManager { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<BranchOfficePageListDto> _targetBranches = [];
    private List<TransferItemModel> _transferItems = [];
    private int _targetBranchId;
    private bool _transferring;

    protected override async Task OnInitializedAsync()
    {
        var result = await BranchOfficeManager.GetPageBranchListAsync();
        if (result.Success)
        {
            _targetBranches = (result.Data ?? [])
                .Where(b => b.Id != SourceBranchId)
                .ToList();
        }

        _transferItems = SelectedItems.Select(s => new TransferItemModel
        {
            ProductVariantId = s.ProductVariantId,
            ProductName = s.ProductName,
            CurrentStock = s.CurrentStock,
            Quantity = s.CurrentStock
        }).ToList();
    }

    private async Task TransferAsync()
    {
        if (_targetBranchId <= 0) return;

        _transferring = true;
        var items = _transferItems
            .Where(i => i.Quantity > 0)
            .Select(i => new TransferItemDto(i.ProductVariantId, i.Quantity))
            .ToList();

        var result = await OfficeStockManager.TransferStockAsync(SourceBranchId, _targetBranchId, items);
        _transferring = false;

        if (result.Success)
        {
            Snackbar.Add(result.Message ?? $"{items.Count} ürün başarıyla transfer edildi.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.Message ?? "Transfer sırasında hata oluştu.", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private class TransferItemModel
    {
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int Quantity { get; set; }
    }
}
