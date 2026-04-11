using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class SuggestionsControllerTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Theory]
    [InlineData("/marketplace/sync/suggestions/brands?mp=1&q=nike")]
    [InlineData("/marketplace/sync/suggestions/categories?mp=1&q=elbise")]
    [InlineData("/marketplace/sync/suggestions/attributes?mp=1&q=renk")]
    [InlineData("/marketplace/sync/suggestions/attributes/1/values?mp=1&q=kirmizi")]
    public async Task SuggestionEndpoints_RedirectToLogin_WhenNotAuthenticated(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "tum suggestions endpoint'leri [Authorize] ile korumali olmali");
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
