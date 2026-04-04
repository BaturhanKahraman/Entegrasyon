using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Tum polling background service'ler icin tenant-aware base class.
/// Her poll dongusu: aktif tenant'lari iterate et, feature kontrolu yap,
/// tenant scope olustur, PollForTenantAsync cagir.
/// </summary>
public abstract class TenantAwarePollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastPollTimes = new();

    /// <summary>Polling araligi.</summary>
    protected abstract TimeSpan PollInterval { get; }

    /// <summary>
    /// Bu servisin calismasi icin gereken feature permission.
    /// null ise feature kontrolu yapilmaz (tum tenantlar icin calisir).
    /// </summary>
    protected virtual string? RequiredFeature => null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessTenantsOnceAsync(stoppingToken);
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor, normal shutdown — exception'i yutuyoruz.
        }
    }

    /// <summary>
    /// Tum aktif tenant'lari iterate eder. Test'ten dogrudan cagrilabilir.
    /// </summary>
    public async Task ProcessTenantsOnceAsync(CancellationToken ct)
    {
        var tenants = await tenantRegistry.GetAllActiveAsync();

        foreach (var tenant in tenants)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                // Tenant context'i initialize et
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenantContext.Initialize(tenant);

                // Feature kontrolu
                if (RequiredFeature is not null)
                {
                    var featureService = scope.ServiceProvider.GetRequiredService<IFeatureService>();
                    if (!await featureService.IsFeatureEnabledAsync(RequiredFeature))
                        continue;
                }

                var lastPoll = _lastPollTimes.GetOrAdd(tenant.TenantId,
                    _ => DateTimeOffset.UtcNow.AddDays(-1));

                await PollForTenantAsync(scope.ServiceProvider, tenant.TenantId, lastPoll, ct);

                _lastPollTimes[tenant.TenantId] = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Service} failed for tenant {TenantId}",
                    GetType().Name, tenant.TenantId);
            }
        }
    }

    /// <summary>
    /// Tek bir tenant icin polling islemi. Alt siniflar implement eder.
    /// </summary>
    protected abstract Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct);
}
