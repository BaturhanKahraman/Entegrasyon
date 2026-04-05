using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class ReportTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Theory]
    [InlineData("/reports/customers")]
    [InlineData("/reports/returns")]
    [InlineData("/reports/tax")]
    [InlineData("/reports/category-sales")]
    [InlineData("/reports/shipping")]
    public async Task ReportPage_RedirectsToLogin_WhenNotAuthenticated(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
