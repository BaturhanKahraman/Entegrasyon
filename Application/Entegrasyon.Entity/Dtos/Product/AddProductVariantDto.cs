using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed class AddProductVariantDto
{
    public decimal DimensionalWeight { get; set; }
    public string CurrencyType { get; set; }
    public string Barcode { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal VatRate { get; set; }
    public AttributeKeyValue[] AttributeKeyValues { get; set; }
    public List<AddBranchOfficeStockDto> BranchOfficeStocks { get; set; }
    public IFormFileCollection UploadedImages { get; set; }
    
}