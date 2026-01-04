using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.Products
{
    public record ProductDetailViewModel(
        [property: Display(Description = "Ürün kimlik numarası",Name = "Ürün Kimlik")] Guid Id,
        [property: Display(Name = "Ürün Başlığı")] string Title,
        [property: Display(Name = "Ürün Açıklaması")] string Description,
        [property: Display(Name = "Stok Kodu")] string StockCode,
        [property: Display(Name = "Marka")] string BrandName,
        [property: Display(Name = "Kategori")] string CategoryName,
        [property: Display(Name = "Toplam Miktar")] int TotalQuantity,
        [property: Display(Name = "Toplam Satılmış Miktar")] int TotalSoldQuantity)
    {
        [Display(Name = "Stoktaki Ürün")]
        public int ProductsInStock => TotalQuantity - TotalSoldQuantity;
        IEnumerable<ProductVariantDetailDto> ProductVariantsDetails { get; set; }
        IEnumerable<AttributeKeyValueDetailDto> AttributeKeyValueDetails { get; set; }
    }
}
