namespace Entegrasyon.Business.Abstract;

public interface IStorefrontTenantResolver
{
    Task<StorefrontTenantInfo?> ResolveAsync(string hostname);
    void InvalidateCache(string hostname);
}
