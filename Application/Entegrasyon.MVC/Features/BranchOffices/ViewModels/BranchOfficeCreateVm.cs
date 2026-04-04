using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.BranchOffices.ViewModels;

public class BranchOfficeCreateVm
{
    [Required(ErrorMessage = "Sube adi zorunludur.")]
    public string Name { get; set; } = "";
}
