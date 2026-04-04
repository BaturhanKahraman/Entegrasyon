using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class AbandonedCartTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task AbandonedCarts_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/storefront/abandoned-carts");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
