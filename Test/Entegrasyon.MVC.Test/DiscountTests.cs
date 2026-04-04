using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class DiscountTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task DiscountsIndex_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/discounts");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task DiscountsCreate_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/discounts/create");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
