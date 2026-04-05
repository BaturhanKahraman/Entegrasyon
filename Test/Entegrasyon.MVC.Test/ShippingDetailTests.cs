using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class ShippingDetailTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Detail_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/shipping/1");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
