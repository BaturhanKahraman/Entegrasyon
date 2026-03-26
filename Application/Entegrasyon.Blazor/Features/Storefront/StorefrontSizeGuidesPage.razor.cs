using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontSizeGuidesPage
{
    [Inject] private IStorefrontSizeGuideManager SizeGuideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<StorefrontSizeGuide> _guides = [];
    private bool _loading = true;
    private bool _dialogVisible;
    private StorefrontSizeGuide _editingGuide = new() { SizeData = "[]" };

    private readonly DialogOptions _dialogOptions = new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadGuides();
    }

    private async Task LoadGuides()
    {
        _loading = true;
        var result = await SizeGuideManager.GetAllSizeGuidesAsync(1);
        if (result.Success)
            _guides = result.Data;
        _loading = false;
    }

    private void OpenCreateDialog()
    {
        _editingGuide = new StorefrontSizeGuide
        {
            TenantId = 1,
            IsActive = true,
            SizeData = "[]"
        };
        _dialogVisible = true;
    }

    private void OpenEditDialog(StorefrontSizeGuide guide)
    {
        _editingGuide = new StorefrontSizeGuide
        {
            Id = guide.Id,
            TenantId = guide.TenantId,
            Name = guide.Name,
            CategoryIds = guide.CategoryIds,
            MeasurementImageUrl = guide.MeasurementImageUrl,
            MeasurementInstructions = guide.MeasurementInstructions,
            SizeData = guide.SizeData,
            IsActive = guide.IsActive
        };
        _dialogVisible = true;
    }

    private void CloseDialog()
    {
        _dialogVisible = false;
    }

    private async Task SaveGuide()
    {
        var result = await SizeGuideManager.CreateOrUpdateAsync(_editingGuide);
        if (result.Success)
        {
            Snackbar.Add(result.Message, Severity.Success);
            _dialogVisible = false;
            await LoadGuides();
        }
        else
        {
            Snackbar.Add(result.Message, Severity.Error);
        }
    }

    private async Task DeleteGuide(int id)
    {
        var result = await SizeGuideManager.DeleteAsync(id);
        if (result.Success)
        {
            Snackbar.Add(result.Message, Severity.Success);
            await LoadGuides();
        }
        else
        {
            Snackbar.Add(result.Message, Severity.Error);
        }
    }
}
