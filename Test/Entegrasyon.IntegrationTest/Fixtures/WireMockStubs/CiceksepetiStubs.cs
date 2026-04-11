using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Ciceksepeti API stub helper'lari. Ciceksepeti REST API + x-api-key header auth.
///
/// NOT (Faz 3.7): Iskelet. Stub'lar Podman blokeri cozuldugunde doldurulacak.
/// </summary>
public static class CiceksepetiStubs
{
    /// <summary>
    /// Stub: Category tree (genelde buyuk JSON — Fixtures/Ciceksepeti/categories.json).
    /// </summary>
    public static void RegisterCategoryTree(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/categories.json
    }

    /// <summary>
    /// Stub: Product create/update endpoint'i.
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/product-create.json
    }

    /// <summary>
    /// Stub: Stock/price batch update.
    /// </summary>
    public static void RegisterStockPriceUpdate(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/stock-price-update.json
    }

    /// <summary>
    /// Stub: Order listesi.
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/orders-page1.json
    }

    /// <summary>
    /// Stub: Invoice gonderimi.
    /// </summary>
    public static void RegisterInvoice(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/invoice.json
    }

    /// <summary>
    /// Stub: Return (iade) islemleri.
    /// </summary>
    public static void RegisterReturn(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/return.json
    }

    /// <summary>
    /// Stub: QnA (soru-cevap) servisi.
    /// </summary>
    public static void RegisterQnA(WireMockServer server)
    {
        // TODO: Faz 3.7-sonrasi — Fixtures/Ciceksepeti/qna.json
    }
}
