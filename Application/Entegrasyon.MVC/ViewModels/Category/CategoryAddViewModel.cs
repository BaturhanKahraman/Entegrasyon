using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.Category;

public class CategoryAddViewModel
{
    [Display(Name="Kategori İsmi")]
    [StringLength(45,MinimumLength =2,ErrorMessage ="Lütfen en az 2 en fazla 45 karakter girin.")]
    [Required(AllowEmptyStrings=false,ErrorMessage ="Bu alan gereklidir. Boşluklardan oluşamaz.")]
    public string Name { get; set; }
    public List<CategoryAttributeAddViewModel> CategoryAttributes { get; set; } = new();
    public int? SuperCategoryId { get; set; }
    public bool IsFavorite { get; set; } = false;

}