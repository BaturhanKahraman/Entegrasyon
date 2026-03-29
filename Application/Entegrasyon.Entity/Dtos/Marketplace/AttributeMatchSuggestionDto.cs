namespace Entegrasyon.Entity.Dtos.Marketplace;

public record AttributeMatchSuggestionDto(
    int ApplicationAttributeId,
    string ApplicationAttributeName,
    int SuggestedMarketplaceAttributeId,
    string SuggestedMarketplaceAttributeName,
    double Confidence);

public record ValueMatchSuggestionDto(
    int ApplicationValueId,
    string ApplicationValueName,
    int SuggestedMarketplaceValueId,
    string SuggestedMarketplaceValueName,
    double Confidence);
