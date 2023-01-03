using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public sealed class AddProductVariantDto
{
    public decimal DimensionalWeight { get; set; }
    public string CurrencyType { get; set; }
    public string Barcode { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public IEnumerable<ProductVariantAttribute> ProductVariantAttributes { get; set; }
    public IEnumerable<AddBranchOfficeStockDto> BranchOfficeStocks { get; set; }
    public IEnumerable<UploadedImage> UploadedImages { get; set; }

}

public sealed record UploadedImage
{
    public IFormFile UploadedImageFile { get; set; }
    public bool IsMainImage { get; set; }
}