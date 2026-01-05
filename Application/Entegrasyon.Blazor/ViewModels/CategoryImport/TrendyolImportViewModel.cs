namespace Entegrasyon.Blazor.ViewModels.CategoryImport;

public sealed class TrendyolImportViewModel
{
    public string? Id { get; set; }
    public string? Text { get; set; }
    public string? Parent { get; set; }
    public List<TrendyolImportViewModel> Children { get; set; } = new();

}
