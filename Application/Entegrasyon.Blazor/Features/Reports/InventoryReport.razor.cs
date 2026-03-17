using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class InventoryReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private List<BranchOffice> _branches = [];
    private int? _selectedBranchId;
    private StockFilter _stockFilter = StockFilter.All;
    private bool _loading;
    private string _searchTerm = string.Empty;
    private InventoryReportDto? _report;

    private Func<StockItemDto, bool> QuickFilter => item =>
        string.IsNullOrWhiteSpace(_searchTerm) ||
        item.ProductTitle.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
        (item.Barcode?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);

    protected override async Task OnInitializedAsync()
    {
        var result = await BranchOfficeManager.GetBranchList();
        if (result.Success)
            _branches = result.Data ?? [];
    }

    private async Task GenerateReportAsync()
    {
        _loading = true;
        try
        {
            var filter = new InventoryReportFilterDto(_selectedBranchId, _stockFilter);
            _report = await ReportManager.GetInventoryReportAsync(filter);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Rapor oluşturulamadı: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
