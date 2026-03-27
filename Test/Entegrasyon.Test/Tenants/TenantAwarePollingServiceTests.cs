using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Entegrasyon.UnitTest.Tenants;

// Concrete test implementation
public class TestPollingService : TenantAwarePollingService
{
    public int ProcessedTenantCount { get; private set; }
    public List<int> ProcessedTenantIds { get; } = new();

    public TestPollingService(
        IServiceScopeFactory scopeFactory,
        ITenantRegistry tenantRegistry,
        ILogger logger)
        : base(scopeFactory, tenantRegistry, logger)
    {
    }

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(1);
    protected override string? RequiredFeature => "Permissions.Products.View";

    protected override Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        ProcessedTenantCount++;
        ProcessedTenantIds.Add(tenantId);
        return Task.CompletedTask;
    }
}

public class TenantAwarePollingServiceTests
{
    [Fact]
    public async Task ProcessTenantsAsync_IteratesAllActiveTenants()
    {
        var tenants = new List<TenantRegistryEntry>
        {
            new(1, "acme", "Acme", "conn1", true, "Standard"),
            new(2, "beta", "Beta", "conn2", true, "Pro"),
        };

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetAllActiveAsync())
            .ReturnsAsync(tenants.AsReadOnly());

        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ITenantContext>(_ => new HttpTenantContext());
        serviceCollection.AddScoped<IFeatureService>(_ => mockFeatureService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var service = new TestPollingService(
            scopeFactory,
            mockRegistry.Object,
            NullLogger.Instance);

        await service.ProcessTenantsOnceAsync(CancellationToken.None);

        service.ProcessedTenantCount.Should().Be(2);
        service.ProcessedTenantIds.Should().Contain(1);
        service.ProcessedTenantIds.Should().Contain(2);
    }

    [Fact]
    public async Task ProcessTenantsAsync_SkipsTenantWithoutFeature()
    {
        var tenants = new List<TenantRegistryEntry>
        {
            new(1, "acme", "Acme", "conn1", true, "Standard"),
            new(2, "beta", "Beta", "conn2", true, "Pro"),
        };

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetAllActiveAsync())
            .ReturnsAsync(tenants.AsReadOnly());

        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync("Permissions.Products.View"))
            .ReturnsAsync(false);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ITenantContext>(_ => new HttpTenantContext());
        serviceCollection.AddScoped<IFeatureService>(_ => mockFeatureService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var service = new TestPollingService(
            scopeFactory,
            mockRegistry.Object,
            NullLogger.Instance);

        await service.ProcessTenantsOnceAsync(CancellationToken.None);

        service.ProcessedTenantCount.Should().Be(0);
    }
}
