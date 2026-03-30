using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.MasterCatalog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class MasterCatalogImportDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IMasterCatalogImportService ImportService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _loading = true;
    private bool _importing;
    private int _activeTab;

    private List<SectorPackageDto> _sectorPackages = [];
    private int? _selectedPackageId;

    private List<FlatLeafCategory> _flatLeafCategories = [];
    private HashSet<int> _selectedCategoryIds = [];

    private bool HasSelection =>
        _activeTab == 0 ? _selectedPackageId.HasValue : _selectedCategoryIds.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        try
        {
            _sectorPackages = (await ImportService.GetSectorPackagesAsync()).ToList();
            var tree = await ImportService.GetMasterCategoryTreeAsync();
            _flatLeafCategories = FlattenLeafNodes(tree, "");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Veriler yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void SelectPackage(int packageId)
    {
        _selectedPackageId = _selectedPackageId == packageId ? null : packageId;
    }

    private void ToggleCategory(int id, bool selected)
    {
        if (selected) _selectedCategoryIds.Add(id);
        else _selectedCategoryIds.Remove(id);
    }

    private async Task Import()
    {
        _importing = true;
        try
        {
            IList<int> categoryIds;

            if (_activeTab == 0 && _selectedPackageId.HasValue)
            {
                categoryIds = await ImportService.GetSectorPackageCategoryIdsAsync(_selectedPackageId.Value);
            }
            else
            {
                categoryIds = _selectedCategoryIds.ToList();
            }

            if (categoryIds.Count == 0)
            {
                Snackbar.Add("Lütfen en az bir kategori seçin.", Severity.Warning);
                return;
            }

            // TenantId = 0 (mevcut tek tenant setup'ında kullanılmıyor)
            var result = await ImportService.ImportFromMasterAsync(0, categoryIds);
            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"İçe aktarma hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _importing = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private static List<FlatLeafCategory> FlattenLeafNodes(
        IEnumerable<MasterCategoryTreeDto> nodes,
        string parentPath)
    {
        var result = new List<FlatLeafCategory>();
        foreach (var node in nodes)
        {
            var path = string.IsNullOrEmpty(parentPath) ? node.Name : $"{parentPath} > {node.Name}";
            if (node.IsLeaf)
                result.Add(new FlatLeafCategory(node.Id, path));
            else
                result.AddRange(FlattenLeafNodes(node.Children, path));
        }
        return result;
    }

    private sealed record FlatLeafCategory(int Id, string FullPath);
}
