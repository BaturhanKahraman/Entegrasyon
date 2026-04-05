using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class OrderDetailTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task OrderDetail_RedirectsToLogin_WhenNotAuthenticated()
    {
        var id = Guid.Empty;
        var response = await _client.GetAsync($"/orders/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task Print_RedirectsToLogin_WhenNotAuthenticated()
    {
        var id = Guid.Empty;
        var response = await _client.GetAsync($"/orders/{id}/print");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
