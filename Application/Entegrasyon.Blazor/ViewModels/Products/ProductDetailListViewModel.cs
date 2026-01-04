namespace Entegrasyon.Blazor.ViewModels.Products
{
    public record ProductDetailListViewModel(
        Guid Id,
        string Title,
        string Description,
        string StockCode,
        string BrandName,
        string CategoryName,
        int TotalQuantity,
        int TotalSoldQuantity,
        int VariantCount)
    {
        public int TotalCurrentStock => TotalQuantity - TotalSoldQuantity;
    }
}
