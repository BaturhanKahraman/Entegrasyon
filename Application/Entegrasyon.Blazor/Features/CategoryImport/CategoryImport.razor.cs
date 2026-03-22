using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Blazor.ViewModels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class CategoryImport
{
    [Inject]
    private ILogger<CategoryImport> Logger { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private TrendyolCategoryImporter TrendyolImporter { get; set; } = null!;

    [Inject]
    private N11CategoryImporter N11Importer { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private EventChannel<CategoryImportRequestedEvent> ImportRequestedChannel { get; set; } = null!;

    [Inject]
    private EventChannel<CategoryImportCompletedEvent> ImportCompletedChannel { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private List<CategoryTreeNode> categories = [];
    private IReadOnlyCollection<CategoryTreeNode>? selectedNodes;
    private bool loading;
    private bool importing;
    private string searchQuery = string.Empty;

    // N11 state (separate from Trendyol)
    private List<CategoryTreeNode> n11Categories = [];
    private IReadOnlyCollection<CategoryTreeNode>? n11SelectedNodes;
    private bool n11Loading;

    private async Task LoadTrendyolCategoriesAsync()
    {
        loading = true;
        var result = await LoadCategoriesWithRetryAsync();
        if (result.Success && result.Data != null)
        {
            categories = result.Data.Select(MapToTreeNode).ToList();
            var totalCount = CountAllCategories(categories);
            Logger.LogInformation("Loaded {Count} categories from Trendyol", totalCount);
            Snackbar.Add($"{totalCount} kategori yüklendi.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Message ?? "Kategoriler yüklenemedi.", Severity.Error);
        }
        loading = false;
    }

    private async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> LoadCategoriesWithRetryAsync()
    {
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await TrendyolImporter.GetExternalCategoriesAsync();
            }
            catch (HttpRequestException ex) when (attempt < maxRetries - 1)
            {
                Logger.LogWarning(ex, "API call failed, attempt {Attempt}/{MaxRetries}", attempt + 1, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
        return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, "API çağrısı başarısız oldu.");
    }

    private async Task ShowImportConfirmationAsync()
    {
        if (selectedNodes == null || !selectedNodes.Any())
        {
            Snackbar.Add("Lütfen en az bir kategori seçin.", Severity.Warning);
            return;
        }

        await ImportSelectedCategoriesAsync();
    }

    private async Task ImportSelectedCategoriesAsync()
    {
        importing = true;
        try
        {
            // var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            // var user = authState.User;

            // if (!user.Identity?.IsAuthenticated == true)
            // {
            //     Snackbar.Add("Kullanıcı girişi gerekli.", Severity.Error);
            //     return;
            // }

            // Mock user ID for now
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var importRequests = selectedNodes!.Select(MapToImportRequest).ToList();

            // Publish import request event
            var importEvent = new CategoryImportRequestedEvent("Trendyol", importRequests, userId);
            await ImportRequestedChannel.PublishAsync(importEvent);

            Snackbar.Add("Kategori içe aktarma işlemi başlatıldı. Tamamlandığında bildirim alacaksınız.", Severity.Info);

            // Clear selection and navigate
            selectedNodes = null;
            NavigationManager.NavigateTo("/categories");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Category import request failed");
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            importing = false;
        }
    }

    private void RemoveSelectedCategory(CategoryTreeNode node)
    {
        if (selectedNodes != null)
        {
            var list = selectedNodes.ToList();
            list.Remove(node);
            selectedNodes = list;
        }
    }

    private void ClearSelection()
    {
        selectedNodes = null;
    }

    private int CountAllCategories(IEnumerable<CategoryTreeNode> nodes) =>
        nodes.Sum(n => 1 + CountAllCategories(n.Children));

    private CategoryTreeNode MapToTreeNode(ExternalCategoryDto dto) => new()
    {
        ExternalId = dto.ExternalId,
        Name = dto.Name,
        ParentExternalId = dto.ParentExternalId,
        Children = dto.Children.Select(MapToTreeNode).ToHashSet()
    };

    private ExternalCategoryImportRequest MapToImportRequest(CategoryTreeNode node) => new()
    {
        ExternalId = node.ExternalId,
        Name = node.Name,
        ParentExternalId = node.ParentExternalId,
        Children = node.Children
            .Where(c => selectedNodes?.Contains(c) == true)
            .Select(MapToImportRequest)
            .ToList()
    };

    private async Task LoadN11CategoriesAsync()
    {
        n11Loading = true;
        var result = await N11Importer.GetExternalCategoriesAsync();
        if (result.Success && result.Data != null)
        {
            n11Categories = result.Data.Select(MapToTreeNode).ToList();
            Snackbar.Add($"{n11Categories.Count} N11 üst kategori yüklendi.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Message ?? "N11 kategorileri yüklenemedi.", Severity.Error);
        }
        n11Loading = false;
    }

    private async Task ImportN11CategoriesAsync()
    {
        if (n11SelectedNodes == null || !n11SelectedNodes.Any())
        {
            Snackbar.Add("Lütfen en az bir kategori seçin.", Severity.Warning);
            return;
        }

        importing = true;
        try
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var importRequests = n11SelectedNodes.Select(MapToImportRequest).ToList();
            var importEvent = new CategoryImportRequestedEvent("N11", importRequests, userId);
            await ImportRequestedChannel.PublishAsync(importEvent);
            Snackbar.Add("N11 kategori içe aktarma işlemi başlatıldı.", Severity.Info);
            n11SelectedNodes = null;
            NavigationManager.NavigateTo("/categories");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 category import request failed");
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            importing = false;
        }
    }

    private void RemoveN11SelectedCategory(CategoryTreeNode node)
    {
        if (n11SelectedNodes != null)
        {
            var list = n11SelectedNodes.ToList();
            list.Remove(node);
            n11SelectedNodes = list;
        }
    }

    private void ClearN11Selection()
    {
        n11SelectedNodes = null;
    }

}
