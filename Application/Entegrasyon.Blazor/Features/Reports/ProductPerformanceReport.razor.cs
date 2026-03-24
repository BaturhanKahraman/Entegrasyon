using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class ProductPerformanceReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private DateTime? _startDate = DateTime.Today.AddDays(-30);
    private DateTime? _endDate = DateTime.Today;
    private bool _loading;
    private string _searchTerm = string.Empty;
    private List<ProductPerformanceDto> _products = [];

    private Func<ProductPerformanceDto, bool> QuickFilter => item =>
        string.IsNullOrWhiteSpace(_searchTerm) ||
        item.ProductName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);

    private async Task GenerateReportAsync()
    {
        if (_startDate is null || _endDate is null)
        {
            Snackbar.Add("Lutfen tarih araligi secin.", Severity.Warning);
            return;
        }

        _loading = true;
        try
        {
            var filter = new ProductPerformanceFilterDto(
                DateOnly.FromDateTime(_startDate.Value),
                DateOnly.FromDateTime(_endDate.Value));

            _products = await ReportManager.GetProductPerformanceAsync(filter);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Rapor olusturulamadi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
