using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class EditProductVm
{
    [Required(ErrorMessage = "Urun adi zorunludur.")]
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? StockCode { get; set; }
    public string? Season { get; set; }
    public string? Year { get; set; }

    [Required(ErrorMessage = "Marka secimi zorunludur.")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Kategori secimi zorunludur.")]
    public int CategoryId { get; set; }
}
