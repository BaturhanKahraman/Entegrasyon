using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Entegrasyon.IntegrationTest.Fixtures.WireMockStubs;

/// <summary>
/// Temu API stub helper'lari. Temu "tek router endpoint" pattern'i kullanir:
/// POST /openapi/router → body "type" alaninda dispatch. Farkli type'lar icin
/// farkli stub'lar gerekebilir (product.create, order.list vs.) — simdilik
/// generic 200 response.
/// </summary>
public static class TemuStubs
{
    public static void RegisterRouter(WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithPath(new WildcardMatcher("*/openapi/router*"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Fixtures/temu/router-default.json"));
    }

    public static void RegisterAll(WireMockServer server)
    {
        RegisterRouter(server);
    }
}
