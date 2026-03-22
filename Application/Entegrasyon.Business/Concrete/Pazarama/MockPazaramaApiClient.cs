using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Mock Pazarama API client — development ve test için.
/// Gerçek HTTP çağrısı yapmaz, statik JSON yanıtlar döndürür.
/// </summary>
public sealed class MockPazaramaApiClient(
    ILogger<MockPazaramaApiClient> logger) : IPazaramaApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        logger.LogDebug("[MOCK] Pazarama GET: {Url}", relativeUrl);

        string json;

        if (relativeUrl.Contains("getCategoryWithAttributes", StringComparison.OrdinalIgnoreCase))
        {
            json = """{"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false,"data":{"id":"00000000-0000-0000-0000-000000000001","name":"Mock Kategori","displayName":"Mock Kategori","description":null,"attributes":[]}}""";
        }
        else if (relativeUrl.Contains("categor", StringComparison.OrdinalIgnoreCase))
        {
            json = """{"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false,"data":[]}""";
        }
        else if (relativeUrl.Contains("brand", StringComparison.OrdinalIgnoreCase))
        {
            json = """{"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false,"data":[]}""";
        }
        else
        {
            json = """{"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false,"data":null}""";
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        logger.LogDebug("[MOCK] Pazarama POST: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        logger.LogDebug("[MOCK] Pazarama PUT: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    public Task<HttpResponseMessage> DeleteAsync(string relativeUrl)
    {
        logger.LogDebug("[MOCK] Pazarama DELETE: {Url}", relativeUrl);
        return Task.FromResult(CreateSuccessResponse());
    }

    private static HttpResponseMessage CreateSuccessResponse() =>
        new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false,"data":null}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };
}
