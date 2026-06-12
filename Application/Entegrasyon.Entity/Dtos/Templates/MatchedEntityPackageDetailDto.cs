using Entegrasyon.Entity.Templates;

namespace Entegrasyon.Entity.Dtos.Templates;

/// <summary>
/// Paket detayı — tüm hiyerarşi, attribute'lar, value'lar ve marketplace mapping'ler dahil.
/// </summary>
public record MatchedEntityPackageDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public MatchedEntityType EntityType { get; init; }
    public int Version { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    // EntityType'a göre sadece biri dolu olur
    public List<TemplateCategoryDetailDto> Categories { get; init; } = [];
    public List<TemplateBrandDetailDto> Brands { get; init; } = [];
    public List<TemplateCargoCompanyDetailDto> CargoCompanies { get; init; } = [];
}

public record TemplateCategoryDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public decimal? DefaultVatRate { get; init; }
    public List<TemplateCategoryDetailDto> Children { get; init; } = [];
    public List<MarketplaceMappingDto> MarketplaceMappings { get; init; } = [];
    public List<TemplateAttributeDetailDto> Attributes { get; init; } = [];
}

public record TemplateAttributeDetailDto
{
    public int Id { get; init; }
    public string AttributeKey { get; init; } = null!;
    public string? AttributeHumanized { get; init; }
    public bool IsRequired { get; init; }
    public bool IsSlicer { get; init; }
    public bool IsVarianter { get; init; }
    public List<MarketplaceMappingDto> MarketplaceMappings { get; init; } = [];
    public List<TemplateAttributeValueDetailDto> Values { get; init; } = [];
}

public record TemplateAttributeValueDetailDto
{
    public int Id { get; init; }
    public string ValueName { get; init; } = null!;
    public List<MarketplaceMappingDto> MarketplaceMappings { get; init; } = [];
}

public record TemplateBrandDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public List<MarketplaceMappingDto> MarketplaceMappings { get; init; } = [];
}

public record TemplateCargoCompanyDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public List<MarketplaceMappingDto> MarketplaceMappings { get; init; } = [];
}

/// <summary>
/// Tüm template entity türleri için ortak marketplace mapping DTO.
/// </summary>
public record MarketplaceMappingDto
{
    public int MarketPlaceId { get; init; }
    public string? MarketPlaceName { get; init; }
    public int ExternalId { get; init; }
    public string? ExternalStringId { get; init; }
    public string? ExternalName { get; init; }
}
