namespace Entegrasyon.MVC.ViewModels.Category;

public class CategoryAddViewModel
{
    public string Name { get; set; }
    public List<CategoryAttributeAddViewModel> CategoryAttributes { get; set; } = new();
    public int? SuperCategoryId { get; set; }
    public bool IsFavorite { get; set; }

}