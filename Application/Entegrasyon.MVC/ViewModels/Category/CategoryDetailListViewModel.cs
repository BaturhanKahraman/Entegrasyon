namespace Entegrasyon.MVC.ViewModels.Category;

public sealed record CategoryDetailListViewModel(
    int Id,
    int TotalProductCount,
    string Name,
    string SuperCategoryName,
    int SubCategoryCount,
    bool IsFavorited,
    int AttributeCount)
{
    public bool HasChild => SubCategoryCount > 0;
    public bool IsChild => !string.IsNullOrEmpty(SuperCategoryName);
}
