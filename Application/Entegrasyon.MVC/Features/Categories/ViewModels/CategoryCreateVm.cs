namespace Entegrasyon.MVC.Features.Categories.ViewModels;

public class CategoryCreateVm
{
    public string Name { get; set; } = "";
    public int? SuperCategoryId { get; set; }
    public string? SuperCategoryName { get; set; }
    public bool IsFavorite { get; set; }
    public decimal? DefaultVatRate { get; set; }
    public List<CategoryAttributeSelectionVm> Attributes { get; set; } = [];
}

public class CategoryAttributeSelectionVm
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public bool Selected { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}

public class CategoryCreateSuccessVm
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public bool HasAttributes { get; set; }
}
