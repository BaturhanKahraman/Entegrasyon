namespace Entegrasyon.Entity.Labels;

public sealed class LabelElement
{
    public string ElementType { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int FontSize { get; set; } = 24;
    public bool Bold { get; set; }
    public bool Strikethrough { get; set; }
    public string? CustomText { get; set; }
}
