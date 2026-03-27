using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch;

public partial class BulkCategoryMatchPage
{
    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<MarketplaceOption> _marketplaces = [];
    private MarketplaceOption? _selectedMarketplace;
    private List<Category> _unmappedCategories = [];
    private HashSet<Category> _selectedCategories = [];
    private Dictionary<int, MarketplaceCategorySearchResult?> _mappingAssignments = new();
    private bool _isLoadingMarketplaces = true;
    private bool _isLoadingCategories;
    private bool _isSubmitting;

    private bool HasValidMappings => _mappingAssignments.Any(m => m.Value is not null);
    private int ReadyCount => _mappingAssignments.Count(m => m.Value is not null);

    protected override async Task OnInitializedAsync()
    {
        await LoadMarketplaces();
    }

    private async Task LoadMarketplaces()
    {
        _isLoadingMarketplaces = true;
        var result = await MarketPlaceManager.GetAllAsync();
        if (result.Success && result.Data is not null)
            _marketplaces = result.Data.Select(mp => new MarketplaceOption(mp.Id, mp.Name)).ToList();
        _isLoadingMarketplaces = false;
    }

    private async Task OnMarketplaceChanged(MarketplaceOption? marketplace)
    {
        _selectedMarketplace = marketplace;
        _selectedCategories.Clear();
        _mappingAssignments.Clear();

        if (marketplace is not null)
            await LoadUnmappedCategories(marketplace.Id);
    }

    private async Task LoadUnmappedCategories(int marketPlaceId)
    {
        _isLoadingCategories = true;

        var allCategories = await CategoryService.GetAllCategoriesWithHierarchyAsync();
        var existingMappings = await CategoryMatchService.GetAllCategoryMappingsAsync(marketPlaceId);
        var mappedCategoryIds = existingMappings.Select(m => m.ApplicationCategoryId).ToHashSet();

        _unmappedCategories = allCategories
            .Where(c => !mappedCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToList();

        _isLoadingCategories = false;
    }

    private void OnSelectionChanged(HashSet<Category> selection)
    {
        _selectedCategories = selection;

        // Remove mappings for deselected categories
        var selectedIds = selection.Select(c => c.Id).ToHashSet();
        var toRemove = _mappingAssignments.Keys.Where(k => !selectedIds.Contains(k)).ToList();
        foreach (var key in toRemove)
            _mappingAssignments.Remove(key);

        // Add empty mapping for newly selected
        foreach (var cat in selection)
        {
            _mappingAssignments.TryAdd(cat.Id, null);
        }
    }

    private void OnMappingChanged((int CategoryId, MarketplaceCategorySearchResult? Result) mapping)
    {
        _mappingAssignments[mapping.CategoryId] = mapping.Result;
    }

    private void OnSuggestionApplied((int CategoryId, MarketplaceCategorySearchResult Result) suggestion)
    {
        _mappingAssignments[suggestion.CategoryId] = suggestion.Result;
        StateHasChanged();
    }

    private async Task SubmitBulkMapping()
    {
        if (_selectedMarketplace is null) return;

        var items = _mappingAssignments
            .Where(m => m.Value is not null)
            .Select(m => new BulkCategoryMatchItemDto
            {
                ApplicationCategoryId = m.Key,
                MarketPlaceCategoryId = m.Value!.Id,
                MarketPlaceCategoryName = m.Value.FullPath ?? m.Value.Name
            })
            .ToList();

        if (items.Count == 0) return;

        _isSubmitting = true;

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = _selectedMarketplace.Id,
            Items = items
        };

        var result = await CategoryMatchService.BulkCreateCategoryMappingsAsync(dto);

        if (result.Success)
        {
            var data = result.Data;
            var parameters = new DialogParameters<BulkMatchProgressDialog>
            {
                { x => x.Result, data }
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<BulkMatchProgressDialog>("Eşleştirme Sonucu", parameters, options);

            // Refresh the unmapped list
            _selectedCategories.Clear();
            _mappingAssignments.Clear();
            await LoadUnmappedCategories(_selectedMarketplace.Id);
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }

        _isSubmitting = false;
    }
}
