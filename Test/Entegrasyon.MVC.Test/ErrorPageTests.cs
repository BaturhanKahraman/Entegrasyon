using System.Net;
using FluentAssertions;

namespace Entegrasyon.MVC.Test;

public class ErrorPageTests(MvcTestFactory factory) : IClassFixture<MvcTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/error/404", HttpStatusCode.NotFound)]
    [InlineData("/error/403", HttpStatusCode.Forbidden)]
    public async Task ErrorPage_ReturnsCorrectStatusCode(string path, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(expectedStatus);
    }
}
