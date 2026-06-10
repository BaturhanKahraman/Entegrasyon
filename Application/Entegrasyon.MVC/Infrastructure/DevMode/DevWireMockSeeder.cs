using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.MVC.Infrastructure.DevMode;

/// <summary>
/// Development ortaminda DB'deki MarketPlace.BaseUrl alanini WireMock URL'ine
/// update eder. MVC uygulamasi startup'ta calistirilir (sadece Development).
///
/// Amac: Lokal gelistirici ortaminda internetsiz calisabilmek. Gelistirici
/// docker-compose.dev.yml ile WireMock container'i baslatir, MVC DevMode:WireMockUrl
/// config'ini kullanarak DB'deki marketplace BaseUrl'lerini WireMock'a cevirir.
/// Boylece TrendyolApiClient, HepsiburadaApiClient vs. HTTP cagrilari gercek API
/// yerine lokal WireMock'a dusurur.
///
/// Gercek sandbox API'ye gitmek isteyen bir marketplace icin
/// appsettings.Development.json'da:
///   "DevMode": {
///     "WireMockUrl": "http://wiremock:8080",
///     "RealApiMarketplaces": ["Trendyol"]
///   }
/// listeye marketplace adi eklenir — seeder o marketplace'in BaseUrl'ine dokunmaz.
///
/// GUVENLIK: Sadece IsDevelopment() kosulunda calistirilir. Staging/Production'da
/// calistirilirsa production DB'de marketplace URL'lerini bozar — bu yuzden Program.cs
/// entegrasyonunda explicit environment check yapilmalidir.
/// </summary>
public static class DevWireMockSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
    {
        var wireMockUrl = configuration["DevMode:WireMockUrl"];
        if (string.IsNullOrWhiteSpace(wireMockUrl))
        {
            logger.LogDebug("DevWireMockSeeder: DevMode:WireMockUrl tanimli degil, atlaniyor.");
            return;
        }

        // Gercek API'ye gitmesi istenen marketplace'ler — seeder bunlara dokunmaz
        var realApiMarketplaces = configuration.GetSection("DevMode:RealApiMarketplaces").Get<string[]>() ?? [];

        try
        {
            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = contextFactory.CreateDbContext();

            // AsTracking ŞART: context global no-tracking (TenantDbContextFactory) →
            // tracking'siz okunan entity'de mp.BaseUrl değişikliği SaveChanges'te no-op olur.
            var marketplaces = await db.MarketPlaces.AsTracking().ToListAsync();
            var updated = 0;

            foreach (var mp in marketplaces)
            {
                // RealApiMarketplaces listesinde yer aliyorsa dokunma
                if (realApiMarketplaces.Contains(mp.Name, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogDebug(
                        "DevWireMockSeeder: {Marketplace} gercek API listesinde — BaseUrl korunuyor ({BaseUrl})",
                        mp.Name, mp.BaseUrl);
                    continue;
                }

                if (mp.BaseUrl != wireMockUrl)
                {
                    var previous = mp.BaseUrl;
                    mp.BaseUrl = wireMockUrl;
                    updated++;
                    logger.LogInformation(
                        "DevWireMockSeeder: {Marketplace} BaseUrl {Previous} → {New}",
                        mp.Name, previous, wireMockUrl);
                }
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("DevWireMockSeeder: {Count} marketplace BaseUrl'i WireMock URL'ine guncellendi.", updated);
            }
            else
            {
                logger.LogDebug("DevWireMockSeeder: Tum marketplace'ler zaten WireMock'a yonlendirilmis, degisiklik yok.");
            }
        }
        catch (Exception ex)
        {
            // DB baglantisi yoksa (bazi gelistiriciler DB container'i ayri calistirir)
            // seeder fail olmasin — log'la ve gec. MVC normal calismaya devam etsin.
            logger.LogWarning(ex, "DevWireMockSeeder: DB update başarısız, gecillenecek. Hata: {Message}", ex.Message);
        }
    }
}
