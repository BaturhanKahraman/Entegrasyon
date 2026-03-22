namespace Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

public class TrendyolCategory
{
    public int id { get; set; }
    public string name { get; set; } = null!;
    public string displayName { get; set; } = null!;
    public TrendyolCategoryAttribute[] categoryAttributes { get; set; } = [];
}

public class TrendyolCategoryAttribute
{
    public bool AllowCustom { get; set; }
    public TrendyolAttribute Attribute { get; set; } = null!;
    public TrendyolAttributeValue[] AttributeValues { get; set; } = [];
    public int CategoryId { get; set; }
    public bool Required { get; set; }
    public bool Varianter { get; set; }
    public bool Slicer { get; set; }
}

public class TrendyolAttribute
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class TrendyolAttributeValue
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}