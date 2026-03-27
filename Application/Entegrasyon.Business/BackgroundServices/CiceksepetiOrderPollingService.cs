using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 60 saniyede Çiçeksepeti sipariş API'sini poll eder,
/// son 2 saatlik sipariş penceresi kullanılır.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class CiceksepetiOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<CiceksepetiOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(2);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(60);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<ICiceksepetiOrderService>();

        var effectiveLastPoll = lastPoll < DateTimeOffset.UtcNow - OrderLookbackWindow
            ? DateTimeOffset.UtcNow - OrderLookbackWindow
            : lastPoll;
        var endDate = DateTimeOffset.UtcNow;

        var request = new CiceksepetiGetOrdersRequest(
            StartDate: effectiveLastPoll.ToString("o"),
            EndDate: endDate.ToString("o"),
            PageSize: 100,
            Page: 1,
            StatusId: null,
            OrderNo: null,
            OrderItemNo: null);

        var result = await orderService.GetOrdersAsync(request, ct);

        if (!result.Success)
        {
            logger.LogWarning("Çiçeksepeti sipariş fetch başarısız for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        var orders = result.Data?.SupplierOrderListWithBranch ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportCiceksepetiOrdersAsync(orders) eklendiğinde buraya ekle
            logger.LogInformation("Tenant {TenantId}: Çiçeksepeti: {Count} sipariş alındı",
                tenantId, orders.Count);
        }
        else
        {
            logger.LogDebug("Tenant {TenantId}: Çiçeksepeti: yeni sipariş yok", tenantId);
        }
    }
}
