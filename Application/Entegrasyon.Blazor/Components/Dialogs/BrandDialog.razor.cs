using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Dialogs;

public partial class BrandDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public BrandListDetailDto? Brand { get; set; }

    [Inject]
    private IBrandService BrandService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private readonly BrandFormModel _model = new();

    protected override void OnInitialized()
    {
        if (IsEditMode && Brand is not null)
            _model.Name = Brand.Name;
    }

    private async Task Submit()
    {
        await _form.Validate();
        if (!_isValid) return;

        _saving = true;
        try
        {
            if (IsEditMode)
            {
                var entityResult = await BrandService.GetBrandById(Brand!.Id);
                if (!entityResult.Success || entityResult.Data is null)
                {
                    Snackbar.Add(entityResult.Message ?? "Marka bulunamadı.", Severity.Error);
                    return;
                }

                var entity = entityResult.Data;
                entity.Name = _model.Name;

                var result = await BrandService.UpdateBrand(entity);
                if (result.Success)
                {
                    Snackbar.Add("Marka güncellendi.", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(result.Message ?? "Marka güncellenirken hata oluştu.", Severity.Error);
                }
            }
            else
            {
                var result = await BrandService.AddBrand(new AddBrandDto { Name = _model.Name });
                if (result.Success)
                {
                    Snackbar.Add("Marka eklendi.", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(result.Message ?? "Marka eklenirken hata oluştu.", Severity.Error);
                }
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private class BrandFormModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
