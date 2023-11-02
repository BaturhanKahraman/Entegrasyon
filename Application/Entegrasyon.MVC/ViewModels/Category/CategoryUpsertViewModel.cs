using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.Category;

public sealed class CategoryUpsertViewModel
{
    public int Id { get; set; }
    [Display(Name = "Kategori İsmi")]
    [StringLength(45, MinimumLength = 2, ErrorMessage = "Lütfen en az 2 en fazla 45 karakter girin.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "Bu alan gereklidir. Boşluklardan oluşamaz.")]
    public string Name { get; set; }
    [Display(Name="Üst Kategori")]
    public int? SuperCategoryId { get; set; }
    [Display(Name = "Favori Durumu", Description = "Favori mi ?")]
    public bool IsFavorite { get; set; } = false;
    public List<SelectListItem> SuperCategories { get; set; } = new();
}
