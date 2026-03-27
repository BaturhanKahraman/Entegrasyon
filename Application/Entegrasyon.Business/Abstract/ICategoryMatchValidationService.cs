using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryMatchValidationService
{
    Task<IDataResult<CategoryMatchValidationResultDto>> ValidateCategoryMatchAsync(int categoryId, int marketPlaceId);
    Task<IDataResult<List<CategoryMatchValidationResultDto>>> ValidateAllMatchesAsync(int marketPlaceId);
}
