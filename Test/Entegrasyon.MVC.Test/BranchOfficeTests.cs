using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class BranchOfficeTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task CreatePage_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/branch-offices/create");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task EditPage_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/branch-offices/1/edit");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
