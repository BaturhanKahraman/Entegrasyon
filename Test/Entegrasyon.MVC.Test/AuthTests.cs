using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class AuthTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var response = await _client.GetAsync("/auth/login");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Root_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task ProtectedPage_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
