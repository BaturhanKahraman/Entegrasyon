namespace Entegrasyon.MVC.Shared.ViewModels;

public class PaginationVm
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string BaseUrl { get; set; } = "";
    public string TargetId { get; set; } = "";
    public Dictionary<string, string> QueryParams { get; set; } = new();
}
