using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Çiçeksepeti kategori API çağrılarını yöneten servis.
/// </summary>
public interface ICiceksepetiCategoryService
{
    /// <summary>
    /// Çiçeksepeti'nden tüm kategorileri ağaç yapısında getirir.
    /// GET /api/v1/Categories
    /// </summary>
    Task<IDataResult<CiceksepetiCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Belirtilen kategoriye ait özellikleri getirir.
    /// GET /api/v1/Categories/{categoryId}/attributes
    /// </summary>
    Task<IDataResult<CiceksepetiCategoryAttributeResponse>> GetCategoryAttributesAsync(int categoryId, CancellationToken ct = default);
}
