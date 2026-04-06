using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class BrandMasterImportTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task MasterImport_RedirectsToLogin_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/brands/master-import");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }

    [Fact]
    public async Task MasterImportExecute_RedirectsToLogin_WhenNotAuthenticated()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("selectedBrandIds", "1"),
            new KeyValuePair<string, string>("selectedBrandIds", "2")
        ]);

        var response = await _client.PostAsync("/brands/master-import", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
