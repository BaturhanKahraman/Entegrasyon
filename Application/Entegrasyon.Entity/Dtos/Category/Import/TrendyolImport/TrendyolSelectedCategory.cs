
namespace Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

public class TrendyolSelectedCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public List<TrendyolSelectedCategory> SubCategories { get; set; } = new();
    public bool IsParent => SubCategories.Any();
}
/*
 *   id: number;
  name: string;
  parentId: number;
 */