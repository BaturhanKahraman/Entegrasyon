using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.BranchOffices.ViewModels;

public class BranchOfficeEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Depo adi zorunludur.")]
    public string Name { get; set; } = "";

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    public string? Address { get; set; }
}
