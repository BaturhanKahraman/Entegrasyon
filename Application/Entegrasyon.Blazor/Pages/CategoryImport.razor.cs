using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services.Channels.Events;
using Entegrasyon.Blazor.ViewModels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Shared.Results;

namespace Entegrasyon.Blazor.Pages;

public partial class CategoryImport
{
    [Inject]
    private ILogger<CategoryImport> Logger { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private TrendyolCategoryImporter TrendyolImporter { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private EventChannel<CategoryImportRequestedEvent> ImportRequestedChannel { get; set; } = null!;

    [Inject]
    private EventChannel<CategoryImportCompletedEvent> ImportCompletedChannel { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

     private List<CategoryTreeNode> categories = new();
    private IReadOnlyCollection<CategoryTreeNode>? selectedNodes;
    private bool loading;
    private bool importing;
    private string searchQuery = string.Empty;

    private async Task LoadTrendyolCategoriesAsync()
    {
        loading = true;
        try
        {
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
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Category loading failed");
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            loading = false;
        }
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

}
