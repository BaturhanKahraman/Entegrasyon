using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Hepsiburada API stub helper'lari. Fixture JSON dosyalari:
/// docs/wiremock/__files/hepsiburada/*.json
/// </summary>
public static class HepsiburadaStubs
{
    public static void RegisterListing(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/listings/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/hepsiburada/listing.json"));
    }

    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/orders/merchantid/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/hepsiburada/orders-page1.json"));
    }

    public static void RegisterProductCreate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/product/api/products/*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/hepsiburada/product-create.json"));
    }

    public static void RegisterClaim(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/claims/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/hepsiburada/claim.json"));
    }

    public static void RegisterQnA(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/qna/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/hepsiburada/qna.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterListing(server);
        RegisterOrderImport(server);
        RegisterProductCreate(server);
        RegisterClaim(server);
        RegisterQnA(server);
    }
}
