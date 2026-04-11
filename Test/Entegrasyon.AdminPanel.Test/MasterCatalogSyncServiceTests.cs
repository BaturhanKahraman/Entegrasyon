using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

namespace Entegrasyon.AdminPanel.Test;

public class MasterCatalogSyncServiceTests : TestBase
{
    private const int TrendyolMarketplaceId = 1;

    private MasterCatalogSyncService CreateService(string apiResponseJson)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apiResponseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("TrendyolPublic")).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton(DbContext);
        services.AddSingleton(httpClientFactory.Object);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new MasterCatalogSyncService(scopeFactory, NullLogger<MasterCatalogSyncService>.Instance);
    }

    [Fact]
    public async Task SyncAsync_ShouldInsertNewCategoriesWithHierarchy()
    {
        var json = """
        {
          "categories": [
            { "id": 100, "name": "Giyim", "subCategories": [
                { "id": 101, "name": "Kadın Elbise", "subCategories": [] }
              ]
            },
            { "id": 200, "name": "Elektronik", "subCategories": [] }
          ]
        }
        """;
        var service = CreateService(json);

        await service.SyncAsync(CancellationToken.None);

        var refs = DbContext.MarketplaceReferences
            .Where(r => r.MarketplaceId == TrendyolMarketplaceId &&
                        r.EntityType == MarketplaceEntityType.Category)
            .ToList();

        refs.Should().HaveCount(3);
        refs.Should().ContainSingle(r => r.ExternalId == "100" && r.Name == "Giyim" && r.ParentExternalId == null);
        refs.Should().ContainSingle(r => r.ExternalId == "101" && r.Name == "Kadın Elbise" && r.ParentExternalId == "100");
        refs.Should().ContainSingle(r => r.ExternalId == "200" && r.Name == "Elektronik");
        refs.Should().OnlyContain(r => r.IsActive);
    }

    [Fact]
    public async Task SyncAsync_ShouldUpdateNameWhenChanged()
    {
        // İlk sync
        var initialJson = """
        {"categories":[{"id":100,"name":"Giyim","subCategories":[]}]}
        """;
        await CreateService(initialJson).SyncAsync(CancellationToken.None);

        // İkinci sync — isim değişmiş
        var updatedJson = """
        {"categories":[{"id":100,"name":"Kıyafet","subCategories":[]}]}
        """;
        await CreateService(updatedJson).SyncAsync(CancellationToken.None);

        var reference = DbContext.MarketplaceReferences
            .Single(r => r.ExternalId == "100");
        reference.Name.Should().Be("Kıyafet");
        reference.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAsync_ShouldDeactivateCategoriesMissingFromApi()
    {
        var initialJson = """
        {"categories":[
          {"id":100,"name":"Giyim","subCategories":[]},
          {"id":200,"name":"Elektronik","subCategories":[]}
        ]}
        """;
        await CreateService(initialJson).SyncAsync(CancellationToken.None);

        // İkinci sync — Elektronik kaldırılmış
        var shrunkJson = """
        {"categories":[{"id":100,"name":"Giyim","subCategories":[]}]}
        """;
        await CreateService(shrunkJson).SyncAsync(CancellationToken.None);

        var elektronik = DbContext.MarketplaceReferences.Single(r => r.ExternalId == "200");
        elektronik.IsActive.Should().BeFalse();

        var giyim = DbContext.MarketplaceReferences.Single(r => r.ExternalId == "100");
        giyim.IsActive.Should().BeTrue();
    }
}
