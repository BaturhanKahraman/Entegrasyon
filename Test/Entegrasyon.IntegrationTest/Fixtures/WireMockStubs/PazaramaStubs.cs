using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Pazarama API stub helper'lari. Integration testler ihtiyac duyduklari
/// endpoint'leri bu sinif araciligi ile WireMock'a kaydeder.
///
/// NOT (Faz 3.4): Iskelet. Stub'lar Podman blokeri cozuldugunde doldurulacak.
/// PazaramaApiClient (OAuth2 token), PazaramaProductService,
/// PazaramaStockPriceService, PazaramaOrderService ve PazaramaRefundService'in
/// gercek HTTP call'lari asagidaki metod iskeletlerinde.
/// </summary>
public static class PazaramaStubs
{
    /// <summary>
    /// Stub: OAuth2 token endpoint'i — PazaramaApiClient.EnsureValidTokenAsync icin.
    /// </summary>
    public static void RegisterAuthToken(WireMockServer server)
    {
        // TODO: Faz 3.4-sonrasi — Fixtures/Pazarama/auth-token.json
    }

    /// <summary>
    /// Stub: Product create/update endpoint'i.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        // TODO: Faz 3.4-sonrasi — Fixtures/Pazarama/product-create.json
    }

    /// <summary>
    /// Stub: Stock/price update endpoint'i.
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        // TODO: Faz 3.4-sonrasi — Fixtures/Pazarama/stock-price-update.json
    }

    /// <summary>
    /// Stub: Order listesi endpoint'i.
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.4-sonrasi — Fixtures/Pazarama/orders-page1.json
    }

    /// <summary>
    /// Stub: Refund (iade) endpoint'i.
    /// </summary>
    public static void RegisterRefund(WireMockServer server)
    {
        // TODO: Faz 3.4-sonrasi — Fixtures/Pazarama/refund.json
    }
}
