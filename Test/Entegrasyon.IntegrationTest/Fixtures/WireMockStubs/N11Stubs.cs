using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// N11 API stub helper'lari. N11'in hem REST (varsayilan) hem SOAP (legacy)
/// stack'i var — stub'lar REST endpoint'leri hedefler. SOAP legacy fallback
/// ayri stub set gerektirir.
///
/// NOT (Faz 3.3): Iskelet. Stub'lar Podman blokeri cozuldugunde doldurulacak.
/// N11RestProductService, N11RestStockPriceService, N11RestOrderService ve
/// N11ClaimService gercek HTTP call'lari asagida listelenmis.
/// </summary>
public static class N11Stubs
{
    /// <summary>
    /// Stub: N11 REST product create/update endpoint'i.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        // TODO: Faz 3.3-sonrasi — Fixtures/N11/product-create.json
    }

    /// <summary>
    /// Stub: N11 REST stock/price update endpoint'i.
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        // TODO: Faz 3.3-sonrasi — Fixtures/N11/stock-price-update.json
    }

    /// <summary>
    /// Stub: N11 REST order listesi endpoint'i
    /// (/ms/order/tasklet/orders-v2 pattern).
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.3-sonrasi — Fixtures/N11/orders-page1.json
    }

    /// <summary>
    /// Stub: N11 claim (iade) servisi.
    /// </summary>
    public static void RegisterClaim(WireMockServer server)
    {
        // TODO: Faz 3.3-sonrasi — Fixtures/N11/claim.json
    }
}
