using CommunityToolkit.Maui;
using Entegrasyon.Desktop.Data;
using Entegrasyon.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Entegrasyon.Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // MudBlazor
        builder.Services.AddMudServices();

        // SQLite offline database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "entegrasyon-offline.db");
        builder.Services.AddDbContext<OfflineDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Connectivity monitoring
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

        // Sync service
        builder.Services.AddSingleton<SyncService>();

        // Offline sale service
        builder.Services.AddScoped<OfflineSaleService>();

        // Settings service
        builder.Services.AddSingleton<SettingsService>();

        // PrintAgent hosted service (embedded background web API)
        builder.Services.AddSingleton<PrintAgentHostedService>();

        return builder.Build();
    }
}
