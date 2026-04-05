using Entegrasyon.Entity.Dtos.Product;
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
    Task<IDataResult<ProductVariantEditDetailDto>> GetVariantEditDetail(Guid variantId);
    Task<IResult> AddVariant(Guid productId, AddProductVariantDto dto);
    Task<IResult> UpdateVariant(EditProductVariantDto dto);
    Task<IResult> SoftDeleteVariant(Guid variantId);
    Task<List<(Guid VariantId, string Barcode, List<(int ImageId, string Src, bool IsMain)> Images)>> GetProductVariantImages(Guid productId, Guid? excludeVariantId);
    Task<IDataResult<VariantDetailPageDto>> GetVariantDetailPage(Guid variantId);
}
