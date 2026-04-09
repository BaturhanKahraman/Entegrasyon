using System.Collections.Concurrent;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utility.Constants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti API'ye x-api-key header ile HTTP çağrıları yapan client.
/// MarketPlace tablosundan (Id=8) ApiKey/BaseUrl çeker.
/// Kimlik bilgileri tenant başına izole ConcurrentDictionary'de cache'lenir (multi-tenant uyumlu).
/// </summary>
public sealed class CiceksepetiApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<CiceksepetiApiClient> logger) : ICiceksepetiApiClient
{
    private const string DefaultBaseUrl = "https://apis.ciceksepeti.com";
    private const string ApiPrefix = "/api/v1";

    // Multi-tenant credential cache: static to survive scoped/transient lifetime
    private static readonly ConcurrentDictionary<int, (string ApiKey, string BaseUrl)> CredentialCache = new();
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> CredentialLocks = new();

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default)
    {
        var (client, url) = await CreateConfiguredClientAsync(relativeUrl, ct);
        logger.LogDebug("Çiçeksepeti GET: {Url}", url);
        return await client.GetAsync(url, ct);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        var (client, url) = await CreateConfiguredClientAsync(relativeUrl, ct);
        logger.LogDebug("Çiçeksepeti POST: {Url}", url);
        return await client.PostAsJsonAsync(url, body, ct);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        var (client, url) = await CreateConfiguredClientAsync(relativeUrl, ct);
        logger.LogDebug("Çiçeksepeti PUT: {Url}", url);
        return await client.PutAsJsonAsync(url, body, ct);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        var (client, url) = await CreateConfiguredClientAsync(relativeUrl, ct);
        logger.LogDebug("Çiçeksepeti DELETE: {Url}", url);
        return await client.DeleteAsync(url, ct);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendRawAsync(
        string absolutePath,
        HttpMethod method,
        HttpContent? content = null,
        CancellationToken ct = default)
    {
        // SendRawAsync does NOT prepend /api/v1/ — uses absolutePath directly on base URL
        var credentials = await EnsureCredentialsAsync(ct);

        var baseUrl = credentials.BaseUrl.TrimEnd('/');
        var url = baseUrl + "/" + absolutePath.TrimStart('/');

        var client = httpClientFactory.CreateClient(StringConstants.CiceksepetiApi);
        client.DefaultRequestHeaders.Add("x-api-key", credentials.ApiKey);

        using var request = new HttpRequestMessage(method, url) { Content = content };
        logger.LogDebug("Çiçeksepeti RAW {Method}: {Url}", method, url);
        return await client.SendAsync(request, ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private async Task<(HttpClient client, string url)> CreateConfiguredClientAsync(
        string relativeUrl, CancellationToken ct)
    {
        var credentials = await EnsureCredentialsAsync(ct);

        var client = httpClientFactory.CreateClient(StringConstants.CiceksepetiApi);
        client.DefaultRequestHeaders.Add("x-api-key", credentials.ApiKey);

        var baseUrl = credentials.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}{ApiPrefix}/{relativeUrl.TrimStart('/')}";
        return (client, url);
    }

    private async Task<(string ApiKey, string BaseUrl)> EnsureCredentialsAsync(CancellationToken ct)
    {
        // Fast path: cache hit
        if (CredentialCache.TryGetValue(CiceksepetiMarketPlaceId, out var cached))
            return cached;

        var semaphore = CredentialLocks.GetOrAdd(CiceksepetiMarketPlaceId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check inside lock
            if (CredentialCache.TryGetValue(CiceksepetiMarketPlaceId, out cached))
                return cached;

            await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
            var marketplace = await dbContext.MarketPlaces
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == CiceksepetiMarketPlaceId, ct)
                ?? throw new InvalidOperationException(
                    "Çiçeksepeti marketplace kaydı bulunamadı (Id=8).");

            var apiKey = marketplace.ApiKey
                ?? throw new InvalidOperationException(
                    "Çiçeksepeti API key tanımlı değil. Lütfen MarketPlace kaydına ApiKey ekleyin.");

            var credentials = (apiKey, marketplace.BaseUrl ?? DefaultBaseUrl);
            CredentialCache[CiceksepetiMarketPlaceId] = credentials;

            logger.LogInformation(
                "Çiçeksepeti credentials yüklendi (MarketPlaceId={Id})", CiceksepetiMarketPlaceId);
            return credentials;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Test helper — clears the static credential cache to prevent cross-test pollution.
    /// </summary>
    internal static void ClearCredentialCache() => CredentialCache.Clear();
}
