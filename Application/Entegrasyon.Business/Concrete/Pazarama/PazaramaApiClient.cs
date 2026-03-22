using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama API'ye OAuth2 Bearer token ile HTTP çağrıları yapan client.
/// MarketPlace tablosundan (Id=5) clientId/clientSecret/tokenUrl çeker,
/// token tenant başına izole cache'lenir (multi-tenant uyumlu).
/// </summary>
public sealed class PazaramaApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PazaramaApiClient> logger) : IPazaramaApiClient
{
    private const string DefaultBaseUrl = "https://isortagimapi.pazarama.com";
    private const string DefaultTokenUrl = "https://isortagimgiris.pazarama.com/connect/token";
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Tenant-aware token cache: key = MarketPlace.Id
    /// </summary>
    private readonly ConcurrentDictionary<int, CachedToken> _tokenCache = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _tokenLocks = new();

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var (client, absoluteUrl) = await CreateConfiguredClientAsync(relativeUrl);
        logger.LogDebug("Pazarama GET: {Url}", absoluteUrl);
        return await client.GetAsync(absoluteUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var (client, absoluteUrl) = await CreateConfiguredClientAsync(relativeUrl);
        logger.LogDebug("Pazarama POST: {Url}", absoluteUrl);
        return await client.PostAsJsonAsync(absoluteUrl, body);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var (client, absoluteUrl) = await CreateConfiguredClientAsync(relativeUrl);
        logger.LogDebug("Pazarama PUT: {Url}", absoluteUrl);
        return await client.PutAsJsonAsync(absoluteUrl, body);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativeUrl)
    {
        var (client, absoluteUrl) = await CreateConfiguredClientAsync(relativeUrl);
        logger.LogDebug("Pazarama DELETE: {Url}", absoluteUrl);
        return await client.DeleteAsync(absoluteUrl);
    }

    private async Task<(HttpClient client, string absoluteUrl)> CreateConfiguredClientAsync(string relativeUrl)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == PazaramaMarketPlaceId)
            ?? throw new InvalidOperationException("Pazarama marketplace kaydı bulunamadı.");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;
        var tokenUrl = marketplace.TokenUrl ?? DefaultTokenUrl;
        var clientId = marketplace.ApiKey ?? "";
        var clientSecret = marketplace.ApiSecret ?? "";

        var accessToken = await EnsureValidTokenAsync(marketplace.Id, clientId, clientSecret, tokenUrl);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var absoluteUrl = baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
        return (client, absoluteUrl);
    }

    private async Task<string> EnsureValidTokenAsync(int marketPlaceId, string clientId, string clientSecret, string tokenUrl)
    {
        // Fast-path: cache'te geçerli token var
        if (_tokenCache.TryGetValue(marketPlaceId, out var cached) && cached.IsValid(TokenExpiryBuffer))
            return cached.AccessToken;

        var semaphore = _tokenLocks.GetOrAdd(marketPlaceId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            // Double-check inside lock
            if (_tokenCache.TryGetValue(marketPlaceId, out cached) && cached.IsValid(TokenExpiryBuffer))
                return cached.AccessToken;

            logger.LogDebug("Pazarama token yenileniyor (MarketPlaceId={MarketPlaceId})...", marketPlaceId);

            var tokenResponse = await FetchTokenAsync(clientId, clientSecret, tokenUrl);
            var newCached = new CachedToken(
                tokenResponse.AccessToken,
                DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

            _tokenCache[marketPlaceId] = newCached;

            logger.LogInformation("Pazarama token alındı (MarketPlaceId={MarketPlaceId}), geçerlilik: {ExpiresAt:O}",
                marketPlaceId, newCached.ExpiresAt);

            return tokenResponse.AccessToken;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<TokenResponse> FetchTokenAsync(string clientId, string clientSecret, string tokenUrl)
    {
        var tokenClient = httpClientFactory.CreateClient();

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "merchantgatewayapi.fullaccess")
        ]);

        var response = await tokenClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Pazarama token endpoint geçersiz yanıt döndürdü.");

        return tokenResponse;
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt)
    {
        public bool IsValid(TimeSpan buffer) => DateTimeOffset.UtcNow < ExpiresAt - buffer;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
