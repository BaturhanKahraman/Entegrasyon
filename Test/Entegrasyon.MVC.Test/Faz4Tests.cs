using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class Faz4Tests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Pricing_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/pricing");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task PricingRules_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/pricing/rules");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task Loyalty_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/loyalty");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task StorefrontReferrals_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/storefront/referrals");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
