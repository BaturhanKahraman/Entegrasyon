using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// Integration HTTP testleri icin sabit kimlikli auth handler.
/// Her istegi ayni kullanici (sabit NameIdentifier) ile dogrular — boylece
/// [Authorize] gecer ve antiforgery token GET (uretim) ile POST (dogrulama)
/// arasinda ayni kimlige baglanir. Antiforgery KAPATILMAZ; sadece login akisi
/// (gecici sifre/reset) bypass edilir, gercek antiforgery + binding + manager
/// zinciri korunur.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTestAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
            new Claim(ClaimTypes.Name, "integration-admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("TenantId", "1")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
