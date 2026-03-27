using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 5 dakikada Pazarama iade/iptal taleplerini poll eder.
/// Pazarama:UseMock = true ise polling yapılmaz.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class PazaramaRefundPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PazaramaRefundPollingService> logger,
    IConfiguration configuration)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(5);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        if (configuration.GetValue<bool>("Pazarama:UseMock"))
        {
            logger.LogInformation("PazaramaRefundPollingService: UseMock=true, polling devre disi for tenant {TenantId}",
                tenantId);
            return;
        }

        var refundService = services.GetRequiredService<IPazaramaRefundService>();

        var now = DateTimeOffset.UtcNow;

        // Poll pending refunds (status=1)
        var refundResult = await refundService.GetRefundsAsync(lastPoll, now, refundStatus: 1);
        if (refundResult.Success && refundResult.Data?.RefundList?.Count > 0)
        {
            logger.LogInformation("Tenant {TenantId}: Pazarama: {Count} yeni iade talebi bulundu",
                tenantId, refundResult.Data.RefundList.Count);
        }

        // Poll pending cancellations (status=1)
        var cancelResult = await refundService.GetCancellationsAsync(lastPoll, now, refundStatus: 1);
        if (cancelResult.Success && cancelResult.Data?.RefundList?.Count > 0)
        {
            logger.LogInformation("Tenant {TenantId}: Pazarama: {Count} yeni iptal talebi bulundu",
                tenantId, cancelResult.Data.RefundList.Count);
        }
    }
}
