using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class GiftCardTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task GiftCardIndex_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/gift-cards");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task GiftCardCreate_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/gift-cards/create");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
