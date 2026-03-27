namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryAutoMatchSuggestionDto
{
    public int ApplicationCategoryId { get; set; }
    public string ApplicationCategoryName { get; set; } = string.Empty;
    public int SuggestedMarketPlaceCategoryId { get; set; }
    public string SuggestedMarketPlaceCategoryName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public record CategoryAutoMatchRequestDto
{
    public int MarketPlaceId { get; set; }
    public List<CategoryAutoMatchItemDto> Categories { get; set; } = [];
}

public record CategoryAutoMatchItemDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ParentCategoryName { get; set; }
}
