using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Business.Utility.Tenant;

public class HttpHeaderTenantProvider:ITenantProvider
{
    private readonly HttpContext _context;
    private const string HeaderName = "Tenant";

    public HttpHeaderTenantProvider(HttpContextAccessor contextAccessor)
    {
        _context = contextAccessor.HttpContext;
    }

    public string GetCurrentTenantId()
    {
        string tenantId = _context.Request.Headers[HeaderName];
        if (string.IsNullOrEmpty(tenantId))
            throw new TenantNotFoundException();
        return tenantId;
    }
}