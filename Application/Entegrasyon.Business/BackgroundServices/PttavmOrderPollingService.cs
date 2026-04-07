using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 5 dakikada PttAVM Sipariş API'sini poll eder,
/// son 24 saatlik Sipariş penceresi kullanilir.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class PttavmOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PttavmOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(5);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<IPttavmOrderService>();

        var effectiveLastPoll = lastPoll < DateTimeOffset.UtcNow - OrderLookbackWindow
            ? DateTimeOffset.UtcNow - OrderLookbackWindow
            : lastPoll;
        var endDate = DateTimeOffset.UtcNow;

        var result = await orderService.SearchOrdersAsync(
            effectiveLastPoll.UtcDateTime, endDate.UtcDateTime, false, ct);

        if (!result.Success)
        {
            logger.LogWarning("PttAVM sipariş fetch başarısız for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        var orders = result.Data ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportPttavmOrdersAsync(orders) eklendiginde buraya ekle
            logger.LogInformation("Tenant {TenantId}: PttAVM: {Count} sipariş alındı",
                tenantId, orders.Count);
        }
        else
        {
            logger.LogDebug("Tenant {TenantId}: PttAVM: yeni sipariş yok", tenantId);
        }
    }
}
