using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class ImportDialog : ComponentBase
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] public IBulkOperationManager BulkOperationManager { get; set; } = null!;

    [Parameter] public BulkOperationType OperationType { get; set; }

    private IBrowserFile? _selectedFile;
    private bool _isImporting;
    private string? _errorMessage;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private void OnFileSelected(IBrowserFile file)
    {
        _selectedFile = file;
        _errorMessage = null;
    }

    private async Task ImportAsync()
    {
        if (_selectedFile is null) return;

        _isImporting = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            await using var stream = _selectedFile.OpenReadStream(MaxFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            // Use a placeholder userId; in production this comes from auth
            var userId = Guid.Empty;

            var result = OperationType switch
            {
                BulkOperationType.ProductImport => await BulkOperationManager.ImportProductsAsync(ms, _selectedFile.Name, userId),
                BulkOperationType.PriceImport => await BulkOperationManager.ImportPricesAsync(ms, _selectedFile.Name, userId),
                BulkOperationType.StockImport => await BulkOperationManager.ImportStockAsync(ms, _selectedFile.Name, userId),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(result.Data));
            }
            else
            {
                _errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Dosya okunurken hata oluştu: {ex.Message}";
        }
        finally
        {
            _isImporting = false;
            StateHasChanged();
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private string GetTitle() => OperationType switch
    {
        BulkOperationType.ProductImport => "Ürün İçe Aktarma",
        BulkOperationType.PriceImport => "Fiyat İçe Aktarma",
        BulkOperationType.StockImport => "Stok İçe Aktarma",
        _ => "İçe Aktarma"
    };

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
