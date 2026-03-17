using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Utility.Tenant;

public class IntegrationDbContextFactory: IIntegrationDbContextFactory
{
    private readonly ITenantProvider _tenantProvider;

    public IntegrationDbContextFactory(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public IntegrationDbContext Create()
    {
        var dbContextOptionsBuilder= new DbContextOptionsBuilder();
        #if DEBUG
            dbContextOptionsBuilder.EnableDetailedErrors();
            dbContextOptionsBuilder.EnableSensitiveDataLogging();
        #endif
        throw new InvalidOperationException("Multi-tenant DbContext factory is not yet configured.");
    }
}