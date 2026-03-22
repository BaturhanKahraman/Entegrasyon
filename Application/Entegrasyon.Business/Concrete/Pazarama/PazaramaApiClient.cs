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
/// MarketPlace tablosundan (Id=4) clientId/clientSecret/tokenUrl çeker,
/// token in-memory cache'lenir, son 5 dakikada otomatik yenilenir.
/// </summary>
public sealed class PazaramaApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PazaramaApiClient> logger) : IPazaramaApiClient
{
    private const string DefaultBaseUrl = "https://isortagimapi.pazarama.com";
    private const string DefaultTokenUrl = "https://isortagimgiris.pazarama.com/connect/token";
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(5);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

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

    /// <summary>
    /// Returns a configured HttpClient (Bearer auth, no BaseAddress set) and the resolved absolute URL.
    /// Avoids setting BaseAddress so the same underlying HttpClient can be reused without issue.
    /// </summary>
    private async Task<(HttpClient client, string absoluteUrl)> CreateConfiguredClientAsync(string relativeUrl)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == PazaramaMarketPlaceId)
            ?? throw new InvalidOperationException("Pazarama marketplace kaydı bulunamadı (Id=4).");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;
        var tokenUrl = marketplace.TokenUrl ?? DefaultTokenUrl;
        var clientId = marketplace.ApiKey ?? "";
        var clientSecret = marketplace.ApiSecret ?? "";

        await EnsureValidTokenAsync(clientId, clientSecret, tokenUrl);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        // Build absolute URL — avoids setting BaseAddress after the client has fired requests
        var absoluteUrl = baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');

        return (client, absoluteUrl);
    }

    private async Task EnsureValidTokenAsync(string clientId, string clientSecret, string tokenUrl)
    {
        // Fast-path: token still valid
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt - TokenExpiryBuffer)
            return;

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check inside lock
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt - TokenExpiryBuffer)
                return;

            logger.LogDebug("Pazarama token yenileniyor...");

            var tokenResponse = await FetchTokenAsync(clientId, clientSecret, tokenUrl);
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            logger.LogInformation("Pazarama token alındı, geçerlilik: {ExpiresAt:O}", _tokenExpiresAt);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<TokenResponse> FetchTokenAsync(string clientId, string clientSecret, string tokenUrl)
    {
        var tokenClient = httpClientFactory.CreateClient();

        // Basic Auth header for token endpoint — set on the request directly to avoid shared-client issues
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

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
