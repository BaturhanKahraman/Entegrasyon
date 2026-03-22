namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryMatchSummaryDto
{
    public int TotalCategories { get; set; }
    public int MappedCategories { get; set; }
    public int UnmappedCategories { get; set; }
}
