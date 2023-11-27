namespace Entegrasyon.MVC.ViewModels.Category;

public class CategoryDetailListViewModel
{
    public int Id { get; set; }
    public int TotalProductCount { get; set; }
    public string Name { get; set; }
    public string SuperCategoryName { get; set; }
    public int SubCategoryCount { get; set; }
    public bool IsFavorited { get; set; }
    public int AttributeCount { get; set; }

    public bool HasChild => SubCategoryCount>0;
    public bool IsChild => !string.IsNullOrEmpty(SuperCategoryName);
}   