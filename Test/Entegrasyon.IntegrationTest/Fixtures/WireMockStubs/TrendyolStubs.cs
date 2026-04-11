using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Trendyol API stub helper'lari. Integration testler ihtiyac duyduklari
/// endpoint'leri bu sinif araciligi ile WireMock'a kaydeder.
///
/// Kullanim ornegi (integration test OnInitializeAsync icinde):
///   TrendyolStubs.RegisterOrderImport(WireMock.Server);
///   TrendyolStubs.RegisterProductCreate(WireMock.Server);
///
/// Fixture JSON dosyalari: Fixtures/Trendyol/*.json (WireMock.Net WithBodyFromFile
/// convention). Dosyalar csproj CopyToOutputDirectory="PreserveNewest" ile
/// bin/Debug/.../Fixtures/Trendyol/ altina kopyalanir, WireMock server startup'ta
/// RootFolder bu path'e set edilir.
///
/// NOT (Faz 3.1): Bu sinif su an ISKELET. Integration testler Podman/Testcontainers
/// blokeri nedeniyle Fedora'da calismadigi icin stub'larin gerceklestirilmesi
/// erteleniyor — bloker cozuldugunde her integration test yazilirken stub'lar
/// burada doldurulacak. TrendyolProductService, TrendyolOrderService,
/// TrendyolStockPriceService, TrendyolInvoiceService ve TrendyolMarketplaceSearchService'in
/// gercek HTTP call'lari oneri olarak asagidaki metod iskeletlerinde listelenmis.
/// </summary>
public static class TrendyolStubs
{
    /// <summary>
    /// Stub: GET /suppliers/{sellerId}/orders — Trendyol sipariş sorgulama
    /// (TrendyolOrderService.FetchOrdersAsync icin).
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server, string sellerId = "12345")
    {
        // TODO: Faz 3.1-sonrasi — Fixtures/Trendyol/orders-page1.json dosyasini yaz
        // ve asagidaki stub'i aktif et.
        //
        // server
        //     .Given(Request.Create()
        //         .WithPath($"/suppliers/{sellerId}/orders")
        //         .UsingGet())
        //     .RespondWith(Response.Create()
        //         .WithStatusCode(200)
        //         .WithHeader("Content-Type", "application/json")
        //         .WithBodyFromFile("Fixtures/Trendyol/orders-page1.json"));
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/v2/products — ürün oluşturma / toplu
    /// (TrendyolProductService.AddProductAsync icin). Dönen response batchRequestId icerir.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server, string sellerId = "12345")
    {
        // TODO: Faz 3.1-sonrasi — Fixtures/Trendyol/product-create.json
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/products/price-and-inventory — stok/fiyat güncellemesi
    /// (TrendyolStockPriceService.UpdateStockPriceAsync icin).
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server, string sellerId = "12345")
    {
        // TODO: Faz 3.1-sonrasi — Fixtures/Trendyol/stock-price-update.json
    }

    /// <summary>
    /// Stub: GET /product/brands/by-name — marka arama
    /// (TrendyolMarketplaceSearchService.SearchBrandAsync icin).
    /// </summary>
    public static void RegisterBrandSearch(WireMockServer server)
    {
        // TODO: Faz 3.1-sonrasi — Fixtures/Trendyol/brand-search.json
    }

    /// <summary>
    /// Stub: POST /suppliers/{sellerId}/invoice-link — fatura link gonderme
    /// (TrendyolInvoiceService.SendInvoiceLinkAsync icin).
    /// </summary>
    public static void RegisterInvoiceLink(WireMockServer server, string sellerId = "12345")
    {
        // TODO: Faz 3.1-sonrasi — Fixtures/Trendyol/invoice-link.json
    }
}
