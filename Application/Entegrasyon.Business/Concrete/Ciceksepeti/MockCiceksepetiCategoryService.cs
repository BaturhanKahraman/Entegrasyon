using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti kategori servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; boş başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockCiceksepetiCategoryService(
    ILogger<MockCiceksepetiCategoryService> logger) : ICiceksepetiCategoryService
{
    public Task<IDataResult<CiceksepetiCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get categories");
        var response = new CiceksepetiCategoryResponse(Categories: []);
        return Task.FromResult<IDataResult<CiceksepetiCategoryResponse>>(
            new SuccessDataResult<CiceksepetiCategoryResponse>(response));
    }

    public Task<IDataResult<CiceksepetiCategoryAttributeResponse>> GetCategoryAttributesAsync(int categoryId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get category attributes: CategoryId={CategoryId}", categoryId);
        var response = new CiceksepetiCategoryAttributeResponse(
            CategoryId: categoryId,
            CategoryName: "Mock Kategori",
            CategoryAttributes: []);
        return Task.FromResult<IDataResult<CiceksepetiCategoryAttributeResponse>>(
            new SuccessDataResult<CiceksepetiCategoryAttributeResponse>(response));
    }
}
