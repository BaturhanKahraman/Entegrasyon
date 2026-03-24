using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.CommissionRates;

public partial class CommissionRateDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public SaveCommissionRateDto Dto { get; set; } = new();

    [Inject] private ICommissionCalculator CommissionCalculator { get; set; } = null!;
    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private MudForm _form = null!;
    private bool _saving;
    private CategorySelectItem? _selectedCategory;

    protected override void OnParametersSet()
    {
        if (Dto.CategoryId.HasValue)
        {
            _selectedCategory = new CategorySelectItem(Dto.CategoryId.Value, "");
        }
    }

    private async Task<IEnumerable<CategorySelectItem>> SearchCategories(string? value, CancellationToken ct)
    {
        var result = await CategoryService.GetCategoryDetailList();
        if (!result.Success) return [];

        var categories = result.Data
            .Select(c => new CategorySelectItem(c.Id, c.Name))
            .ToList();

        if (string.IsNullOrWhiteSpace(value))
            return categories;

        return categories.Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private async Task Save()
    {
        await _form.Validate();
        if (!_form.IsValid) return;

        _saving = true;
        try
        {
            if (!Dto.IsDefault && _selectedCategory is not null)
                Dto.CategoryId = _selectedCategory.Id;
            else if (Dto.IsDefault)
                Dto.CategoryId = null;

            var result = await CommissionCalculator.SaveCommissionRateAsync(Dto);

            if (result.Success)
            {
                Snackbar.Add(result.Message ?? "Kaydedildi.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message ?? "Hata oluştu.", Severity.Error);
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    internal sealed record CategorySelectItem(int Id, string Name);
}
