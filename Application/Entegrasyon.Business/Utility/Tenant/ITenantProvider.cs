namespace Entegrasyon.Business.Utility.Tenant;

public interface ITenantProvider
{
    string GetCurrentTenantId();
}