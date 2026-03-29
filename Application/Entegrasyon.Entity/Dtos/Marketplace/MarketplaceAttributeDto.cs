namespace Entegrasyon.Entity.Dtos.Marketplace;

public record MarketplaceAttributeDto(
    int Id,
    string Name,
    bool IsRequired,
    bool AllowCustom,
    List<MarketplaceAttributeValueDto> Values);

public record MarketplaceAttributeValueDto(
    int Id,
    string Name);
