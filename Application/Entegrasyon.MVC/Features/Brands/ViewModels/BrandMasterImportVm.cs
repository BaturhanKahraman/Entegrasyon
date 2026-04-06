using Entegrasyon.Entity.Dtos.MasterCatalog;

namespace Entegrasyon.MVC.Features.Brands.ViewModels;

public class BrandMasterImportVm
{
    public IList<MasterBrandDto> Brands { get; set; } = [];
    public int TotalCount { get; set; }
}
