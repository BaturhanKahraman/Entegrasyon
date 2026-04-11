using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Pazarama API stub helper'lari. OAuth2 Bearer token auth + REST API.
/// Fixture JSON dosyalari: docs/wiremock/__files/pazarama/*.json
/// </summary>
public static class PazaramaStubs
{
    public static void RegisterAuthToken(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*oauth/token*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pazarama/auth-token.json"));
    }

    public static void RegisterProductCreate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/products*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pazarama/product-create.json"));
    }

    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/stockPrice*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pazarama/stock-price-update.json"));
    }

    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/orders*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pazarama/orders-page1.json"));
    }

    public static void RegisterRefund(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/refund*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pazarama/refund.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterAuthToken(server);
        RegisterProductCreate(server);
        RegisterStockPriceUpdate(server);
        RegisterOrderImport(server);
        RegisterRefund(server);
    }
}
