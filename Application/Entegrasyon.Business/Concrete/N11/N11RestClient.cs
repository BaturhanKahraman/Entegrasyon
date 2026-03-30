using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 REST API'ye credential-aware HTTP çağrıları gönderen istemci.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret çeker
/// ve request header'larına ekler.
/// </summary>
public sealed class N11RestClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<N11RestClient> logger,
    ITenantContext tenantContext) : IN11RestClient
{
    private const string HttpClientName = "N11Rest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // -----------------------------------------------------------------------
    // GET
    // -----------------------------------------------------------------------

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateClientAsync(ct);
            logger.LogDebug("N11 REST GET {Path}", path);

            var response = await client.GetAsync(path, ct);
            return await ParseResponseAsync<T>(response, "GET", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST GET {Path} başarısız", path);
            return default;
        }
    }

    // -----------------------------------------------------------------------
    // POST
    // -----------------------------------------------------------------------

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateClientAsync(ct);
            logger.LogDebug("N11 REST POST {Path}", path);

            var response = await client.PostAsJsonAsync(path, body, JsonOptions, ct);
            return await ParseResponseAsync<TResponse>(response, "POST", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST POST {Path} başarısız", path);
            return default;
        }
    }

    // -----------------------------------------------------------------------
    // PUT
    // -----------------------------------------------------------------------

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateClientAsync(ct);
            logger.LogDebug("N11 REST PUT {Path}", path);

            var response = await client.PutAsJsonAsync(path, body, JsonOptions, ct);
            return await ParseResponseAsync<TResponse>(response, "PUT", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST PUT {Path} başarısız", path);
            return default;
        }
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// MarketPlace tablosundan N11 credentials çeker, header'lara ekler ve HttpClient döner.
    /// </summary>
    private async Task<HttpClient> CreateClientAsync(CancellationToken ct)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var n11MarketPlaceId = tenantContext.GetMarketPlaceId("N11");
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == n11MarketPlaceId, ct)
            ?? throw new InvalidOperationException($"N11 marketplace kaydı bulunamadı (Id={n11MarketPlaceId}).");

        var client = httpClientFactory.CreateClient(HttpClientName);

        // Önceki istekten kalan başlıkları temizle (HttpClient paylaşımlı olabilir)
        client.DefaultRequestHeaders.Remove("appkey");
        client.DefaultRequestHeaders.Remove("appsecret");

        client.DefaultRequestHeaders.TryAddWithoutValidation("appkey", marketplace.ApiKey);
        client.DefaultRequestHeaders.TryAddWithoutValidation("appsecret", marketplace.ApiSecret);

        return client;
    }

    private async Task<T?> ParseResponseAsync<T>(HttpResponseMessage response, string method, string path)
    {
        var body = await response.Content.ReadAsStringAsync();
        logger.LogDebug("N11 REST {Method} {Path} yanıtı (HTTP {StatusCode}): {Body}", method, path, (int)response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("N11 REST {Method} {Path} HTTP hatası ({StatusCode}): {Body}",
                method, path, (int)response.StatusCode, body);
            return default;
        }

        if (string.IsNullOrWhiteSpace(body)) return default;

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
