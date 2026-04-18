using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IUnifiedSaleManager
{
    Task<IDataResult<Pageable<UnifiedSaleListItemDto>>> GetPageableAsync(UnifiedSaleFilterDto filter);
    Task<IDataResult<UnifiedSaleSummaryDto>> GetSummaryAsync(UnifiedSaleFilterDto filter);
    Task<IDataResult<List<UnifiedSaleSourceCountDto>>> GetSourceCountsAsync(UnifiedSaleFilterDto filter);
}
