using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontSearchHistoryManager
{
    Task<IResult> RecordSearchAsync(int tenantId, string query);
    Task<IDataResult<List<string>>> GetPopularSearchesAsync(int tenantId, int count = 10);
}
