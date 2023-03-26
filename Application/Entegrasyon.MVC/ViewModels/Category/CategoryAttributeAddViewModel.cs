using System.Text;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.MVC.ViewModels.Category;

public class CategoryAttributeAddViewModel
{
    public int Id { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowCustom { get; set; }
    public bool IsVarianter { get; set; }
    public string CategoryAttributeKey { get; set; }
    public bool IsSlicer { get; set; }
    public string CategoryAttributeHumanized { get; set; }
    public string CategoryAttributeValues { get; set; }
    //public List<CategoryAttributeValue> CategoryAttributeValues { get; set; }
}