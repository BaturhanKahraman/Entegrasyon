namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryMatchValidationResultDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int TotalRequiredAttributes { get; set; }
    public int MappedRequiredAttributes { get; set; }
    public int TotalVarianterAttributes { get; set; }
    public int MappedVarianterAttributes { get; set; }
    public bool HasCategoryMapping { get; set; }
    public bool HasBrandMapping { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
