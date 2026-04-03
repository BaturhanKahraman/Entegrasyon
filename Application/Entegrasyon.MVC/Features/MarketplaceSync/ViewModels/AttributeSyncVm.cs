using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class AttributeSyncVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public List<AttributeListItemVm> Attributes { get; set; } = [];
}

public class AttributeListItemVm
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Humanized { get; set; }
    public bool IsMapped { get; set; }
    public int? MarketPlaceAttributeId { get; set; }
}

public class AttributeMatchPanelVm
{
    public int AttributeId { get; set; }
    public string AttributeKey { get; set; } = string.Empty;
    public string? AttributeHumanized { get; set; }
    public int MarketPlaceId { get; set; }
    public CategoryAttributeMarketPlaceMatch? AttributeMatch { get; set; }
    public List<AttributeValueMatchItemVm> ValueMatches { get; set; } = [];
}

public class AttributeValueMatchItemVm
{
    public int ValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public bool IsMapped { get; set; }
    public int? MarketPlaceValueId { get; set; }
    public string? MarketPlaceValueExternalId { get; set; }
}
