using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync;

public record CategoryAttributeWithMatchInfo(
    int AttributeId,
    string AttributeName,
    bool IsRequired,
    bool IsVarianter,
    bool IsSlicer,
    bool IsMatched,
    string? MatchedMarketplaceName,
    int? MatchedMarketplaceAttributeId,
    List<AttributeValueInfo> Values);

public record AttributeValueInfo(
    int ValueId,
    string ValueName,
    bool IsMatched,
    string? MatchedMarketplaceValueName);

public partial class AttributeListPanel
{
    [Parameter] public List<CategoryAttributeWithMatchInfo> Attributes { get; set; } = [];
    [Parameter] public int? SelectedAttributeId { get; set; }
    [Parameter] public EventCallback<int?> SelectedAttributeIdChanged { get; set; }
    [Parameter] public EventCallback<int> OnRemoveMatch { get; set; }
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public Dictionary<int, CategoryAttributeValueMarketPlaceMatch> ExistingValueMatches { get; set; } = new();
    [Parameter] public EventCallback OnValueMatchChanged { get; set; }

    // Injected from parent via cascading or from page — marketplace attributes lookup
    [Parameter] public List<MarketplaceAttributeDto> MarketplaceAttributes { get; set; } = [];

    private async Task SelectAttribute(int attributeId)
    {
        var newId = SelectedAttributeId == attributeId ? (int?)null : attributeId;
        await SelectedAttributeIdChanged.InvokeAsync(newId);
    }

    private async Task RemoveMatch(int attributeId)
    {
        await OnRemoveMatch.InvokeAsync(attributeId);
    }

    private List<MarketplaceAttributeValueDto> GetMarketplaceValuesForAttr(CategoryAttributeWithMatchInfo attr)
    {
        if (attr.MatchedMarketplaceAttributeId is null) return [];
        var mpAttr = MarketplaceAttributes.FirstOrDefault(a => a.Id == attr.MatchedMarketplaceAttributeId.Value);
        return mpAttr?.Values ?? [];
    }
}
