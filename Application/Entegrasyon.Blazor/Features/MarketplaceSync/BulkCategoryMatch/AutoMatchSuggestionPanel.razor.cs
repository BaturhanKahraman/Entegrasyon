using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch;

public partial class AutoMatchSuggestionPanel
{
    [Parameter] public HashSet<Category> SelectedCategories { get; set; } = [];
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public EventCallback<(int CategoryId, MarketplaceCategorySearchResult Result)> OnSuggestionApplied { get; set; }

    [Inject] private ICategoryAutoMatchService AutoMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<CategoryAutoMatchSuggestionDto> Suggestions { get; set; } = [];
    private bool IsLoading { get; set; }
    private bool IsOllamaAvailable { get; set; }
    private bool HasRequested { get; set; }

    private int HighConfidenceCount => Suggestions.Count(s => s.Confidence >= 0.8);

    protected override async Task OnInitializedAsync()
    {
        IsOllamaAvailable = await AutoMatchService.IsAvailableAsync();
    }

    private async Task RequestSuggestions()
    {
        if (SelectedCategories.Count == 0) return;

        IsLoading = true;
        HasRequested = true;
        Suggestions.Clear();

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = MarketPlaceId,
            Categories = SelectedCategories.Select(c => new CategoryAutoMatchItemDto
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                ParentCategoryName = null // Could be enriched with parent info
            }).ToList()
        };

        var result = await AutoMatchService.GetAutoMatchSuggestionsAsync(request);

        if (result.Success)
        {
            Suggestions = result.Data.OrderByDescending(s => s.Confidence).ToList();

            if (Suggestions.Count == 0)
                Snackbar.Add("Secili kategoriler icin oneri bulunamadi.", Severity.Info);
            else
                Snackbar.Add($"{Suggestions.Count} oneri olusturuldu.", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Oneri olusturulamadi: {result.Message}", Severity.Warning);
        }

        IsLoading = false;
    }

    private async Task ApplySuggestion(CategoryAutoMatchSuggestionDto suggestion)
    {
        var searchResult = new MarketplaceCategorySearchResult(
            suggestion.SuggestedMarketPlaceCategoryId,
            suggestion.SuggestedMarketPlaceCategoryName,
            suggestion.SuggestedMarketPlaceCategoryName);

        await OnSuggestionApplied.InvokeAsync((suggestion.ApplicationCategoryId, searchResult));
        Snackbar.Add($"'{suggestion.ApplicationCategoryName}' icin oneri uygulandi.", Severity.Success);
    }

    private async Task ApplyHighConfidenceSuggestions()
    {
        var highConfidence = Suggestions.Where(s => s.Confidence >= 0.8).ToList();

        foreach (var suggestion in highConfidence)
        {
            await ApplySuggestion(suggestion);
        }

        Snackbar.Add($"{highConfidence.Count} yuksek guvenli oneri uygulandi.", Severity.Success);
    }

    private static Color GetConfidenceColor(double confidence) => confidence switch
    {
        >= 0.8 => Color.Success,
        >= 0.5 => Color.Warning,
        _ => Color.Error
    };
}
