using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync;

public partial class AttributeMatchPanel
{
    [Parameter] public CategoryAttributeWithMatchInfo? SelectedAttribute { get; set; }
    [Parameter] public List<MarketplaceAttributeDto> MarketplaceAttributes { get; set; } = [];
    [Parameter] public List<AttributeMatchSuggestionDto> Suggestions { get; set; } = [];
    [Parameter] public Dictionary<int, CategoryAttributeMarketPlaceMatch> AllExistingMatches { get; set; } = new();
    [Parameter] public EventCallback<(int appAttrId, int mpAttrId)> OnMatchConfirmed { get; set; }

    private string _searchText = string.Empty;
    private int? _selectedMpAttributeId;

    private IEnumerable<MarketplaceAttributeDto> FilteredMarketplaceAttributes =>
        string.IsNullOrWhiteSpace(_searchText)
            ? MarketplaceAttributes
            : MarketplaceAttributes.Where(a =>
                a.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    protected override void OnParametersSet()
    {
        // Reset selection when attribute changes
        _selectedMpAttributeId = null;
        _searchText = string.Empty;
    }

    private bool IsMarketplaceAttrUsedByOther(int mpAttrId, int currentAppAttrId)
    {
        return AllExistingMatches.Any(kvp =>
            kvp.Value.MarketPlaceCategoryAttributeId == mpAttrId &&
            kvp.Key != currentAppAttrId);
    }

    private void SelectSuggestion(int mpAttrId)
    {
        _selectedMpAttributeId = mpAttrId;
    }

    private async Task ConfirmMatch()
    {
        if (SelectedAttribute is null || _selectedMpAttributeId is null) return;
        await OnMatchConfirmed.InvokeAsync((SelectedAttribute.AttributeId, _selectedMpAttributeId.Value));
        _selectedMpAttributeId = null;
    }

    private Color GetConfidenceColor(double confidence) => confidence switch
    {
        >= 0.8 => Color.Success,
        >= 0.5 => Color.Warning,
        _ => Color.Default
    };
}
