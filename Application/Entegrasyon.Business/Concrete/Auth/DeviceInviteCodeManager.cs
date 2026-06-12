using System.Security.Cryptography;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Devices;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Auth;

public class DeviceInviteCodeManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<DeviceInviteCodeManager> logger) : IDeviceInviteCodeManager
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string CodePrefix = "ENT-";

    public async Task<IDataResult<DeviceInviteCode>> IssueAsync(int tenantId, Guid createdByUserId, TimeSpan? ttl = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var code = new DeviceInviteCode
        {
            TenantId = tenantId,
            Code = GenerateCode(),
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl ?? DefaultTtl),
            CreatedByUserId = createdByUserId
        };

        dbContext.DeviceInviteCodes.Add(code);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Cihaz davet kodu oluşturuldu: {code.Code}", LogType.Settings, LogAction.Add);
        logger.LogInformation(
            "Device invite code issued: {Code} Tenant={TenantId} ExpiresAt={ExpiresAt}",
            code.Code, tenantId, code.ExpiresAt);

        return new SuccessDataResult<DeviceInviteCode>(code, "Davet kodu oluşturuldu.");
    }

    public async Task<IDataResult<DeviceInviteCode>> RedeemAsync(string code, int deviceId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorDataResult<DeviceInviteCode>(null!, "Davet kodu boş olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invite = await dbContext.DeviceInviteCodes
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);

        if (invite is null)
            return new ErrorDataResult<DeviceInviteCode>(null!, "Davet kodu bulunamadı.");

        if (invite.RedeemedAt is not null)
            return new ErrorDataResult<DeviceInviteCode>(null!, "Davet kodu zaten kullanılmış.");

        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
            return new ErrorDataResult<DeviceInviteCode>(null!, "Davet kodunun süresi dolmuş.");

        invite.RedeemedAt = DateTimeOffset.UtcNow;
        invite.RedeemedByDeviceId = deviceId;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Cihaz davet kodu kullanıldı: {invite.Code}", LogType.Settings, LogAction.Update);
        logger.LogInformation(
            "Device invite code redeemed: {Code} DeviceId={DeviceId}", invite.Code, deviceId);

        return new SuccessDataResult<DeviceInviteCode>(invite, "Davet kodu kullanıldı.");
    }

    public async Task<List<DeviceInviteCode>> GetActiveAsync(int tenantId)
    {
        var now = DateTimeOffset.UtcNow;
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.DeviceInviteCodes
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && !c.IsDeleted
                        && c.RedeemedAt == null
                        && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    // 0/O, 1/I gibi karışan karakterler yok — okunur + URL-safe + sabit uzunluk
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static string GenerateCode()
    {
        // 10-char crypto-güçlü, bias'sız, her zaman tam 10 karakter (~50 bit entropi).
        // Eski yöntem base64'ten +,/,= SİLDİĞİ için string 10'un altına düşüp Substring
        // patlatabiliyordu (~%5 flaky); GetString sabit uzunluk garanti eder.
        return CodePrefix + RandomNumberGenerator.GetString(CodeAlphabet, 10);
    }
}
