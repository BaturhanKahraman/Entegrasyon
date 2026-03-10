using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IProductVariantManager
{
    Task<ProductVariant> GetById(Guid id);
    Task<string> GetLastProductVariantBarcode();
    Task<List<string>> GetAllVariantsBarcodes();
    Task<IDataResult<ProductVariantSaleSearchDto>> GetProductVariantByBarcode(string barcode);
    Task<IResult> GetProductVariantsBySearchText(string fullTextSearch);
}
