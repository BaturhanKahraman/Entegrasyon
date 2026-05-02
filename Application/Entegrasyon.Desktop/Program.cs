using Entegrasyon.Desktop.Data;
using Entegrasyon.Desktop.Printing.Configuration;
using Entegrasyon.Desktop.Printing.Transport;
using Entegrasyon.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Photino.Blazor;
using System.Runtime.InteropServices;
using Velopack;

namespace Entegrasyon.Desktop;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var deepLinkUrl = args.FirstOrDefault(a => a.StartsWith("entegrasyon-print://", StringComparison.OrdinalIgnoreCase));

        // Single-instance kontrolü.
        // Primary instance MUTEX'i tutar + named pipe sunucusu çalıştırır.
        // İkinci tetikleme (UI veya deep-link) URL'i pipe ile primary'ye forward eder ve çıkar.
        var lockInstance = new SingleInstanceLock();
        var isPrimary = lockInstance.TryAcquire();

        if (!isPrimary)
        {
            // Bu, ikinci instance — primary'ye URL forward et (varsa) ve çık.
            if (deepLinkUrl is not null)
            {
                var forwarded = SingleInstanceLock.ForwardUrlAsync(deepLinkUrl).GetAwaiter().GetResult();
                if (!forwarded)
                    Console.Error.WriteLine("Birincil instance'a URL iletilemedi — primary muhtemelen kapalı.");
            }
            else
            {
                Console.Error.WriteLine("Entegrasyon Desktop zaten açık — bu instance kapatılıyor.");
            }
            lockInstance.Dispose();
            return;
        }

        // Primary instance — kilidi tutuyoruz.
        try
        {
            if (deepLinkUrl is not null)
            {
                // Doğrudan deep-link argümanıyla başlatıldı: headless yazdırma + çık.
                var parsed = DeepLinkHandler.TryParse(deepLinkUrl);
                if (parsed is null)
                {
                    Console.Error.WriteLine($"Geçersiz deep-link: {deepLinkUrl}");
                    return;
                }
                RunDeepLinkOnly(parsed).GetAwaiter().GetResult();
                return;
            }

            // Normal UI — pipe sunucusunu başlat ki ileride gelen deep-link'ler de bu process'e düşsün.
            lockInstance.StartServer();
            lockInstance.UrlReceived += url =>
            {
                var parsed = DeepLinkHandler.TryParse(url);
                if (parsed is null) return;
                _ = Task.Run(() => RunDeepLinkOnly(parsed));
            };

            RunUiApp(args);
        }
        finally
        {
            lockInstance.Dispose();
        }
    }

    private static async Task RunDeepLinkOnly(DeepLinkInvocation invocation)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
        services.AddSingleton<DeviceCredentialStore>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IPrinterTransport, RawTcpPrinterTransport>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            services.AddSingleton<ILocalPrinterTransport, WindowsUsbPrinterTransport>();
        else
            services.AddSingleton<ILocalPrinterTransport, CupsPrinterTransport>();
        services.AddSingleton<DeepLinkHandler>();

        // Yazıcı konfigürasyonu — varsayılan değerlerle (printers.json yoksa)
        services.Configure<PrinterConfiguration>(c =>
        {
            c.Printers =
            [
                new PrinterEntry { Name = "Varsayilan-Yazici", Type = "Network", Address = "192.168.1.50", Port = 9100, IsDefault = true }
            ];
        });

        await using var sp = services.BuildServiceProvider();
        var handler = sp.GetRequiredService<DeepLinkHandler>();
        await handler.HandleAsync(invocation);
    }

    private static void RunUiApp(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        appBuilder.Services.AddMudServices();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Entegrasyon", "offline.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        appBuilder.Services.AddDbContext<OfflineDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        appBuilder.Services.AddSingleton<SettingsService>();
        appBuilder.Services.AddSingleton<SyncService>();
        appBuilder.Services.AddScoped<OfflineSaleService>();
        appBuilder.Services.AddSingleton<UpdateService>();
        appBuilder.Services.AddSingleton<DeviceCredentialStore>();
        appBuilder.Services.AddSingleton<UrlProtocolRegistrar>();

        appBuilder.RootComponents.Add<Components.Routes>("app");

        var app = appBuilder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();
            db.Database.EnsureCreated();
        }

        // URL protocol kaydını ilk açılışta garanti altına al
        app.Services.GetRequiredService<UrlProtocolRegistrar>().RegisterIfMissing();

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
