using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Settings;

namespace Entegrasyon.Business.Abstract;

public interface IVatRateManager
{
    Task<List<VatRate>> GetAllAsync();
    Task<IDataResult<VatRate>> CreateAsync(string name, decimal rate, string? description);
    Task<IResult> UpdateAsync(int id, string name, decimal rate, string? description);
    Task<IResult> DeleteAsync(int id);
    Task<IResult> SetDefaultAsync(int id);
    Task<decimal> GetDefaultRateAsync();
}
