using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Shared.DTO;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IProductService
{
    Task<IResult> AddProduct(AddProductDto dto);
    Task<IResult> GetProductByBarcode(string barcode);
    Task<IResult> UpdateProduct(ProductEditDetailDto dto);
    Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId);
    Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto);
    Task<IDataResult<ProductEditDetailDto>> GetProductEditDetailById(Guid id);
    Task<int> GetProductCountByCategoryId(int categoryId);
}
