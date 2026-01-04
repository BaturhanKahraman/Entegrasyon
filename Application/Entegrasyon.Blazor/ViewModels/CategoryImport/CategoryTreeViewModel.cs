using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

namespace Entegrasyon.Blazor.ViewModels.CategoryImport;

public class CategoryTreeViewModel
{
    public List<ImportedTrendyolCategory> TrendyolCategories { get; set; }
    public string SelectedCategories { get; set; }
}