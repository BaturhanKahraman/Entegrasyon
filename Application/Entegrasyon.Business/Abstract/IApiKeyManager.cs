using Entegrasyon.Entity.ApiKeys;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IApiKeyManager
{
    Task<IDataResult<(ApiKey Key, string PlainKey)>> CreateAsync(int tenantId, string name, string[] scopes, DateTimeOffset? expiresAt, Guid createdByUserId);
    Task<List<ApiKey>> GetAllAsync(int tenantId);
    Task<IResult> RevokeAsync(int id);
    Task<ApiKey?> ValidateKeyAsync(string plainKey);
    Task UpdateLastUsedAsync(int id);
}
