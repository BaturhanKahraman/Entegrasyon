using System.Net;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Entegrasyon.Test.Fixtures;

/// <summary>
/// Faz 0 smoke test — WireMockFixture'in dogru kuruldugunu ve basic stub/response
/// akisinin calistigini dogrular. Herhangi bir gercek marketplace API'sine dokunmaz.
///
/// Eger bu test fail ederse, Faz 1'deki hicbir WireMock-tabanli test calismaz.
/// Oncelikli debug noktasi.
/// </summary>
[Collection(WireMockCollection.Name)]
public sealed class WireMockSmokeTest(WireMockFixture wm)
{
    [Fact]
    public async Task Server_Starts_And_Responds_With_Header_And_Log_Verification()
    {
        // Arrange: Fixture class'lar arasi paylasildigi icin, her test Başlangıçinda
        // onceki class'larin stub'larini temizlemek gerekir.
        wm.ResetAll();

        // Stub: GET /ping, Authorization header "Basic smoke-token" bekliyor → 200 "pong"
        wm.Server
            .Given(Request.Create()
                .WithPath("/ping")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("pong"));

        // Act: Faz 1'deki gercek client'lar gibi, HttpClient.BaseAddress set edip
        // Authorization header ekleyerek istek at
        using var client = new HttpClient { BaseAddress = new Uri(wm.BaseUrl) };
        client.DefaultRequestHeaders.Add("Authorization", "Basic smoke-token");

        var response = await client.GetAsync("/ping");
        var body = await response.Content.ReadAsStringAsync();

        // Assert 1: Response dogrulugu
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("pong");

        // Assert 2: Log entries verification — Faz 1'de header/body assertion icin
        // bu pattern kullanilacak. FindLogEntries + Single = tam olarak 1 Eşleşme
        // beklentisi; 0 veya 2+ olursa test fail olur.
        var log = wm.Server.FindLogEntries(
            Request.Create().WithPath("/ping").UsingGet()).Single();

        log.RequestMessage.Method.Should().Be("GET");
        log.RequestMessage.Path.Should().Be("/ping");

        // Assert 3: Header injection dogrulama — Faz 1'de Basic Auth, x-api-key,
        // x-amz-access-token gibi header'lar ayni pattern ile dogrulanacak
        log.RequestMessage.Headers.Should().ContainKey("Authorization");
        log.RequestMessage.Headers!["Authorization"]
            .ToString().Should().Contain("Basic smoke-token");
    }
}
