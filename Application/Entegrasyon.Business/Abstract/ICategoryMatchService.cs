using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryMatchService
{
    Task<CategoryMatchSummaryDto> GetCategoryMatchSummaryAsync(int? marketPlaceId = null);
    Task<List<CategoryMarketplaceMappingDto>> GetAllCategoryMappingsAsync(int marketPlaceId);
    Task<IResult> CreateCategoryMappingAsync(CreateCategoryMarketplaceMatchDto dto);
    Task<IResult> RemoveCategoryMappingAsync(int categoryId, int marketPlaceId);

    /// <summary>
    /// Bu kategorideki, pazaryerine GÖNDERİLMİŞ (Published) ürün sayısı. Unmap/silme öncesi
    /// kullanıcıyı uyarmak için (eşleme kalkınca bu ürünler pazaryerinde güncellenemez).
    /// </summary>
    Task<int> GetPublishedProductCountAsync(int categoryId, int marketPlaceId);

    /// <summary>
    /// Herhangi bir pazaryerine aktif eşleşmesi olan kategori id'leri. Kategori oluşturma wizard'ında
    /// "eşli kategoriye alt kategori ekleme" uyarısı için (eşli kategori parent olunca mapping geçersizleşir).
    /// </summary>
    Task<HashSet<int>> GetMappedCategoryIdsAsync();

    Task<IDataResult<BulkCategoryMatchResultDto>> BulkCreateCategoryMappingsAsync(BulkCategoryMatchDto dto);

    // Template operations
    Task<IDataResult<List<CategoryMatchTemplateDto>>> GetTemplatesAsync(int marketPlaceId);
    Task<IDataResult<CategoryMatchTemplateDetailDto>> GetTemplateDetailAsync(int templateId);
    Task<IResult> SaveTemplateAsync(SaveCategoryMatchTemplateDto dto);
    Task<IDataResult<BulkCategoryMatchResultDto>> ApplyTemplateAsync(int templateId);
    Task<IResult> DeleteTemplateAsync(int templateId);
}
