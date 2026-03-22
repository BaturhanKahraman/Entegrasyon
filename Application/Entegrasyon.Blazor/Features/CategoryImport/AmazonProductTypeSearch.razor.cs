using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class AmazonProductTypeSearch
{
    [Inject]
    private IAmazonProductTypeService ProductTypeService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Parameter]
    public List<AmazonProductTypeSearchResult> SelectedProductTypes { get; set; } = [];

    [Parameter]
    public EventCallback<List<AmazonProductTypeSearchResult>> SelectedProductTypesChanged { get; set; }

    private string SearchKeywords { get; set; } = string.Empty;
    private List<AmazonProductTypeSearchResult> SearchResults { get; set; } = [];
    private bool Loading { get; set; }

    private HashSet<AmazonProductTypeSearchResult> SelectedItemsInternal
    {
        get => SelectedProductTypes.ToHashSet();
        set => SelectedProductTypesChanged.InvokeAsync(value?.ToList() ?? []);
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(SearchKeywords))
        {
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        Loading = true;
        try
        {
            // TODO: MarketplaceId'yi config'den al
            var result = await ProductTypeService.SearchProductTypesAsync(
                SearchKeywords, "A33AVAJ2PDY3EV");

            if (result.Success && result.Data != null)
            {
                SearchResults = result.Data;
                Snackbar.Add($"{SearchResults.Count} product type bulundu.", Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message ?? "Arama başarısız.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            Loading = false;
        }
    }
}
