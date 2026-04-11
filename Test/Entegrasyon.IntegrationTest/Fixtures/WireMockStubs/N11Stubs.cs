using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// N11 REST API stub helper'lari. SOAP legacy stack ayri handle edilir.
/// Fixture JSON dosyalari: docs/wiremock/__files/n11/*.json
/// </summary>
public static class N11Stubs
{
    public static void RegisterProductCreate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/ms/product*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/n11/product-create.json"));
    }

    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/ms/product/stock-price*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/n11/stock-price-update.json"));
    }

    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/ms/order/tasklet/orders*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/n11/orders-page1.json"));
    }

    public static void RegisterClaim(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/ms/claim*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/n11/claim.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterProductCreate(server);
        RegisterStockPriceUpdate(server);
        RegisterOrderImport(server);
        RegisterClaim(server);
    }
}
