using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync;

public partial class AttributeSyncPage
{
    [Inject] private IDbContextFactory<IntegrationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IAttributeMatchManager AttributeMatchManager { get; set; } = null!;
    [Inject] private IMarketplaceCategoryAttributeProvider AttributeProvider { get; set; } = null!;
    [Inject] private IAttributeAutoMatchService AutoMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery] public int? MarketplaceId { get; set; }
    [SupplyParameterFromQuery] public int? CategoryId { get; set; }

    private List<MarketPlace> _marketplaces = [];
    private List<CategoryMarketplace> _matchedCategories = [];
    private List<CategoryAttributeCategory> _appAttributes = [];
    private List<MarketplaceAttributeDto> _marketplaceAttributes = [];
    private Dictionary<int, CategoryAttributeMarketPlaceMatch> _existingAttrMatches = new();
    private Dictionary<int, CategoryAttributeValueMarketPlaceMatch> _existingValueMatches = new();
    private List<CategoryAttributeWithMatchInfo> _attributeListItems = [];
    private List<AttributeMatchSuggestionDto> _suggestions = [];

    private int? _selectedMarketplaceId;
    private int? _selectedCategoryId;
    private int? _selectedAppAttributeId;
    private bool _isLoading;

    private CategoryAttributeWithMatchInfo? _selectedAttributeInfo =>
        _selectedAppAttributeId.HasValue
            ? _attributeListItems.FirstOrDefault(a => a.AttributeId == _selectedAppAttributeId.Value)
            : null;

    protected override async Task OnInitializedAsync()
    {
        await LoadMarketplaces();

        if (MarketplaceId.HasValue)
        {
            await OnMarketplaceChanged(MarketplaceId.Value);
            if (CategoryId.HasValue)
                await OnCategoryChanged(CategoryId.Value);
        }
    }

    private async Task LoadMarketplaces()
    {
        await using var db = await DbContextFactory.CreateDbContextAsync();
        _marketplaces = await db.MarketPlaces.AsNoTracking().ToListAsync();
    }

    private async Task OnMarketplaceChanged(int? marketplaceId)
    {
        _selectedMarketplaceId = marketplaceId;
        _selectedCategoryId = null;
        _selectedAppAttributeId = null;
        _matchedCategories = [];
        _appAttributes = [];
        _marketplaceAttributes = [];
        _existingAttrMatches = new();
        _existingValueMatches = new();
        _attributeListItems = [];
        _suggestions = [];

        if (marketplaceId.HasValue)
        {
            await using var db = await DbContextFactory.CreateDbContextAsync();
            _matchedCategories = await db.CategoryMarketplaces
                .AsNoTracking()
                .Include(cm => cm.Category)
                .Where(cm => cm.MarketPlaceId == marketplaceId.Value && cm.IsActive && !cm.IsDeleted)
                .ToListAsync();
        }
    }

    private async Task OnCategoryChanged(int? categoryId)
    {
        _selectedCategoryId = categoryId;
        _selectedAppAttributeId = null;
        _appAttributes = [];
        _marketplaceAttributes = [];
        _existingAttrMatches = new();
        _existingValueMatches = new();
        _attributeListItems = [];
        _suggestions = [];

        if (categoryId.HasValue && _selectedMarketplaceId.HasValue)
        {
            _isLoading = true;
            StateHasChanged();

            await LoadAttributeData(categoryId.Value, _selectedMarketplaceId.Value);

            _isLoading = false;
        }
    }

    private async Task LoadAttributeData(int categoryId, int marketplaceId)
    {
        // Load app attributes for this category
        await using var db = await DbContextFactory.CreateDbContextAsync();
        _appAttributes = await db.CategoryAttributeCategories
            .AsNoTracking()
            .Include(cac => cac.CategoryAttribute)
                .ThenInclude(ca => ca.CategoryAttributeValues)
            .Where(cac => cac.CategoryId == categoryId && !cac.IsDeleted)
            .ToListAsync();

        // Get marketplace category ID for fetching marketplace attributes
        var categoryMarketplace = _matchedCategories.FirstOrDefault(cm => cm.CategoryId == categoryId);
        if (categoryMarketplace is not null)
        {
            var attrResult = await AttributeProvider.GetAttributesForCategoryAsync(categoryMarketplace.MarketPlaceCategoryId);
            if (attrResult.Success && attrResult.Data is not null)
                _marketplaceAttributes = attrResult.Data;
        }

        // Load existing matches
        var attrIds = _appAttributes.Select(a => a.CategoryAttributeId).ToList();
        var attrMatches = await AttributeMatchManager.GetAttributeMatchesAsync(marketplaceId, attrIds);
        _existingAttrMatches = attrMatches.ToDictionary(m => m.ApplicationCategoryAttributeId);

        var valueIds = _appAttributes
            .SelectMany(a => a.CategoryAttribute?.CategoryAttributeValues ?? [])
            .Select(v => v.Id)
            .ToList();
        if (valueIds.Count > 0)
        {
            var valueMatches = await AttributeMatchManager.GetValueMatchesAsync(marketplaceId, valueIds);
            _existingValueMatches = valueMatches.ToDictionary(m => m.ApplicationCategoryAttributeValueId);
        }

        // Build attribute list items
        BuildAttributeListItems();

        // Auto-suggest via Ollama
        _ = LoadSuggestionsAsync();
    }

    private void BuildAttributeListItems()
    {
        _attributeListItems = _appAttributes
            .Where(cac => cac.CategoryAttribute is not null)
            .Select(cac =>
            {
                var attr = cac.CategoryAttribute;
                _existingAttrMatches.TryGetValue(attr.Id, out var match);

                string? matchedMpName = null;
                if (match is not null)
                {
                    var mpAttr = _marketplaceAttributes.FirstOrDefault(a => a.Id == match.MarketPlaceCategoryAttributeId);
                    matchedMpName = mpAttr?.Name;
                }

                var values = attr.CategoryAttributeValues?.Select(v =>
                {
                    _existingValueMatches.TryGetValue(v.Id, out var vm);
                    string? matchedValueName = null;
                    if (vm is not null)
                    {
                        var mpValue = _marketplaceAttributes
                            .SelectMany(a => a.Values)
                            .FirstOrDefault(val => val.Id == vm.MarketPlaceCategoryAttributeValueId);
                        matchedValueName = mpValue?.Name;
                    }
                    return new AttributeValueInfo(v.Id, v.Name ?? v.Id.ToString(), vm is not null, matchedValueName);
                }).ToList() ?? [];

                return new CategoryAttributeWithMatchInfo(
                    attr.Id,
                    attr.CategoryAttributeHumanized ?? attr.CategoryAttributeKey ?? attr.Id.ToString(),
                    cac.IsRequired,
                    cac.IsVarianter,
                    cac.IsSlicer,
                    match is not null,
                    matchedMpName,
                    match?.MarketPlaceCategoryAttributeId,
                    values);
            })
            .ToList();
    }

    private async Task LoadSuggestionsAsync()
    {
        if (!_appAttributes.Any() || !_marketplaceAttributes.Any()) return;

        var unmatchedAppAttrs = _appAttributes
            .Where(cac => cac.CategoryAttribute is not null && !_existingAttrMatches.ContainsKey(cac.CategoryAttributeId))
            .Select(cac => new AppAttributeForMatchDto(
                cac.CategoryAttributeId,
                cac.CategoryAttribute.CategoryAttributeHumanized ?? cac.CategoryAttribute.CategoryAttributeKey ?? cac.CategoryAttributeId.ToString()))
            .ToList();

        if (!unmatchedAppAttrs.Any()) return;

        var result = await AutoMatchService.SuggestAttributeMatchesAsync(unmatchedAppAttrs, _marketplaceAttributes);
        if (result.Success && result.Data is not null)
        {
            _suggestions = result.Data;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnAttributeSelected(int? attributeId)
    {
        _selectedAppAttributeId = attributeId;
    }

    private async Task OnMatchConfirmed((int appAttrId, int mpAttrId) args)
    {
        var result = await AttributeMatchManager.SaveAttributeMatchAsync(
            args.appAttrId, _selectedMarketplaceId!.Value, args.mpAttrId);

        if (result.Success)
        {
            Snackbar.Add("Eşleştirme kaydedildi.", Severity.Success);
            await ReloadMatches();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private async Task OnRemoveMatch(int appAttrId)
    {
        var result = await AttributeMatchManager.RemoveAttributeMatchAsync(appAttrId, _selectedMarketplaceId!.Value);

        if (result.Success)
        {
            Snackbar.Add("Eşleştirme kaldırıldı.", Severity.Info);
            await ReloadMatches();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private async Task OnValueMatchChanged()
    {
        await ReloadMatches();
    }

    private async Task ReloadMatches()
    {
        if (!_selectedCategoryId.HasValue || !_selectedMarketplaceId.HasValue) return;

        var attrIds = _appAttributes.Select(a => a.CategoryAttributeId).ToList();
        var attrMatches = await AttributeMatchManager.GetAttributeMatchesAsync(_selectedMarketplaceId.Value, attrIds);
        _existingAttrMatches = attrMatches.ToDictionary(m => m.ApplicationCategoryAttributeId);

        var valueIds = _appAttributes
            .SelectMany(a => a.CategoryAttribute?.CategoryAttributeValues ?? [])
            .Select(v => v.Id)
            .ToList();
        if (valueIds.Count > 0)
        {
            var valueMatches = await AttributeMatchManager.GetValueMatchesAsync(_selectedMarketplaceId.Value, valueIds);
            _existingValueMatches = valueMatches.ToDictionary(m => m.ApplicationCategoryAttributeValueId);
        }

        BuildAttributeListItems();
    }
}
