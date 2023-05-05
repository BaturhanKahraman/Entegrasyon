using System.ComponentModel.DataAnnotations;
using System.Text;
using Entegrasyon.Entity.Categories;
using Entegrasyon.MVC.Utility.Attributes;

namespace Entegrasyon.MVC.ViewModels.Category;

public class CategoryAttributeAddViewModel
{
    public int? CategoryId { get; set; }
    public List<CategoryAttributeCreateViewModel> CategoryAttributeList { get; set; }

}

public class CategoryAttributeCreateViewModel
{
    public int? Id { get; set; }
    [Display(Name = "Zorunlu mu?",Description = "Bu kategoriden bir ürün oluştururken zorunlu olan değer.")]
    public bool IsRequired { get; set; } 
    [Display(Name = "Düz yazıya izin ver.",Description = "Bu seçenek işaretlendiğinde, değer yazılarak girilir.")]
    public bool AllowCustom { get; set; }
    [ExclusiveBoolean(nameof(IsSlicer),ErrorMessage = "İki özellik de aynı anda aktif olamaz.")]
    [Display(Name = "Varyant")]
    public bool IsVarianter { get; set; } 
    [Display(Name = "Sayfa Bölücü")]
    public bool IsSlicer { get; set; } 
    [Required(AllowEmptyStrings = false,ErrorMessage = "Lütfen özellik için bir isim girin.")]
    [MaxLength(55,ErrorMessage = "Özellik ismi 55 karakterden fazla olamaz.")]
    [MinLength(2,ErrorMessage = "Özellik ismi 2 karakterden az olamaz.")]
    [Display(Name = "Kategori Özellik İsmi",Prompt ="Kategori İsmi")]
    public string CategoryAttributeKey { get; set; }
    public List<CategoryAttributeValueViewModel> CategoryAttributeValues { get; set; } = new();
}