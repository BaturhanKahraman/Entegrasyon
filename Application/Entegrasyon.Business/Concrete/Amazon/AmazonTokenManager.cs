using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// Amazon SP-API OAuth 2.0 / LWA token yöneticisi.
/// Singleton olarak register edilir — tenant başına izole token cache.
/// Her tenant (MarketPlace.Id) kendi access_token'ını tutar.
/// Token 1 saat geçerli, 55 dk cache (5 dk margin).
/// </summary>
public sealed class AmazonTokenManager : IAmazonTokenManager, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AmazonTokenManager> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Tenant-aware token cache: key = MarketPlace.Id
    /// Multi-tenant geçişinde her tenant kendi token'ını tutar.
    /// </summary>
    private readonly ConcurrentDictionary<int, CachedToken> _tokenCache = new();

    private const int CacheMarginMinutes = 5;
    private const int TokenLifetimeMinutes = 60;
    private const string DefaultTokenUrl = "https://api.amazon.com/auth/o2/token";

    public AmazonTokenManager(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<AmazonTokenManager> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        return await GetAccessTokenForMarketPlaceAsync(MarketPlaceConstants.AmazonMarketPlaceId, ct);
    }

    /// <summary>
    /// Belirli bir MarketPlace ID için access token döner.
    /// Multi-tenant: farklı tenant'lar farklı MarketPlace ID'leri kullanabilir.
    /// </summary>
    public async Task<string> GetAccessTokenForMarketPlaceAsync(int marketPlaceId, CancellationToken ct = default)
    {
        // Fast path: cache'te geçerli token var
        if (_tokenCache.TryGetValue(marketPlaceId, out var cached) && cached.IsValid)
            return cached.AccessToken;

        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_tokenCache.TryGetValue(marketPlaceId, out cached) && cached.IsValid)
                return cached.AccessToken;

            return await RefreshTokenInternalAsync(marketPlaceId, ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void InvalidateToken()
    {
        InvalidateTokenForMarketPlace(MarketPlaceConstants.AmazonMarketPlaceId);
    }

    /// <summary>
    /// Belirli bir MarketPlace ID'nin token'ını geçersiz kılar.
    /// </summary>
    public void InvalidateTokenForMarketPlace(int marketPlaceId)
    {
        _tokenCache.TryRemove(marketPlaceId, out _);
        _logger.LogInformation("Amazon access token invalidated for MarketPlaceId={MarketPlaceId}", marketPlaceId);
    }

    private async Task<string> RefreshTokenInternalAsync(int marketPlaceId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == marketPlaceId, ct)
            ?? throw new InvalidOperationException($"Amazon marketplace kaydı bulunamadı (Id={marketPlaceId}).");

        if (string.IsNullOrWhiteSpace(marketplace.RefreshToken))
            throw new InvalidOperationException($"Amazon refresh token tanımlı değil (MarketPlaceId={marketPlaceId}).");

        var clientId = marketplace.ApiKey
            ?? throw new InvalidOperationException($"Amazon client_id (ApiKey) tanımlı değil (MarketPlaceId={marketPlaceId}).");
        var clientSecret = marketplace.ApiSecret
            ?? throw new InvalidOperationException($"Amazon client_secret (ApiSecret) tanımlı değil (MarketPlaceId={marketPlaceId}).");
        var tokenUrl = marketplace.TokenUrl ?? DefaultTokenUrl;

        var client = _httpClientFactory.CreateClient(StringConstants.AmazonApi);
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = marketplace.RefreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        _logger.LogDebug("Amazon token refresh for MarketPlaceId={MarketPlaceId}: {TokenUrl}", marketPlaceId, tokenUrl);
        var response = await client.PostAsync(tokenUrl, requestBody, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Amazon token refresh failed for MarketPlaceId={MarketPlaceId}: {Status} {Error}",
                marketPlaceId, response.StatusCode, error);
            throw new InvalidOperationException($"Amazon token refresh başarısız: {response.StatusCode} — {error}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<LwaTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Amazon token response parse edilemedi.");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(TokenLifetimeMinutes - CacheMarginMinutes);
        _tokenCache[marketPlaceId] = new CachedToken(tokenResponse.AccessToken, expiresAt);

        _logger.LogInformation("Amazon access token refreshed for MarketPlaceId={MarketPlaceId}, expires at {ExpiresAt}",
            marketPlaceId, expiresAt);
        return tokenResponse.AccessToken;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt)
    {
        public bool IsValid => DateTimeOffset.UtcNow < ExpiresAt;
    }

    private sealed record LwaTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}
