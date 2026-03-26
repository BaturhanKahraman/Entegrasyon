using Entegrasyon.DataAccess;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WhenConnectionStringProvided_CreatesContextWithCorrectConnection()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => "Host=localhost;Database=tenant_test_1;Username=test;Password=test",
            loggerFactory);

        using var dbContext = factory.CreateDbContext();

        dbContext.Should().NotBeNull();
        dbContext.Should().BeOfType<IntegrationDbContext>();
        dbContext.Database.GetConnectionString().Should().Contain("tenant_test_1");
    }

    [Fact]
    public void CreateDbContext_WhenProviderThrows_PropagatesException()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => throw new InvalidOperationException("Tenant context is not initialized."),
            loggerFactory);

        var act = () => factory.CreateDbContext();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }
}
