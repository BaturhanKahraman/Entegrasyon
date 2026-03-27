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

    [Fact]
    public void CreateDbContext_AppendsMaxPoolSizeIfMissing()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => "Host=localhost;Database=tenant_test;Username=test;Password=test",
            loggerFactory);

        using var dbContext = factory.CreateDbContext();

        dbContext.Database.GetConnectionString()
            .Should().Contain("Maximum Pool Size=10");
    }

    [Fact]
    public void CreateDbContext_DoesNotDuplicateMaxPoolSize()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => "Host=localhost;Database=tenant_test;Username=test;Password=test;Maximum Pool Size=5",
            loggerFactory);

        using var dbContext = factory.CreateDbContext();

        // Should keep the existing value, not append another
        dbContext.Database.GetConnectionString()
            .Should().Contain("Maximum Pool Size=5");
        dbContext.Database.GetConnectionString()
            .Should().NotContain("Maximum Pool Size=10");
    }
}
