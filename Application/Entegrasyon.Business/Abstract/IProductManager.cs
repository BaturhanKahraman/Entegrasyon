using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos;

namespace Entegrasyon.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<Product>> AddProduct(AddProductDto dto);
    Task<IResult> GetProductByBarcode(string barcode);

    // Edit
    Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id);
    Task<IResult> UpdateProduct(EditProductDto dto);

    // Other
    Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId);
    Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto);
    Task<IResult> SoftDeleteProduct(Guid id);
    Task<int> GetProductCountByCategoryId(int categoryId);
    Task<bool> HasSoldProductsInCategory(int categoryId);

    // Storefront
    Task<IDataResult<Product>> GetProductBySeoSlugAsync(string slug);
    Task<IDataResult<Pageable<StorefrontProductCardDto>>> GetStorefrontProductsAsync(StorefrontCatalogQuery query);
    Task<IDataResult<List<StorefrontProductCardDto>>> GetNewProductsAsync(int count);
    Task<IDataResult<List<StorefrontProductCardDto>>> GetBestSellersAsync(int count);
    Task<IDataResult<List<StorefrontSearchSuggestionDto>>> GetSearchSuggestionsAsync(string query, int maxResults = 8);
    Task<IDataResult<StorefrontProductDetailDto>> GetStorefrontProductDetailAsync(string seoSlug);
}
