using Entegrasyon.Business.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Entegrasyon.Business.BackgroundServices;

public class TempBarcodeBackgroundService:BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TempBarcodeBackgroundService( IServiceProvider serviceProvider,IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int delayInMinute = _configuration.GetValue("Barcode:ClearDelay", 10);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(delayInMinute),stoppingToken);
            //await Task.Delay(TimeSpan.FromSeconds(10),stoppingToken);
            using var serviceScope = _serviceProvider.CreateScope();
            var barcodeManager = serviceScope.ServiceProvider.GetRequiredService<TempBarcodeManager>();
            await barcodeManager.ClearAddedBarcodes();
            await barcodeManager.CorrectAddables();
        }
    }
}