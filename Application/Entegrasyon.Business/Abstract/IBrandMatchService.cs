using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBrandMatchService
{
    Task AddRange(List<BrandMarketPlaceMatch> entities);
    Task<List<int>> GetMarketPlaceBrandIdsByMarketPlaceId(int marketPlaceId);

    /// <summary>
    /// Brand mapping özet istatistiklerini getirir (Trendyol için).
    /// </summary>
    Task<BrandMappingSummaryDto> GetBrandMappingsSummaryAsync();

    /// <summary>
    /// Tüm brand mapping'lerini getirir.
    /// </summary>
    Task<List<BrandMarketPlaceMatchDto>> GetAllBrandMappingsAsync(int marketPlaceId);

    /// <summary>
    /// Mapping'i olmayan brand'ları getirir.
    /// </summary>
    Task<List<BrandDto>> GetUnmappedBrandsAsync(int marketPlaceId);

    /// <summary>
    /// Manual olarak yeni brand mapping oluşturur.
    /// </summary>
    Task<IResult> CreateBrandMappingAsync(CreateBrandMarketPlaceMatchDto dto);

    /// <summary>
    /// Brand mapping'ini siler.
    /// </summary>
    Task<IResult> RemoveBrandMappingAsync(int brandId, int marketPlaceId);

    /// <summary>
    /// Trendyol'dan brand'ları import eder (TODO: Implementation pending).
    /// </summary>
    Task<IResult> ImportTrendyolBrandsAsync();

    /// <summary>
    /// Trendyol ile sync'i tetikler (TODO: Implementation pending).
    /// </summary>
    Task<IResult> SyncWithTrendyolAsync();
}
