using Entegrasyon.Desktop.Data;
using Entegrasyon.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Photino.Blazor;
using Velopack;

namespace Entegrasyon.Desktop;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Velopack hook — MUTLAKA ilk satırda olmalı.
        // Installer/uninstaller/update sırasında çağrılır ve process'i yönetir.
        VelopackApp.Build().Run();

        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();

        // MudBlazor
        appBuilder.Services.AddMudServices();

        // SQLite
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entegrasyon", "offline.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        appBuilder.Services.AddDbContext<OfflineDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        appBuilder.Services.AddSingleton<SettingsService>();
        appBuilder.Services.AddSingleton<SyncService>();
        appBuilder.Services.AddScoped<OfflineSaleService>();
        appBuilder.Services.AddSingleton<UpdateService>();

        // PrintAgent background service
        appBuilder.Services.AddSingleton<PrintAgentHostedService>();

        appBuilder.RootComponents.Add<Components.Routes>("app");

        var app = appBuilder.Build();

        // DB oluştur
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();
            db.Database.EnsureCreated();
        }

        // PrintAgent'ı background'da başlat
        var printAgent = app.Services.GetRequiredService<PrintAgentHostedService>();
        _ = Task.Run(() => printAgent.StartAsync(CancellationToken.None));

        // Oto-güncelleme kontrolünü başlat
        var updateService = app.Services.GetRequiredService<UpdateService>();
        _ = Task.Run(() => updateService.CheckForUpdatesAsync());

        app.MainWindow
            .SetTitle("Entegrasyon Desktop")
            .SetUseOsDefaultSize(false)
            .SetSize(1400, 900)
            .SetIconFile("wwwroot/favicon.ico");

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Console.Error.WriteLine(error.ExceptionObject);
        };

        app.Run();
    }
}
