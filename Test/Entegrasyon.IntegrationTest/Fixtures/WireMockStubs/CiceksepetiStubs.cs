using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Ciceksepeti API stub helper'lari. x-api-key header auth.
/// Fixture JSON dosyalari: docs/wiremock/__files/ciceksepeti/*.json
/// </summary>
public static class CiceksepetiStubs
{
    public static void RegisterCategoryTree(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/categories*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/ciceksepeti/categories.json"));
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
                .WithBodyFromFile("Fixtures/ciceksepeti/product-create.json"));
    }

    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/stockPrice*"))
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/ciceksepeti/stock-price-update.json"));
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
                .WithBodyFromFile("Fixtures/ciceksepeti/orders-page1.json"));
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
                .WithBodyFromFile("Fixtures/ciceksepeti/invoice.json"));
    }

    public static void RegisterReturn(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/returns*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/ciceksepeti/return.json"));
    }

    public static void RegisterQnA(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/questions*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/ciceksepeti/qna.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterCategoryTree(server);
        RegisterProductCreate(server);
        RegisterStockPriceUpdate(server);
        RegisterOrderImport(server);
        RegisterInvoice(server);
        RegisterReturn(server);
        RegisterQnA(server);
    }
}
