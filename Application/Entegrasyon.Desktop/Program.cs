using Entegrasyon.Desktop.Data;
using Entegrasyon.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Photino.Blazor;

namespace Entegrasyon.Desktop;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
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

        // PrintAgent background service — Photino'da IHostedService yok,
        // ayri thread'de baslat
        appBuilder.Services.AddSingleton<PrintAgentHostedService>();

        appBuilder.RootComponents.Add<Components.Routes>("app");

        var app = appBuilder.Build();

        // DB olustur
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();
            db.Database.EnsureCreated();
        }

        // PrintAgent'i background'da baslat
        var printAgent = app.Services.GetRequiredService<PrintAgentHostedService>();
        _ = Task.Run(() => printAgent.StartAsync(CancellationToken.None));

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
