using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Hepsiburada API stub helper'lari. Integration testler ihtiyac duyduklari
/// endpoint'leri bu sinif araciligi ile WireMock'a kaydeder.
///
/// NOT (Faz 3.2): Su an iskelet. Stub'lar Podman blokeri cozuldugunde
/// doldurulacak. HepsiburadaProductService, HepsiburadaListingService,
/// HepsiburadaOrderService, HepsiburadaClaimService ve HepsiburadaQnAService'in
/// gercek HTTP call'lari oneri olarak asagidaki metod iskeletlerinde listelenmis.
/// </summary>
public static class HepsiburadaStubs
{
    /// <summary>
    /// Stub: Listing servisi — urun listeleme (HepsiburadaListingService).
    /// </summary>
    public static void RegisterListing(WireMockServer server)
    {
        // TODO: Faz 3.2-sonrasi — Fixtures/Hepsiburada/listing.json
    }

    /// <summary>
    /// Stub: Order servisi — siparis import (HepsiburadaOrderService).
    /// </summary>
    public static void RegisterOrderImport(WireMockServer server)
    {
        // TODO: Faz 3.2-sonrasi — Fixtures/Hepsiburada/orders-page1.json
    }

    /// <summary>
    /// Stub: Product servisi — urun yonetimi (HepsiburadaProductService).
    /// </summary>
    public static void RegisterProductCreate(WireMockServer server)
    {
        // TODO: Faz 3.2-sonrasi — Fixtures/Hepsiburada/product-create.json
    }

    /// <summary>
    /// Stub: Claim servisi — iade/sikayet islemleri (HepsiburadaClaimService).
    /// </summary>
    public static void RegisterClaim(WireMockServer server)
    {
        // TODO: Faz 3.2-sonrasi — Fixtures/Hepsiburada/claim.json
    }

    /// <summary>
    /// Stub: QnA servisi — urun soru-cevap (HepsiburadaQnAService).
    /// </summary>
    public static void RegisterQnA(WireMockServer server)
    {
        // TODO: Faz 3.2-sonrasi — Fixtures/Hepsiburada/qna.json
    }
}
