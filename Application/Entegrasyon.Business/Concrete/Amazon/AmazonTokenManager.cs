using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// Amazon SP-API OAuth 2.0 / LWA token yöneticisi.
/// Singleton olarak register edilir — SemaphoreSlim ile thread-safe token refresh.
/// Token 1 saat geçerli, 55 dk cache (5 dk margin).
/// </summary>
public sealed class AmazonTokenManager : IAmazonTokenManager, IDisposable
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AmazonTokenManager> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private const int CacheMarginMinutes = 5;
    private const int TokenLifetimeMinutes = 60;
    private const string DefaultTokenUrl = "https://api.amazon.com/auth/o2/token";

    public AmazonTokenManager(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<AmazonTokenManager> logger)
    {
        _contextFactory = contextFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: cache'te geçerli token var
        if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _cachedAccessToken;

        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _cachedAccessToken;

            return await RefreshTokenInternalAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void InvalidateToken()
    {
        _cachedAccessToken = null;
        _tokenExpiresAt = DateTimeOffset.MinValue;
        _logger.LogInformation("Amazon access token invalidated");
    }

    private async Task<string> RefreshTokenInternalAsync(CancellationToken ct)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(ct);

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MarketPlaceConstants.AmazonMarketPlaceId, ct)
            ?? throw new InvalidOperationException("Amazon marketplace kaydı bulunamadı (Id=5).");

        if (string.IsNullOrWhiteSpace(marketplace.RefreshToken))
            throw new InvalidOperationException("Amazon refresh token tanımlı değil. MarketPlace tablosunda RefreshToken alanını doldurun.");

        var clientId = marketplace.ApiKey
            ?? throw new InvalidOperationException("Amazon client_id (ApiKey) tanımlı değil.");
        var clientSecret = marketplace.ApiSecret
            ?? throw new InvalidOperationException("Amazon client_secret (ApiSecret) tanımlı değil.");
        var tokenUrl = marketplace.TokenUrl ?? DefaultTokenUrl;

        var client = _httpClientFactory.CreateClient();
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = marketplace.RefreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        _logger.LogDebug("Amazon token refresh: {TokenUrl}", tokenUrl);
        var response = await client.PostAsync(tokenUrl, requestBody, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Amazon token refresh failed: {Status} {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Amazon token refresh başarısız: {response.StatusCode} — {error}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<LwaTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Amazon token response parse edilemedi.");

        _cachedAccessToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(TokenLifetimeMinutes - CacheMarginMinutes);

        _logger.LogInformation("Amazon access token refreshed, expires at {ExpiresAt}", _tokenExpiresAt);
        return _cachedAccessToken;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private sealed record LwaTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}
