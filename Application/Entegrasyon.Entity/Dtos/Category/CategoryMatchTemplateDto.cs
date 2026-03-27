namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryMatchTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MarketPlaceId { get; set; }
    public int MappingCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record SaveCategoryMatchTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MarketPlaceId { get; set; }
}

public record CategoryMatchTemplateDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MarketPlaceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<CategoryMatchTemplateMappingDto> Mappings { get; set; } = [];
}

public record CategoryMatchTemplateMappingDto
{
    public int ApplicationCategoryId { get; set; }
    public string ApplicationCategoryName { get; set; } = string.Empty;
    public int MarketPlaceCategoryId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
}
