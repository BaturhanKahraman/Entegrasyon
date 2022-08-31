using Microsoft.Extensions.Hosting;

namespace TrendyolBackgroundJob;

public class TrendyolBackgroundService : BackgroundService
{
    public TrendyolBackgroundService():base()
    {
        
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}