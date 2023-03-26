using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

namespace Entegrasyon.Business.Utility.Tenant;

public interface IIntegrationDbContextFactory
{
    IntegrationDbContext Create();
}