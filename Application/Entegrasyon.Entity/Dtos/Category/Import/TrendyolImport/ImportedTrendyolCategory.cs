namespace Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

public class RootTrendyolCategory
{
    public IEnumerable<ImportedTrendyolCategory> Categories { get; set; }
}
public class ImportedTrendyolCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public IEnumerable<ImportedTrendyolCategory> SubCategories { get; set; }

}