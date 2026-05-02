using System.Security.Cryptography;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Devices;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Auth;

public class DeviceManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<DeviceManager> logger) : IDeviceManager
{
    private const int KeyLength = 32;
    private const string KeyPrefixValue = "dev_";

    public async Task<IDataResult<(Device Device, string PlainKey)>> RegisterAsync(
        int tenantId, string name, string os, string hostname)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorDataResult<(Device, string)>(default, "Cihaz adı boş olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var duplicate = await dbContext.Devices
            .AnyAsync(d => d.TenantId == tenantId && d.Name == name && !d.IsRevoked && !d.IsDeleted);
        if (duplicate)
            return new ErrorDataResult<(Device, string)>(default, "Bu cihaz adı zaten kullanılıyor.");

        var plainKey = GeneratePlainKey();
        var device = new Device
        {
            TenantId = tenantId,
            Name = name,
            OS = os ?? string.Empty,
            Hostname = hostname ?? string.Empty,
            KeyHash = HashKey(plainKey),
            KeyPrefix = plainKey[..(KeyPrefixValue.Length + 4)],
            RegisteredAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Cihaz kaydedildi: {name}", LogType.Settings, LogAction.Add);
        logger.LogInformation(
            "Device registered: {DeviceId} Name={Name} Prefix={Prefix} OS={OS}",
            device.Id, name, device.KeyPrefix, device.OS);

        return new SuccessDataResult<(Device, string)>(
            (device, plainKey),
            "Cihaz kaydedildi. API anahtarını güvenli yere kopyalayın — tekrar gösterilmez.");
    }

    public async Task<IDataResult<(Device Device, string PlainKey)>> RegisterWithInviteCodeAsync(
        string inviteCode, string deviceName, string os, string hostname)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return new ErrorDataResult<(Device, string)>(default, "Davet kodu gereklidir.");
        if (string.IsNullOrWhiteSpace(deviceName))
            return new ErrorDataResult<(Device, string)>(default, "Cihaz adı boş olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invite = await dbContext.DeviceInviteCodes
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Code == inviteCode && !c.IsDeleted);

        if (invite is null)
            return new ErrorDataResult<(Device, string)>(default, "Davet kodu bulunamadı.");
        if (invite.RedeemedAt is not null)
            return new ErrorDataResult<(Device, string)>(default, "Davet kodu zaten kullanılmış.");
        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
            return new ErrorDataResult<(Device, string)>(default, "Davet kodunun süresi dolmuş.");

        var duplicate = await dbContext.Devices
            .AnyAsync(d => d.TenantId == invite.TenantId && d.Name == deviceName && !d.IsRevoked && !d.IsDeleted);
        if (duplicate)
            return new ErrorDataResult<(Device, string)>(default, "Bu cihaz adı zaten kullanılıyor.");

        var plainKey = GeneratePlainKey();
        var device = new Device
        {
            TenantId = invite.TenantId,
            Name = deviceName,
            OS = os ?? string.Empty,
            Hostname = hostname ?? string.Empty,
            KeyHash = HashKey(plainKey),
            KeyPrefix = plainKey[..(KeyPrefixValue.Length + 4)],
            RegisteredAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();

        invite.RedeemedAt = DateTimeOffset.UtcNow;
        invite.RedeemedByDeviceId = device.Id;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Cihaz kaydedildi (davet koduyla): {deviceName}", LogType.Settings, LogAction.Add);
        logger.LogInformation(
            "Device registered via invite code: {DeviceId} Tenant={TenantId} Code={Code}",
            device.Id, invite.TenantId, inviteCode);

        return new SuccessDataResult<(Device, string)>((device, plainKey), "Cihaz kaydedildi.");
    }

    public async Task<Device?> ValidateKeyAsync(string plainKey)
    {
        if (string.IsNullOrWhiteSpace(plainKey)) return null;

        var hash = HashKey(plainKey);
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        return await dbContext.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.KeyHash == hash && !d.IsRevoked && !d.IsDeleted);
    }

    public async Task<IResult> RevokeAsync(int deviceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device is null || device.IsDeleted)
            return new ErrorResult("Cihaz bulunamadı.");

        device.IsRevoked = true;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Cihaz iptal edildi: {device.Name}", LogType.Settings, LogAction.Update);
        return new SuccessResult("Cihaz iptal edildi.");
    }

    public async Task<List<Device>> GetAllAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.Devices
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .OrderByDescending(d => d.RegisteredAt)
            .ToListAsync();
    }

    public async Task UpdateLastSeenAsync(int deviceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device is null) return;

        device.LastSeenAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private static string GeneratePlainKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyLength);
        return KeyPrefixValue + Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..KeyLength];
    }

    private static string HashKey(string plainKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainKey));
        return Convert.ToHexStringLower(bytes);
    }
}
