using Entegrasyon.IntegrationTest.Fixtures;
using Entegrasyon.MVC.Infrastructure.DevMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Entegrasyon.IntegrationTest.DevMode;

[Trait("Category", "Integration")]
public class DevWireMockSeederTests : IntegrationTestBase
{
    public DevWireMockSeederTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task SeedAsync_WhenTrendyolSellerIdConfigured_SetsSellerId()
    {
        await SeedMarketPlaceAsync(1, "Trendyol");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevMode:WireMockUrl"] = "http://wiremock-test:8080",
                ["DevMode:TrendyolSellerId"] = "999111"
            })
            .Build();

        await DevWireMockSeeder.SeedAsync(Services, config, NullLogger.Instance);

        using var db = CreateDbContext();
        var trendyol = await db.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == "Trendyol");

        trendyol.Should().NotBeNull();
        trendyol!.SellerId.Should().Be("999111");
        trendyol.BaseUrl.Should().Be("http://wiremock-test:8080");
    }

    [Fact]
    public async Task SeedAsync_WhenTrendyolSellerIdNotConfigured_DoesNotChangeSellerId()
    {
        await SeedMarketPlaceAsync(1, "Trendyol");

        using (var db = CreateDbContext())
        {
            var mp = await db.MarketPlaces.AsTracking().FirstAsync(m => m.Name == "Trendyol");
            mp.SellerId = "existing-seller";
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevMode:WireMockUrl"] = "http://wiremock-test:8080"
            })
            .Build();

        await DevWireMockSeeder.SeedAsync(Services, config, NullLogger.Instance);

        using var dbCheck = CreateDbContext();
        var trendyol = await dbCheck.MarketPlaces.AsNoTracking().FirstAsync(m => m.Name == "Trendyol");
        trendyol.SellerId.Should().Be("existing-seller");
    }
}
