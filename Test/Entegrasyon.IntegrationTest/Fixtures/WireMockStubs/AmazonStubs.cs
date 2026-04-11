using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Amazon SP-API stub helper'lari.
///
/// NOT (Faz 3.5): Iskelet. Amazon SP-API OAuth 2.0 token akisi + SP-API endpoint'leri
/// (Catalog, Listing, Feed, Order, Product, ProductType). Stub'lar Podman blokeri
/// cozuldugunde doldurulacak.
/// </summary>
public static class AmazonStubs
{
    /// <summary>
    /// Stub: OAuth 2.0 token endpoint'i (LWA — Login with Amazon).
    /// </summary>
    public static void RegisterAuthToken(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/auth-token.json
    }

    /// <summary>
    /// Stub: Catalog API — urun katalog sorgulama.
    /// </summary>
    public static void RegisterCatalog(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/catalog.json
    }

    /// <summary>
    /// Stub: Listing API — urun listeleme/guncelleme.
    /// </summary>
    public static void RegisterListing(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/listing.json
    }

    /// <summary>
    /// Stub: Feed API — toplu urun gonderimi.
    /// </summary>
    public static void RegisterFeed(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/feed.json
    }

    /// <summary>
    /// Stub: Orders API — siparis listesi.
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/orders-page1.json
    }

    /// <summary>
    /// Stub: ProductType API — urun tipi metadata.
    /// </summary>
    public static void RegisterProductType(WireMockServer server)
    {
        // TODO: Faz 3.5-sonrasi — Fixtures/Amazon/product-type.json
    }
}
