namespace Entegrasyon.Entity.Dtos.Brand;

public record BrandAutoMatchResultDto
{
    public int AutoMatchedCount { get; init; }
    public int SuggestionCount { get; init; }
    public int FailedCount { get; init; }
    public List<BrandAutoMatchSuggestionDto> Suggestions { get; init; } = [];
}

public record BrandAutoMatchSuggestionDto
{
    public int ApplicationBrandId { get; init; }
    public string ApplicationBrandName { get; init; } = string.Empty;
    public int MarketPlaceBrandId { get; init; }
    public string MarketPlaceBrandName { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}
