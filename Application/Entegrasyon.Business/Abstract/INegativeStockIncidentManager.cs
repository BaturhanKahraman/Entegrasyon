using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Stock;

namespace Entegrasyon.Business.Abstract;

public sealed record CreateIncidentDto(
    int TenantId,
    int BranchOfficeId,
    Guid ProductVariantId,
    int Quantity,
    int StockBefore,
    int StockAfter,
    string TriggeringSource,
    string? TriggeringReferenceId,
    Guid? TriggeringSaleId);

public sealed record ResolveIncidentDto(
    NegativeStockResolution Resolution,
    Guid ResolvedByUserId,
    string? Notes);

public interface INegativeStockIncidentManager
{
    Task<IDataResult<NegativeStockIncident>> CreateAsync(CreateIncidentDto dto);

    Task<List<NegativeStockIncident>> GetActiveAsync(int tenantId);

    Task<IDataResult<NegativeStockIncident>> GetByIdAsync(int id, int tenantId);

    Task<IResult> ResolveAsync(int id, int tenantId, ResolveIncidentDto resolution);

    Task<int> GetActiveCountAsync(int tenantId);
}
