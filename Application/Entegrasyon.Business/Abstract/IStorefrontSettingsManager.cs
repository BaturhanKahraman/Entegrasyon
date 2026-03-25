using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontSettingsManager
{
    Task<IDataResult<StorefrontSettings>> GetByTenantIdAsync(int tenantId);
    Task<IResult> ToggleMaintenanceModeAsync(int tenantId, bool enabled, string? message);
}
