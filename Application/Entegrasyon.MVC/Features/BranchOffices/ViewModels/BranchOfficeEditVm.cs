using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.BranchOffices.ViewModels;

public class BranchOfficeEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Sube adi zorunludur.")]
    public string Name { get; set; } = "";
}
