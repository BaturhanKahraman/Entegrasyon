using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class IntegrationHealthTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task IntegrationHealthPage_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/integrations/health");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
