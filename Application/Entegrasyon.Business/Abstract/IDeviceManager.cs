using Entegrasyon.Entity.Devices;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IDeviceManager
{
    Task<IDataResult<(Device Device, string PlainKey)>> RegisterAsync(
        int tenantId, string name, string os, string hostname);

    Task<IDataResult<(Device Device, string PlainKey)>> RegisterWithInviteCodeAsync(
        string inviteCode, string deviceName, string os, string hostname);

    Task<Device?> ValidateKeyAsync(string plainKey);

    Task<IResult> RevokeAsync(int deviceId);

    Task<List<Device>> GetAllAsync(int tenantId);

    Task UpdateLastSeenAsync(int deviceId);
}
