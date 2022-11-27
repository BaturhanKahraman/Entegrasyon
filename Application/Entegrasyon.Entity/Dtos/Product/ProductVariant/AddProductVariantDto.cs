using Entegrasyon.Entity.Categories;
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
    public decimal VatRate { get; set; }
    public ICollection<AttributeKeyValue> AttributeKeyValues { get; set; }
    public List<AddBranchOfficeStockDto> BranchOfficeStocks { get; set; }
    public ICollection<UploadedImage> UploadedImages { get; set; }

}

public sealed record UploadedImage
{
    public IFormFile UploadedImageFile { get; set; }
    public bool IsMainImage { get; set; }
}