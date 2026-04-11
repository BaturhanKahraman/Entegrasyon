using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Temu API stub helper'lari. Temu "tek router endpoint" uzerinden tum API
/// cagrilarini POST /openapi/router ile yapar — request body "type" alani
/// ile dispatch edilir (product.create, order.list vs.). Stub'lar bu yapiya
/// uygun olmali — body matching ile dispatch simule edilmeli.
///
/// NOT (Faz 3.8): Iskelet. Stub'lar Podman blokeri cozuldugunde doldurulacak.
/// </summary>
public static class TemuStubs
{
    /// <summary>
    /// Stub: Router endpoint'i — tum type'lar icin generic 200 response.
    /// Gelismis testler body matching ile type bazli farkli response donecek.
    /// </summary>
    public static void RegisterRouter(WireMockServer server)
    {
        // TODO: Faz 3.8-sonrasi — Fixtures/Temu/router-default.json
        // Ileri seviye: Fixtures/Temu/router-{type}.json (product-create, order-list vs.)
    }
}
