using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// PttAVM API stub helper'lari. PttAVM iki ayri API (Catalog + Shipment) ama
/// tek WireMock server uzerinden path-based dispatch yeterli.
/// Fixture JSON dosyalari: docs/wiremock/__files/pttavm/*.json
/// </summary>
public static class PttavmStubs
{
    public static void RegisterProductCreate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/product*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pttavm/product-create.json"));
    }

    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/stock-price*"))
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pttavm/stock-price-update.json"));
    }

    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/order*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pttavm/orders-page1.json"));
    }

    public static void RegisterShipping(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/shipping*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pttavm/shipping.json"));
    }

    public static void RegisterInvoice(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/invoice*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/pttavm/invoice.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterProductCreate(server);
        RegisterStockPriceUpdate(server);
        RegisterOrderImport(server);
        RegisterShipping(server);
        RegisterInvoice(server);
    }
}
