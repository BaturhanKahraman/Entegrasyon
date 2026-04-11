using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Amazon SP-API stub helper'lari. OAuth 2.0 LWA + SP-API endpoint'leri
/// (Catalog, Listing, Feed, Order, Product, ProductType).
/// Fixture JSON dosyalari: docs/wiremock/__files/amazon/*.json
/// </summary>
public static class AmazonStubs
{
    public static void RegisterAuthToken(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*auth/o2/token*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/auth-token.json"));
    }

    public static void RegisterCatalog(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/catalog/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/catalog.json"));
    }

    public static void RegisterListing(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/listings/*"))
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/listing.json"));
    }

    public static void RegisterFeed(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/feeds/*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/feed.json"));
    }

    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/orders/*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/orders-page1.json"));
    }

    public static void RegisterProductType(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("/definitions/*productTypes*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/amazon/product-type.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterAuthToken(server);
        RegisterCatalog(server);
        RegisterListing(server);
        RegisterFeed(server);
        RegisterOrderImport(server);
        RegisterProductType(server);
    }
}
