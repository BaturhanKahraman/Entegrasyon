using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardMarketplaceStep
{
    /// <summary>
    /// 0 means "new category" (no existing mappings to load).
    /// </summary>
    [Parameter] public int CategoryId { get; set; }
    [Parameter] public string? CategoryName { get; set; }

    [Parameter] public int? SelectedMarketplaceId { get; set; }
    [Parameter] public EventCallback<int?> SelectedMarketplaceIdChanged { get; set; }

    [Parameter] public EventCallback OnFieldChanged { get; set; }

    [Inject] private IMarketplaceSearchService MarketplaceSearchService { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private IDbContextFactory<IntegrationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<MarketPlace> _marketplaces = [];
    private List<CategoryMarketplaceMappingDto> _existingMappings = [];
    private List<MarketplaceCategorySearchResult> _searchResults = [];
    private MarketplaceCategorySearchResult? _selectedResult;

    private string _searchText = string.Empty;
    private bool _searching;
    private bool _searched;

    public bool HasMapping => _selectedResult is not null;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        _marketplaces = await context.MarketPlaces
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync();

        if (CategoryId > 0)
        {
            // Load existing mappings for all marketplaces for this category
            foreach (var mp in _marketplaces)
            {
                var mappings = await CategoryMatchService.GetAllCategoryMappingsAsync(mp.Id);
                _existingMappings.AddRange(mappings.Where(m => m.ApplicationCategoryId == CategoryId));
            }
        }
    }

    private async Task OnMarketplaceChanged(int? value)
    {
        SelectedMarketplaceId = value;
        await SelectedMarketplaceIdChanged.InvokeAsync(value);
        await OnFieldChanged.InvokeAsync();

        // Reset search state when marketplace changes
        _searchText = string.Empty;
        _searchResults = [];
        _selectedResult = null;
        _searched = false;
    }

    private async Task SearchCategories()
    {
        if (!SelectedMarketplaceId.HasValue || string.IsNullOrWhiteSpace(_searchText))
            return;

        _searching = true;
        _searched = false;
        _searchResults = [];

        try
        {
            var result = await MarketplaceSearchService.SearchCategoriesAsync(SelectedMarketplaceId.Value, _searchText);
            if (result.Success && result.Data is not null)
                _searchResults = result.Data;
            else
                Snackbar.Add(result.Message ?? "Arama başarısız", Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Arama hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _searching = false;
            _searched = true;
        }
    }

    private void SelectCategory(MarketplaceCategorySearchResult result)
    {
        _selectedResult = _selectedResult?.Id == result.Id ? null : result;
        OnFieldChanged.InvokeAsync();
    }

    public CreateCategoryMarketplaceMatchDto? GetMappingDto()
    {
        if (!SelectedMarketplaceId.HasValue || _selectedResult is null)
            return null;

        return new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = CategoryId,
            MarketPlaceId = SelectedMarketplaceId.Value,
            MarketPlaceCategoryId = _selectedResult.Id,
            MarketPlaceCategoryName = _selectedResult.Name
        };
    }
}
