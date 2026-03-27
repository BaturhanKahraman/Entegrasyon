using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Entegrasyon.ApplicationBootstrap.Logger;

public static class SerilogExtensions
{
    public static IHostBuilder AddSerilogWithLoggerProvider(this WebApplicationBuilder @this,IConfiguration configuration) =>
        @this.Host.UseSerilog((hostingCtx, services, loggerConfiguration) => {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext();
        }, writeToProviders: true);
}
