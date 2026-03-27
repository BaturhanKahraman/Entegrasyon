using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryMatchService
{
    Task<CategoryMatchSummaryDto> GetCategoryMatchSummaryAsync();
    Task<List<CategoryMarketplaceMappingDto>> GetAllCategoryMappingsAsync(int marketPlaceId);
    Task<IResult> CreateCategoryMappingAsync(CreateCategoryMarketplaceMatchDto dto);
    Task<IResult> RemoveCategoryMappingAsync(int categoryId, int marketPlaceId);
    Task<IDataResult<BulkCategoryMatchResultDto>> BulkCreateCategoryMappingsAsync(BulkCategoryMatchDto dto);
}
