using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Help;

namespace Entegrasyon.MVC.Features.Help.ViewModels;

public class CreateHelpRequestVm
{
    [Display(Name = "Kategori")]
    public HelpRequestCategory Category { get; set; } = HelpRequestCategory.Bug;

    [Required(ErrorMessage = "Konu boş olamaz.")]
    [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
    [Display(Name = "Konu")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj boş olamaz.")]
    [StringLength(4000, ErrorMessage = "Mesaj en fazla 4000 karakter olabilir.")]
    [Display(Name = "Mesaj")]
    public string Message { get; set; } = string.Empty;
}
