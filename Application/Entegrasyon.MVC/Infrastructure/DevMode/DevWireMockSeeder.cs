using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.MVC.Infrastructure.DevMode;

/// <summary>
/// Development ortaminda DB'deki MarketPlace kaytlarini WireMock'a yonlendirir.
/// MVC uygulamasi startup'ta calistirilir (sadece Development).
///
/// Yapilan isler:
///   1. BaseUrl → DevMode:WireMockUrl (tum marketplace'ler)
///   2. Trendyol SellerId → DevMode:TrendyolSellerId (dev DB'de null olabilir)
///   3. Trendyol ApiKey/ApiSecret → bos ise dummy (WireMock dogrulamaz; IsCredentialComplete()
///      true olsun diye — gercek creds varsa dokunulmaz)
///
/// Gercek sandbox API'ye gitmek isteyen marketplace icin
/// appsettings.Development.json'da RealApiMarketplaces listesine adi ekle —
/// seeder o marketplace'e dokunmaz.
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
        var testSellerId = configuration["DevMode:TrendyolSellerId"];

        try
        {
            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = contextFactory.CreateDbContext();

            // AsTracking ŞART: context global no-tracking (TenantDbContextFactory) →
            // tracking'siz okunan entity'de mp.BaseUrl değişikliği SaveChanges'te no-op olur.
            var marketplaces = await db.MarketPlaces.AsTracking().ToListAsync();
            var updatedIds = new HashSet<int>();

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
                    updatedIds.Add(mp.Id);
                    logger.LogInformation(
                        "DevWireMockSeeder: {Marketplace} BaseUrl {Previous} → {New}",
                        mp.Name, previous, wireMockUrl);
                }

                if (mp.Id == MarketPlaceConstants.TrendyolMarketPlaceId)
                {
                    // SellerId: config'ten gelirse uygula; yoksa bos ise dummy ata.
                    if (!string.IsNullOrEmpty(testSellerId) && mp.SellerId != testSellerId)
                    {
                        mp.SellerId = testSellerId;
                        updatedIds.Add(mp.Id);
                        logger.LogInformation("DevWireMockSeeder: Trendyol SellerId → {SellerId}", testSellerId);
                    }
                    else if (string.IsNullOrEmpty(mp.SellerId))
                    {
                        mp.SellerId = "dev-seller";
                        updatedIds.Add(mp.Id);
                    }

                    // ApiKey/ApiSecret: WireMock credential dogrulamaz; IsCredentialComplete() true
                    // olsun (sayfa/gating dev'de gercekci davransin) diye bos ise dummy ata.
                    // Gercek creds varsa DOKUNMA.
                    if (string.IsNullOrEmpty(mp.ApiKey))
                    {
                        mp.ApiKey = configuration["DevMode:TrendyolApiKey"] ?? "dev-wiremock-key";
                        updatedIds.Add(mp.Id);
                    }
                    if (string.IsNullOrEmpty(mp.ApiSecret))
                    {
                        mp.ApiSecret = configuration["DevMode:TrendyolApiSecret"] ?? "dev-wiremock-secret";
                        updatedIds.Add(mp.Id);
                    }
                }
            }

            // Stok kaynağı garantisi (T109): Trendyol'a stok gönderilebilmesi için en az bir depo
            // eşlenmiş olmalı. Dev DB'de ne MarketPlaceWarehouse ne IsDefaultMarketPlaceStock depo
            // varsa, ürünler quantity:0 ile gider (satılamaz). Bu durumda ilk depoyu varsayılan
            // pazaryeri stok kaynağı işaretle ki dev'de gerçek stok gönderimi test edilebilsin.
            var hasTrendyolWarehouse = await db.MarketPlaceWarehouses
                .AnyAsync(w => w.MarketPlaceId == MarketPlaceConstants.TrendyolMarketPlaceId);
            var hasDefaultStockBranch = await db.BranchOffices.AnyAsync(b => b.IsDefaultMarketPlaceStock);
            if (!hasTrendyolWarehouse && !hasDefaultStockBranch)
            {
                var firstBranch = await db.BranchOffices.AsTracking()
                    .OrderBy(b => b.Id).FirstOrDefaultAsync();
                if (firstBranch is not null)
                {
                    firstBranch.IsDefaultMarketPlaceStock = true;
                    await db.SaveChangesAsync();
                    logger.LogInformation(
                        "DevWireMockSeeder: Stok kaynağı yoktu — '{Branch}' (Id={Id}) varsayılan pazaryeri stok deposu işaretlendi.",
                        firstBranch.Name, firstBranch.Id);
                }
                else
                {
                    logger.LogWarning("DevWireMockSeeder: Hiç depo yok — Trendyol gönderiminde quantity:0 riski sürüyor.");
                }
            }

            if (updatedIds.Count > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("DevWireMockSeeder: {Count} marketplace guncellendi.", updatedIds.Count);
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
