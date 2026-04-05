using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class Faz6Tests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Theory]
    [InlineData("/storefront/wallets")]
    [InlineData("/storefront/wishlists")]
    [InlineData("/storefront/stock-notifications")]
    [InlineData("/storefront/search-analytics")]
    [InlineData("/storefront/push-notifications")]
    public async Task Faz6Page_RedirectsToLogin(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
