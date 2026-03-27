using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
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
    private bool _isValidating;
    private bool _isImporting;
    private string? _errorMessage;
    private ImportValidationPreviewDto? _validationPreview;
    private byte[]? _cachedFileBytes;
    private CancellationTokenSource? _importCts;
    private BulkOperationProgressDto? _currentProgress;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private void OnFileSelected(IBrowserFile file)
    {
        _selectedFile = file;
        _errorMessage = null;
        _validationPreview = null;
        _cachedFileBytes = null;
    }

    private async Task ValidateAsync()
    {
        if (_selectedFile is null) return;

        _isValidating = true;
        _errorMessage = null;
        _validationPreview = null;
        StateHasChanged();

        try
        {
            await using var stream = _selectedFile.OpenReadStream(MaxFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _cachedFileBytes = ms.ToArray();

            ms.Position = 0;
            var result = await BulkOperationManager.ValidateImportAsync(ms, OperationType);

            if (result.Success)
            {
                _validationPreview = result.Data;
            }
            else
            {
                _errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Dosya okunurken hata olustu: {ex.Message}";
        }
        finally
        {
            _isValidating = false;
            StateHasChanged();
        }
    }

    private async Task ImportAsync()
    {
        if (_cachedFileBytes is null) return;

        _isImporting = true;
        _errorMessage = null;
        _currentProgress = null;
        _importCts = new CancellationTokenSource();
        StateHasChanged();

        try
        {
            using var ms = new MemoryStream(_cachedFileBytes);
            var userId = Guid.Empty;
            var ct = _importCts.Token;
            var progress = new Progress<BulkOperationProgressDto>(p =>
            {
                _currentProgress = p;
                InvokeAsync(StateHasChanged);
            });

            var result = OperationType switch
            {
                BulkOperationType.ProductImport => await BulkOperationManager.ImportProductsAsync(ms, _selectedFile!.Name, userId, ct, progress),
                BulkOperationType.PriceImport => await BulkOperationManager.ImportPricesAsync(ms, _selectedFile!.Name, userId, ct, progress),
                BulkOperationType.StockImport => await BulkOperationManager.ImportStockAsync(ms, _selectedFile!.Name, userId, ct, progress),
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
        catch (OperationCanceledException)
        {
            _errorMessage = "Ice aktarma iptal edildi.";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Ice aktarma sirasinda hata olustu: {ex.Message}";
        }
        finally
        {
            _isImporting = false;
            _importCts?.Dispose();
            _importCts = null;
            StateHasChanged();
        }
    }

    private void CancelImport()
    {
        _importCts?.Cancel();
    }

    private void ResetValidation()
    {
        _selectedFile = null;
        _validationPreview = null;
        _cachedFileBytes = null;
        _errorMessage = null;
        StateHasChanged();
    }

    private void Cancel() => MudDialog.Cancel();

    private string GetTitle() => OperationType switch
    {
        BulkOperationType.ProductImport => "Urun Ice Aktarma",
        BulkOperationType.PriceImport => "Fiyat Ice Aktarma",
        BulkOperationType.StockImport => "Stok Ice Aktarma",
        _ => "Ice Aktarma"
    };

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
