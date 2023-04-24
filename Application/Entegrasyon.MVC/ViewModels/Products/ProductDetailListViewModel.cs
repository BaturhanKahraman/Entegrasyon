namespace Entegrasyon.MVC.ViewModels.Products
{
    public class ProductDetailListViewModel {

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StockCode { get; set; }
        public string BrandName { get; set; }
        public string CategoryName { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalSoldQuantity { get; set; }
        public int TotalCurrentStock => TotalQuantity - TotalSoldQuantity;
        public int VariantCount { get; set; }
    } 
}
