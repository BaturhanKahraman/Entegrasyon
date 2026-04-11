using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// PttAVM API stub helper'lari. PttAVM iki ayri API base URL'ine sahip —
/// Catalog API (urun/stok/fiyat) ve Shipment API (kargo/siparis/fatura).
///
/// NOT (Faz 3.6): Iskelet. Stub'lar Podman blokeri cozuldugunde doldurulacak.
/// </summary>
public static class PttavmStubs
{
    /// <summary>
    /// Stub: Catalog API — urun yonetimi.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        // TODO: Faz 3.6-sonrasi — Fixtures/Pttavm/product-create.json
    }

    /// <summary>
    /// Stub: Catalog API — stok/fiyat guncelleme.
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        // TODO: Faz 3.6-sonrasi — Fixtures/Pttavm/stock-price-update.json
    }

    /// <summary>
    /// Stub: Shipment API — siparis listesi.
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.6-sonrasi — Fixtures/Pttavm/orders-page1.json
    }

    /// <summary>
    /// Stub: Shipment API — kargo takip/guncelleme.
    /// </summary>
    public static void RegisterShipping(WireMockServer server)
    {
        // TODO: Faz 3.6-sonrasi — Fixtures/Pttavm/shipping.json
    }

    /// <summary>
    /// Stub: Shipment API — fatura gonderimi.
    /// </summary>
    public static void RegisterInvoice(WireMockServer server)
    {
        // TODO: Faz 3.6-sonrasi — Fixtures/Pttavm/invoice.json
    }
}
