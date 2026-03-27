using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada abandoned cart recovery islemi calistirir.
/// Sadece storefront ozelligi acik olan tenant'lar icin calisir.
/// </summary>
public class AbandonedCartBackgroundService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<AbandonedCartBackgroundService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);
    protected override string? RequiredFeature => null; // Feature check done internally via StorefrontSettings

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Check if this tenant has abandoned cart recovery enabled
        var settings = await db.StorefrontSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted, ct);

        if (settings is null || !settings.AbandonedCartRecoveryEnabled)
            return;

        var cartManager = services.GetRequiredService<IStorefrontAbandonedCartManager>();
        await cartManager.ProcessAbandonedCartsAsync(tenantId);

        logger.LogDebug("Tenant {TenantId}: Abandoned cart processing completed", tenantId);
    }
}
