using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BranchOffices;

public partial class BranchOfficeDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public int BranchId { get; set; }

    [Parameter]
    public string BranchName { get; set; } = string.Empty;

    [Parameter]
    public bool IsDefaultMarketPlaceStock { get; set; }

    [Inject]
    private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private readonly BranchFormModel _model = new();

    protected override void OnInitialized()
    {
        if (IsEditMode)
        {
            _model.Name = BranchName;
            _model.IsDefaultMarketPlaceStock = IsDefaultMarketPlaceStock;
        }
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
                var dto = new BranchOfficeEditDto(BranchId, _model.Name);
                var result = await BranchOfficeManager.Update(dto);
                if (result.Success)
                {
                    Snackbar.Add("Depo güncellendi.", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(result.Message ?? "Depo güncellenirken hata oluştu.", Severity.Error);
                }
            }
            else
            {
                var dto = new BranchOfficeAddDto(_model.Name);
                var result = await BranchOfficeManager.AddBranch(dto);
                if (result.Success)
                {
                    Snackbar.Add("Depo eklendi.", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(result.Message ?? "Depo eklenirken hata oluştu.", Severity.Error);
                }
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private class BranchFormModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDefaultMarketPlaceStock { get; set; }
    }
}
