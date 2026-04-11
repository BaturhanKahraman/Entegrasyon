using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Trendyol API stub helper'lari. Integration testler ihtiyac duyduklari
/// endpoint'leri bu sinif araciligi ile WireMock'a kaydeder.
///
/// Fixture JSON dosyalari: docs/wiremock/__files/trendyol/*.json (tek kaynak —
/// dev mode container ve integration test paylasir). csproj linked content
/// ile test output'a kopyalanir: bin/Debug/net10.0/Fixtures/trendyol/*.json
/// </summary>
public static class TrendyolStubs
{
    /// <summary>
    /// Stub: GET /suppliers/{sellerId}/orders — Trendyol sipariş sorgulama
    /// (TrendyolOrderService.FetchOrdersAsync icin).
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.WildcardMatcher("/suppliers/*/orders*"))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/trendyol/orders-page1.json"));
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/v2/products — ürün oluşturma / toplu
    /// (TrendyolProductService.AddProductAsync icin). Dönen response batchRequestId icerir.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.WildcardMatcher("/suppliers/*/v2/products"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/trendyol/product-create.json"));
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/products/price-and-inventory — stok/fiyat güncellemesi
    /// (TrendyolStockPriceService.UpdateStockPriceAsync icin).
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.WildcardMatcher("/suppliers/*/products/price-and-inventory"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/trendyol/stock-price-update.json"));
    }

    /// <summary>
    /// Stub: GET /product/brands/by-name — marka arama
    /// (TrendyolMarketplaceSearchService.SearchBrandAsync icin).
    /// </summary>
    public static void RegisterBrandSearch(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath("/product/brands/by-name")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/trendyol/brand-search.json"));
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/invoice-link — fatura link gonderme
    /// (TrendyolInvoiceService.SendInvoiceLinkAsync icin).
    /// </summary>
    public static void RegisterInvoiceLink(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.WildcardMatcher("/suppliers/*/invoice-link"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/trendyol/invoice-link.json"));
    }

    /// <summary>
    /// Tum Trendyol endpoint'lerini tek hamlede kaydeder — integration test
    /// setup'inda kolaylik icin.
    /// </summary>
    public static void RegisterAll(WireMockServer server)
    {
        RegisterOrderImport(server);
        RegisterProductCreate(server);
        RegisterStockPriceUpdate(server);
        RegisterBrandSearch(server);
        RegisterInvoiceLink(server);
    }
}
