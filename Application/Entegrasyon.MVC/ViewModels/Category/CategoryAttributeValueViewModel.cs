
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.Category;

public sealed record CategoryAttributeValueViewModel(
    int? Id,
    [property:Required(AllowEmptyStrings =false,ErrorMessage ="Lütfen isim alanına bir değer girin.")]
    [property:MaxLength(55,ErrorMessage ="Lütfen en fazla 55 karakter girin.")]
    [property:MinLength(1,ErrorMessage ="Özellik ismi 1 karakterden az olamaz.")]
    [property:Display(Name ="Kategori Özellik Değeri İsmi")]
    string Name
    );
