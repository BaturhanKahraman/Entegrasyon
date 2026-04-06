namespace Entegrasyon.MVC.Features.Brands.ViewModels;

public class BrandMasterImportResultVm
{
    public bool Success { get; set; }
    public int BrandsImported { get; set; }
    public int BrandsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
}
