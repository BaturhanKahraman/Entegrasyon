using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IProductSyncManager
{
    Task<ProductSyncSummaryDto> GetSyncSummaryAsync(int marketPlaceId);
    Task<DataResult<Pageable<ProductSyncListItemDto>>> GetProductSyncListAsync(
        int marketPlaceId, MarketplaceSyncState? stateFilter, string searchKey, int pageIndex, int pageSize);
    Task<IDataResult<ProductSyncDetailDto>> GetProductSyncDetailAsync(Guid productId);
    Task<IResult> SyncProductAsync(Guid productId, int marketPlaceId);
    Task<IResult> RetryFailedAsync(Guid productId, int marketPlaceId);
    Task<IResult> SyncAllPendingAsync(int marketPlaceId);
    Task<IResult> RetryAllFailedAsync(int marketPlaceId);
}
