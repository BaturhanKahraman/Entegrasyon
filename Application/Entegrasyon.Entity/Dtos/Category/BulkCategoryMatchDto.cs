namespace Entegrasyon.Entity.Dtos.Category;

public record BulkCategoryMatchDto
{
    public int MarketPlaceId { get; set; }
    public List<BulkCategoryMatchItemDto> Items { get; set; } = [];
}

public record BulkCategoryMatchItemDto
{
    public int ApplicationCategoryId { get; set; }
    public int MarketPlaceCategoryId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
}

public record BulkCategoryMatchResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<BulkCategoryMatchErrorDto> Errors { get; set; } = [];
}

public record BulkCategoryMatchErrorDto
{
    public int ApplicationCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
