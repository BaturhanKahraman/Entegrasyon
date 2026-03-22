using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti API client — development ve test ortamları için.
/// Gerçek HTTP çağrısı yapmaz; statik JSON yanıtlar döndürür.
/// </summary>
public sealed class MockCiceksepetiApiClient(
    ILogger<MockCiceksepetiApiClient> logger) : ICiceksepetiApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] Çiçeksepeti GET: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] Çiçeksepeti POST: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] Çiçeksepeti PUT: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] Çiçeksepeti DELETE: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> SendRawAsync(
        string absolutePath, HttpMethod method, HttpContent? content = null, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] Çiçeksepeti RAW {Method}: {Path}", method, absolutePath);
        return Task.FromResult(CreateSuccessResponse());
    }

    private static HttpResponseMessage CreateSuccessResponse() =>
        new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"data":null}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };
}
