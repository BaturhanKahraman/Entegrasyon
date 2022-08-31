using Microsoft.Extensions.DependencyInjection;

namespace TrendyolBackgroundJob;

public static class TrendyolExtension
{
    public static IServiceCollection AddTrendyolBackgroundService(this IServiceCollection service)
    {
        service.AddHostedService<TrendyolBackgroundService>();
        return service;
    }
}