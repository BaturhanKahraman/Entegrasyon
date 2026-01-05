namespace Entegrasyon.Blazor.ViewModels;

/// <summary>
/// Flat representation of tree node for virtual scrolling
/// </summary>
public class FlatTreeNode
{
    public CategoryTreeNode Node { get; set; } = null!;
    public int Level { get; set; }
    public bool Expanded { get; set; }
}
