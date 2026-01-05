using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity;

namespace Entegrasyon.Blazor.ViewModels.Office;

public class OfficeCreateViewModel
{
    [Display(Name = "Ofis İsmi")]
    [Required(ErrorMessage = "Ofis ismi girmek zorunludur.")]
    [MinLength(3,ErrorMessage = "En az 3 karakter giriniz.")]
    public string? Name { get; set; }
}
