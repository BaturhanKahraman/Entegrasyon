namespace Entegrasyon.Blazor.ViewModels.Category;

public class CategoryDetailViewModel
{
    public int Id { get; set; }
    public int TotalProductCount { get; set; }
    public string? Name { get; set; }
    public int SubCategoryCount { get; set; }
    public bool IsFavorited { get; set; }
    public int AttributeCount { get; set; }
}
