using Entegrasyon.Entity.Dtos.MasterCatalog;

namespace Entegrasyon.MVC.Features.Categories.ViewModels;

public class MasterImportVm
{
    public List<MasterCategoryTreeDto> Categories { get; set; } = [];
    public List<SectorPackageDto> SectorPackages { get; set; } = [];
    public int TotalCategoryCount { get; set; }
    public int LeafCategoryCount { get; set; }
}

public class MasterImportResultVm
{
    public int CategoriesImported { get; set; }
    public int AttributesImported { get; set; }
    public int ValuesImported { get; set; }
    public int MappingsImported { get; set; }
    public int CategoriesSkipped { get; set; }
    public int AttributesSkipped { get; set; }
    public int ValuesSkipped { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
