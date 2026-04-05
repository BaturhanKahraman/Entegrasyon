using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class ReturnTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Returns_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/returns");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
