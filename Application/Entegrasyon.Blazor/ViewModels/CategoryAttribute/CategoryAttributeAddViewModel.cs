using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Blazor.Utility.Attributes;
using Entegrasyon.Blazor.Utility.Attributes.Validations;

namespace Entegrasyon.Blazor.ViewModels.CategoryAttribute;

public class CategoryAttributeAddViewModel
{
    public int? CategoryId { get; set; }
    [EnsureAtLeastOne("Lütfen en az bir tane özellik girin.")]
    public List<CategoryAttributeCreateViewModel> CategoryAttributeList { get; set; } = new();

}

public class CategoryAttributeCreateViewModel
{
    public int? Id { get; set; }
    public bool IsAddedAfterward { get; set; }
    public bool IsExistingAdding { get; set; }

    [Display(Name = "Zorunlu mu?", Description = "Bu kategoriden bir ürün oluştururken zorunlu olan değer.")]
    public bool IsRequired { get; set; }

    [Display(Name = "Düz yazı", Description = "Bu seçenek işaretlendiğinde, değer yazılarak girilir.")]
    public bool AllowCustom { get; set; }

    [ExclusiveBoolean(nameof(IsSlicer), ErrorMessage = "İki özellik de aynı anda aktif olamaz.")]
    [Display(Name = "Varyant")]
    public bool IsVarianter { get; set; }

    [Display(Name = "Sayfa Bölücü")]
    [ExclusiveBoolean(nameof(IsVarianter), ErrorMessage = "İki özellik de aynı anda aktif olamaz.")]
    public bool IsSlicer { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Lütfen özellik için bir isim girin.")]
    [MaxLength(55, ErrorMessage = "Özellik ismi 55 karakterden fazla olamaz.")]
    [MinLength(2, ErrorMessage = "Özellik ismi 2 karakterden az olamaz.")]
    [Display(Name = "Kategori Özellik İsmi", Prompt = "Kategori İsmi")]
    public string CategoryAttributeKey { get; set; }
    [Display(Name = "Değerler")]
    public string CustomValues { get; set; }
    public string FormUniqueId { get; set; }
    public List<CategoryAttributeValueViewModel> CategoryAttributeValues { get; set; } = new();
}
public sealed record TagifyValue(int id, string value);