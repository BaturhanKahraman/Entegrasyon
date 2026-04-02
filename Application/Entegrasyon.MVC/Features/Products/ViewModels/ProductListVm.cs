namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class ProductListVm
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
