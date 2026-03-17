
namespace Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

public class TrendyolSelectedCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public List<TrendyolSelectedCategory> SubCategories { get; set; } = [];
    public bool IsParent => SubCategories.Count != 0;
}
/*
 *   id: number;
  name: string;
  parentId: number;
 */