using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.ApiKeys;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class ApiKeyManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<ApiKeyManager> logger) : IApiKeyManager
{
    private const int KeyLength = 32;
    private const string KeyPrefixValue = "ent_";

    public async Task<IDataResult<(ApiKey Key, string PlainKey)>> CreateAsync(
        int tenantId, string name, string[] scopes, DateTimeOffset? expiresAt, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorDataResult<(ApiKey, string)>(default, "API anahtari adi bos olamaz.");
        if (scopes.Length == 0)
            return new ErrorDataResult<(ApiKey, string)>(default, "En az bir scope secilmelidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        if (await dbContext.ApiKeys.AnyAsync(k => k.TenantId == tenantId && k.Name == name))
            return new ErrorDataResult<(ApiKey, string)>(default, "Bu isimde bir API anahtari zaten mevcut.");

        var plainKey = GeneratePlainKey();
        var keyHash = HashKey(plainKey);

        var apiKey = new ApiKey
        {
            TenantId = tenantId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = plainKey[..(KeyPrefixValue.Length + 4)],
            Scopes = JsonSerializer.Serialize(scopes),
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedByUserId = createdByUserId
        };

        dbContext.ApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"API anahtari olusturuldu: {name}", LogType.Settings, LogAction.Add);
        logger.LogInformation("API key created: {KeyId} {Name} prefix={Prefix}", apiKey.Id, name, apiKey.KeyPrefix);

        return new SuccessDataResult<(ApiKey, string)>((apiKey, plainKey), "API anahtari olusturuldu. Anahtari simdi kopyalayin — tekrar gosterilemez.");
    }

    public async Task<List<ApiKey>> GetAllAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.ApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<IResult> RevokeAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var key = await dbContext.ApiKeys.FindAsync(id);
        if (key is null)
            return new ErrorResult("API anahtari bulunamadi.");

        key.IsActive = false;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"API anahtari iptal edildi: {key.Name}", LogType.Settings, LogAction.Update);
        return new SuccessResult("API anahtari iptal edildi.");
    }

    public async Task<ApiKey?> ValidateKeyAsync(string plainKey)
    {
        var hash = HashKey(plainKey);

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var key = await dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

        if (key is null) return null;
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTimeOffset.UtcNow) return null;

        return key;
    }

    public async Task UpdateLastUsedAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await dbContext.ApiKeys
            .Where(k => k.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTimeOffset.UtcNow));
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
