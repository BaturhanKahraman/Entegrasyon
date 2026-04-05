using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class Faz5Tests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Theory]
    [InlineData("/profile/change-password")]
    [InlineData("/profile/activity")]
    [InlineData("/settings/tax")]
    [InlineData("/settings/shipping")]
    [InlineData("/settings/webhooks")]
    [InlineData("/settings/api-keys")]
    public async Task Faz5Page_RedirectsToLogin(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
