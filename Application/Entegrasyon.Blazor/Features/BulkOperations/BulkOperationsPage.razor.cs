using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class BulkOperationsPage : ComponentBase
{
    [Inject] public IBulkOperationManager BulkOperationManager { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    private List<BulkOperationLog>? _recentOperations;
    private bool _isExporting;
    private BulkOperationType _currentExportType;
    private DateRange? _exportDateRange;
    private CancellationTokenSource? _exportCts;
    private ExportFormat _exportFormat = ExportFormat.Excel;
    private List<ExportColumnDto>? _availableProductColumns;
    private List<ExportColumnDto>? _availablePriceColumns;
    private List<ExportColumnDto>? _availableStockColumns;
    private List<string>? _selectedProductColumns;
    private List<string>? _selectedPriceColumns;
    private List<string>? _selectedStockColumns;

    protected override async Task OnInitializedAsync()
    {
        await LoadRecentOperationsAsync();
        await LoadAvailableColumnsAsync();
    }

    private async Task LoadAvailableColumnsAsync()
    {
        var productResult = await BulkOperationManager.GetAvailableColumnsAsync(BulkOperationType.ProductExport);
        if (productResult.Success) _availableProductColumns = productResult.Data;

        var priceResult = await BulkOperationManager.GetAvailableColumnsAsync(BulkOperationType.PriceExport);
        if (priceResult.Success) _availablePriceColumns = priceResult.Data;

        var stockResult = await BulkOperationManager.GetAvailableColumnsAsync(BulkOperationType.StockExport);
        if (stockResult.Success) _availableStockColumns = stockResult.Data;
    }

    private async Task LoadRecentOperationsAsync()
    {
        var result = await BulkOperationManager.GetRecentOperationsAsync();
        if (result.Success)
            _recentOperations = result.Data;
    }

    private async Task OpenImportDialogAsync(BulkOperationType type)
    {
        var parameters = new DialogParameters<ImportDialog>
        {
            { x => x.OperationType, type }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ImportDialog>(GetImportTitle(type), parameters, options);
        var dialogResult = await dialog.Result;

        if (dialogResult is { Canceled: false, Data: BulkImportResultDto importResult })
        {
            await ShowImportResultAsync(importResult);
            await LoadRecentOperationsAsync();
            StateHasChanged();
        }
    }

    private async Task ShowImportResultAsync(BulkImportResultDto result)
    {
        var parameters = new DialogParameters<ImportResultDialog>
        {
            { x => x.ImportResult, result }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        await DialogService.ShowAsync<ImportResultDialog>("İçe Aktarma Sonucu", parameters, options);
    }

    private async Task DownloadTemplateAsync(BulkOperationType type)
    {
        var result = await BulkOperationManager.GetImportTemplateAsync(type);
        if (!result.Success)
        {
            Snackbar.Add(result.Message ?? "Şablon oluşturulamadı.", Severity.Error);
            return;
        }

        var fileName = type switch
        {
            BulkOperationType.ProductImport => "urun-import-sablonu.xlsx",
            BulkOperationType.PriceImport => "fiyat-import-sablonu.xlsx",
            BulkOperationType.StockImport => "stok-import-sablonu.xlsx",
            _ => "sablon.xlsx"
        };

        await DownloadFileAsync(result.Data, fileName);
    }

    private async Task ExportAsync(BulkOperationType type)
    {
        _isExporting = true;
        _currentExportType = type;
        _exportCts = new CancellationTokenSource();
        StateHasChanged();

        try
        {
            var selectedColumns = type switch
            {
                BulkOperationType.ProductExport => _selectedProductColumns,
                BulkOperationType.PriceExport => _selectedPriceColumns,
                BulkOperationType.StockExport => _selectedStockColumns,
                _ => null
            };

            var filter = new ExportFilterDto(
                DateFrom: _exportDateRange?.Start.HasValue == true
                    ? new DateTimeOffset(_exportDateRange.Start.Value, TimeSpan.Zero)
                    : null,
                DateTo: _exportDateRange?.End.HasValue == true
                    ? new DateTimeOffset(_exportDateRange.End.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero)
                    : null,
                Format: _exportFormat,
                SelectedColumns: selectedColumns);
            var ct = _exportCts.Token;
            var result = type switch
            {
                BulkOperationType.ProductExport => await BulkOperationManager.ExportProductsAsync(filter, ct),
                BulkOperationType.PriceExport => await BulkOperationManager.ExportPricesAsync(filter, ct),
                BulkOperationType.StockExport => await BulkOperationManager.ExportStockAsync(filter, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            if (!result.Success)
            {
                Snackbar.Add(result.Message ?? "Dışa aktarma başarısız.", Severity.Error);
                return;
            }

            var ext = _exportFormat == ExportFormat.Csv ? "csv" : "xlsx";
            var fileName = type switch
            {
                BulkOperationType.ProductExport => $"urunler-{DateTime.Now:yyyyMMdd-HHmm}.{ext}",
                BulkOperationType.PriceExport => $"fiyatlar-{DateTime.Now:yyyyMMdd-HHmm}.{ext}",
                BulkOperationType.StockExport => $"stoklar-{DateTime.Now:yyyyMMdd-HHmm}.{ext}",
                _ => $"export.{ext}"
            };

            await DownloadFileAsync(result.Data, fileName);
            Snackbar.Add("Dışa aktarma tamamlandı.", Severity.Success);
        }
        catch (OperationCanceledException)
        {
            Snackbar.Add("Dışa aktarma iptal edildi.", Severity.Info);
        }
        finally
        {
            _isExporting = false;
            _exportCts?.Dispose();
            _exportCts = null;
            StateHasChanged();
        }
    }

    private void CancelExport()
    {
        _exportCts?.Cancel();
    }

    private async Task DownloadFileAsync(byte[] fileBytes, string fileName)
    {
        var base64 = Convert.ToBase64String(fileBytes);
        await JsRuntime.InvokeVoidAsync("downloadFile", fileName, base64);
    }

    private static string GetImportTitle(BulkOperationType type) => type switch
    {
        BulkOperationType.ProductImport => "Ürün İçe Aktarma",
        BulkOperationType.PriceImport => "Fiyat İçe Aktarma",
        BulkOperationType.StockImport => "Stok İçe Aktarma",
        _ => "İçe Aktarma"
    };

    private static Color GetStatusColor(BulkOperationStatus status) => status switch
    {
        BulkOperationStatus.Pending => Color.Default,
        BulkOperationStatus.Processing => Color.Info,
        BulkOperationStatus.Completed => Color.Success,
        BulkOperationStatus.CompletedWithErrors => Color.Warning,
        BulkOperationStatus.Failed => Color.Error,
        BulkOperationStatus.Cancelled => Color.Warning,
        _ => Color.Default
    };

    private static string GetStatusText(BulkOperationStatus status) => status switch
    {
        BulkOperationStatus.Pending => "Bekliyor",
        BulkOperationStatus.Processing => "İşleniyor",
        BulkOperationStatus.Completed => "Tamamlandı",
        BulkOperationStatus.CompletedWithErrors => "Kısmi Başarılı",
        BulkOperationStatus.Failed => "Başarısız",
        BulkOperationStatus.Cancelled => "İptal Edildi",
        _ => "Bilinmiyor"
    };
}
