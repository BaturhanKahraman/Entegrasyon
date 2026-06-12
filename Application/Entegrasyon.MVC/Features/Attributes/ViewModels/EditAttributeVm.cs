namespace Entegrasyon.MVC.Features.Attributes.ViewModels;

public class EditAttributeVm
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; } = null!;
    public string CategoryAttributeHumanized { get; set; } = null!;

    /// <summary>
    /// Existing value IDs and names (for editing/removing).
    /// </summary>
    public List<AttributeValueVm> ExistingValues { get; set; } = [];

    /// <summary>
    /// New values to add (comma-separated or individual inputs).
    /// </summary>
    public string? NewValues { get; set; }
}

public class AttributeValueVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Remove { get; set; }
}
