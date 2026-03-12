using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos;

namespace Entegrasyon.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<Product>> AddProduct(AddProductDto dto);
    Task<IResult> GetProductByBarcode(string barcode);
    Task<IResult> UpdateProduct(ProductEditDetailDto dto);
    Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId);
    Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto);
    Task<IDataResult<ProductEditDetailDto>> GetProductEditDetailById(Guid id);
    Task<int> GetProductCountByCategoryId(int categoryId);
    Task<bool> HasSoldProductsInCategory(int categoryId);
}
