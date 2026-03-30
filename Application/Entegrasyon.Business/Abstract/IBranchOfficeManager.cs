using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBranchOfficeManager
{
    Task<IDataResult<List<BranchOffice>>> GetBranchList(CancellationToken token = default);
    Task<IDataResult<BranchOffice>> AddBranch(BranchOfficeAddDto officeDto);
    Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId);
    Task<IDataResult<BranchOffice>> Update(BranchOfficeEditDto dto);
    Task<IResult> Delete(int id);
    Task<bool> CheckIfOfficesExits(IEnumerable<int> officeIds);
    Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(int pageIndex = 0, int pageSize = 50);
    Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(BranchPaginatedRequest request);
    Task<BranchOffice> GetBranchById(int id);
    Task<IDataResult<List<BranchOfficePageListDto>>> GetPageBranchListAsync();
    Task<IDataResult<List<BranchStockItemDto>>> GetBranchStocksAsync(int branchId);
    Task<IDataResult<List<StockMovementViewDto>>> GetBranchStockMovementsAsync(int branchId, DateTimeOffset? from, DateTimeOffset? to);
    Task<IDataResult<List<MarketPlaceWarehouse>>> GetBranchMarketPlacesAsync(int branchId);
    Task<IResult> AddMarketPlaceWarehouseAsync(int branchId, int marketPlaceId);
    Task<IResult> RemoveMarketPlaceWarehouseAsync(int warehouseId);
}
