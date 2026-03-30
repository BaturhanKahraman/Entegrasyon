using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BranchOffices;

public partial class BranchOfficesPage : ComponentBase
{
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<BranchOfficePageListDto> _branches = [];
    private bool _loading;
    private string _searchTerm = string.Empty;

    private Func<BranchOfficePageListDto, bool> QuickFilter => branch =>
        string.IsNullOrWhiteSpace(_searchTerm) ||
        branch.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        _loading = true;
        var result = await BranchOfficeManager.GetPageBranchListAsync();
        _loading = false;

        if (result.Success)
            _branches = result.Data ?? [];
        else
            Snackbar.Add(result.Message ?? "Depolar yüklenemedi.", Severity.Error);
    }

    private async Task OpenAddDialog()
    {
        var dialog = await DialogService.ShowAsync<BranchOfficeDialog>("Yeni Depo Ekle");
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadBranchesAsync();
    }

    private async Task OpenEditDialog(BranchOfficePageListDto branch)
    {
        var parameters = new DialogParameters<BranchOfficeDialog>
        {
            { x => x.IsEditMode, true },
            { x => x.BranchId, branch.Id },
            { x => x.BranchName, branch.Name },
            { x => x.IsDefaultMarketPlaceStock, branch.IsDefaultMarketPlaceStock }
        };
        var dialog = await DialogService.ShowAsync<BranchOfficeDialog>("Depo Düzenle", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadBranchesAsync();
    }

    private void NavigateToDetail(int id)
    {
        Navigation.NavigateTo($"/branch-offices/{id}");
    }

    private async Task OpenDeleteDialog(BranchOfficePageListDto branch)
    {
        var parameters = new DialogParameters
        {
            { nameof(BranchOfficeDeleteDialog.BranchId), branch.Id },
            { nameof(BranchOfficeDeleteDialog.BranchName), branch.Name }
        };
        var dialog = await DialogService.ShowAsync<BranchOfficeDeleteDialog>("Depo Sil", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadBranchesAsync();
    }
}
