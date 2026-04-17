using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface IReturnReasonManager
{
    Task<IDataResult<List<ReturnReason>>> GetAllAsync(bool activeOnly = true);
    Task<IDataResult<ReturnReason>> GetByIdAsync(int id);
    Task<IResult> CreateAsync(string code, string name, int sortOrder = 0);
    Task<IResult> UpdateAsync(int id, string name, int sortOrder, bool isActive);
    Task<IResult> DeactivateAsync(int id);
}
