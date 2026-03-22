namespace Entegrasyon.Entity.Labels;

public sealed class LabelTemplate : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LabelType Type { get; set; }
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public int Dpi { get; set; } = 203;
    public string LayoutJson { get; set; } = "[]";
    public bool IsDefault { get; set; }
}

public enum LabelType
{
    ProductBarcode = 1,
    Shelf = 2
}
