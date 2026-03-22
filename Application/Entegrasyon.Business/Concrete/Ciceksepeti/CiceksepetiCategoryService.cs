using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti kategori API çağrılarını yapan servis.
/// GET /api/v1/Categories — recursive ağaç döner.
/// GET /api/v1/Categories/{id}/attributes — kategori özelliklerini döner.
/// </summary>
public sealed class CiceksepetiCategoryService(
    ICiceksepetiApiClient apiClient,
    ILogger<CiceksepetiCategoryService> logger) : ICiceksepetiCategoryService
{
    private const string CategoriesEndpoint = "Categories";

    /// <inheritdoc />
    public async Task<IDataResult<CiceksepetiCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync(CategoriesEndpoint, ct);
            response.EnsureSuccessStatusCode();

            var data = await response.Content
                .ReadFromJsonAsync<CiceksepetiCategoryResponse>(cancellationToken: ct);

            if (data is null)
            {
                logger.LogWarning("Çiçeksepeti kategori API'si boş yanıt döndü");
                return new ErrorDataResult<CiceksepetiCategoryResponse>(null, "API boş yanıt döndü");
            }

            logger.LogInformation("Çiçeksepeti'nden {Count} kök kategori alındı", data.Categories.Count);
            return new SuccessDataResult<CiceksepetiCategoryResponse>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<CiceksepetiCategoryResponse>(null, $"Hata: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IDataResult<CiceksepetiCategoryAttributeResponse>> GetCategoryAttributesAsync(
        int categoryId, CancellationToken ct = default)
    {
        try
        {
            var url = $"Categories/{categoryId}/attributes";
            var response = await apiClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var data = await response.Content
                .ReadFromJsonAsync<CiceksepetiCategoryAttributeResponse>(cancellationToken: ct);

            if (data is null)
            {
                logger.LogWarning("Çiçeksepeti kategori özellikleri API'si boş yanıt döndü: {CategoryId}", categoryId);
                return new ErrorDataResult<CiceksepetiCategoryAttributeResponse>(null, "API boş yanıt döndü");
            }

            logger.LogInformation(
                "Çiçeksepeti kategori {CategoryId} için {Count} özellik alındı",
                categoryId, data.CategoryAttributes.Count);

            return new SuccessDataResult<CiceksepetiCategoryAttributeResponse>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti kategori özellikleri çekilirken hata: {CategoryId}", categoryId);
            return new ErrorDataResult<CiceksepetiCategoryAttributeResponse>(null, $"Hata: {ex.Message}");
        }
    }
}
