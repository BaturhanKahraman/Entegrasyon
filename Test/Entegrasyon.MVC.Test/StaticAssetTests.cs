using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class StaticAssetTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/lib/tabler/css/tabler.min.css")]
    [InlineData("/lib/htmx/htmx.min.js")]
    [InlineData("/js/site.js")]
    [InlineData("/css/site.css")]
    public async Task StaticAsset_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
