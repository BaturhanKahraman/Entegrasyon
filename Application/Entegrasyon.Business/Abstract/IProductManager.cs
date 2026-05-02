using Entegrasyon.Entity.Dtos.POS;
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

    /// <summary>
    /// Stok kodunun bir ürün tarafından kullanılıp kullanılmadığını kontrol eder.
    /// Wizard'da erken validation (Step 1) için kullanılır — kullanıcıya Step 6'ya kadar beklemeden hata gösterir.
    /// </summary>
    /// <param name="stockCode">Kontrol edilecek stok kodu. Boş/null ise her zaman true döner.</param>
    /// <returns>Stok kodu kullanılabilir (unique) ise true, zaten kullanılıyorsa false.</returns>
    Task<bool> IsStockCodeAvailableAsync(string? stockCode);

    // Edit
    Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id);
    Task<IResult> UpdateProduct(EditProductDto dto);

    // Other
    Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId);
    Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto);

    /// <summary>
    /// POS ürün arama — fuzzy (pg_trgm similarity) + barkod exact + tsquery.
    /// Varyantları tek seferde getirir; kullanıcı hangi varyantı seçtiğini görebilir.
    /// </summary>
    Task<IDataResult<POSProductSearchResultDto>> SearchPOSProductsAsync(int branchOfficeId, string query, int limit = 10);
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
    Task<IDataResult<List<StorefrontProductDetailDto>>> GetProductsByIdsAsync(List<Guid> ids);
    Task<IDataResult<List<BrandFilterDto>>> GetBrandsForCategoryAsync(int categoryId);

    // Store Settings
    Task<IResult> UpdateStoreSettings(Guid productId, string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords, Dictionary<Guid, decimal> variantECommercePrices);
    Task<IResult> PublishProduct(Guid productId, string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords, Dictionary<Guid, decimal> variantECommercePrices);
    Task<Dictionary<Guid, string>> GetVariantAttributeNamesAsync(Guid productId);
}
