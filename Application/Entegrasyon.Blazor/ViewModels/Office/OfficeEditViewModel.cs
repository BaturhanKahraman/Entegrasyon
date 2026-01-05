using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.Office;

public class OfficeEditViewModel
{
    public int Id { get; set; }
    [Display(Name = "Ofis İsmi")]
    [Required(ErrorMessage = "Ofis ismi girmek zorunludur.")]
    [MinLength(3,ErrorMessage = "En az 3 karakter giriniz.")]
    public string? Name { get; set; }
}
