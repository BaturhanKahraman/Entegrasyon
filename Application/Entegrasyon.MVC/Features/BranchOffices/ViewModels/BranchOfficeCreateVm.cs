using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.BranchOffices.ViewModels;

public class BranchOfficeCreateVm
{
    [Required(ErrorMessage = "Sube adi zorunludur.")]
    public string Name { get; set; } = "";

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    public string? Address { get; set; }
}
