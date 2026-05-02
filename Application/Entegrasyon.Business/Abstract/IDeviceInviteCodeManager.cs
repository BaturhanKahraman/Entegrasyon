using Entegrasyon.Entity.Devices;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IDeviceInviteCodeManager
{
    Task<IDataResult<DeviceInviteCode>> IssueAsync(int tenantId, Guid createdByUserId, TimeSpan? ttl = null);

    Task<IDataResult<DeviceInviteCode>> RedeemAsync(string code, int deviceId);

    Task<List<DeviceInviteCode>> GetActiveAsync(int tenantId);
}
