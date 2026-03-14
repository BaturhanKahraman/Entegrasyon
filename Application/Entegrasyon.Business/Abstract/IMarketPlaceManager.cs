using Entegrasyon.Entity;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketPlaceManager
{
    Task<IDataResult<MarketPlace>> GetByIdAsync(int id);
    Task<IDataResult<List<MarketPlace>>> GetAllAsync();
    Task<IResult> UpdateCredentialsAsync(int id, string apiKey, string apiSecret, string sellerId, string? baseUrl);
    Task<IDataResult<List<MarketPlaceWarehouse>>> GetWarehousesAsync(int marketPlaceId);
    Task<IResult> SetWarehousesAsync(int marketPlaceId, List<int> branchOfficeIds);
    Task<IDataResult<bool>> TestConnectionAsync(int marketPlaceId);
}
