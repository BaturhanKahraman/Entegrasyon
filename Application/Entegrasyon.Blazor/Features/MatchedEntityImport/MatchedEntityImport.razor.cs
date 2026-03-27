using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Templates;
using Entegrasyon.Entity.Templates;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MatchedEntityImport;

public partial class MatchedEntityImport
{
    [Inject] private IMatchedEntityImportManager ImportManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<MatchedEntityPackageDto> _packages = [];
    private string? _searchTerm;
    private int? _typeFilter;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPackagesAsync();
    }

    private async Task LoadPackagesAsync()
    {
        _loading = true;
        var typeFilter = _typeFilter.HasValue ? (MatchedEntityType)_typeFilter.Value : (MatchedEntityType?)null;
        var result = await ImportManager.GetAvailablePackagesAsync(typeFilter, _searchTerm);

        if (result.Success)
            _packages = result.Data;

        _loading = false;
    }

    private async Task OnSearchChanged(string _)
    {
        await LoadPackagesAsync();
    }

    private async Task OnTypeFilterChanged(int? value)
    {
        _typeFilter = value;
        await LoadPackagesAsync();
    }

    private async Task ShowDetailDialog(MatchedEntityPackageDto package)
    {
        var parameters = new DialogParameters<MatchedEntityDetailDialog>
        {
            { x => x.PackageId, package.Id }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true
        };

        await DialogService.ShowAsync<MatchedEntityDetailDialog>(package.Name, parameters, options);
    }

    private async Task StartImport(MatchedEntityPackageDto package)
    {
        // Cakisma tespiti
        var conflictsResult = await ImportManager.DetectConflictsAsync(package.Id);
        if (!conflictsResult.Success)
        {
            Snackbar.Add(conflictsResult.Message ?? "", Severity.Error);
            return;
        }

        if (conflictsResult.Data.Count > 0)
        {
            // Cakisma dialog'u goster
            var parameters = new DialogParameters<ImportConflictDialog>
            {
                { x => x.PackageId, package.Id },
                { x => x.PackageName, package.Name },
                { x => x.Conflicts, conflictsResult.Data }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true
            };

            var dialog = await DialogService.ShowAsync<ImportConflictDialog>("Çakışma Tespit Edildi", parameters, options);
            var dialogResult = await dialog.Result;

            if (dialogResult is { Canceled: false })
            {
                await LoadPackagesAsync();
                Snackbar.Add("Paket başarıyla import edildi.", Severity.Success);
                Navigation.NavigateTo("/categories");
            }
        }
        else
        {
            // Direkt import
            var request = new ImportPackageRequest { PackageId = package.Id };
            var result = await ImportManager.ImportPackageAsync(request);

            if (result.Success)
            {
                Snackbar.Add("Paket başarıyla import edildi.", Severity.Success);
                Navigation.NavigateTo("/categories");
            }
            else
            {
                Snackbar.Add(result.Message ?? "", Severity.Error);
            }
        }
    }
}
