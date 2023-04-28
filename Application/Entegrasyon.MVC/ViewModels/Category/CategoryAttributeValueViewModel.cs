
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.Category;

public sealed record CategoryAttributeValueViewModel(
    int? Id,
    [property:Required(AllowEmptyStrings =false,ErrorMessage ="Lütfen isim alanına bir değer girin.")]
    [property:MaxLength(55,ErrorMessage ="Lütfen en fazla 55 karakter girin.")]
    [property:MinLength(2,ErrorMessage ="Özellik ismi 2 karakterden az olamaz.")]
    [property:Display(Name ="Kategori Özellik Değeri İsmi")]
    string Name
    );
