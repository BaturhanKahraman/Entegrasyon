using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Temu;

/// <summary>
/// Temu API client — tek router endpoint (POST /openapi/router) üzerinden tüm API çağrılarını yapar.
/// Auth: App Key + App Secret + Access Token + MD5 Sign.
/// Credential'lar MarketPlace tablosundan (Id=9) çekilir, tenant başına izole cache'lenir.
/// </summary>
public sealed class TemuApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<TemuApiClient> logger) : ITemuApiClient
{
    private const string DefaultBaseUrl = "https://openapi-b-eu.temu.com";
    private const string RouterPath = "/openapi/router";

    /// <summary>
    /// Multi-tenant credential cache: key = MarketPlace.Id
    /// Static to survive scoped lifetime.
    /// </summary>
    private static readonly ConcurrentDictionary<int, TemuCredentials> CredentialCache = new();
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> CredentialLocks = new();

    /// <inheritdoc />
    public async Task<T> CallAsync<T>(string type, object? parameters, CancellationToken ct)
    {
        var credentials = await EnsureCredentialsAsync(ct);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Build system parameters for sign calculation
        var signParams = new Dictionary<string, string>
        {
            { "type", type },
            { "app_key", credentials.AppKey },
            { "timestamp", timestamp },
            { "access_token", credentials.AccessToken },
            { "data_type", "JSON" }
        };

        var sign = CalculateSign(signParams, credentials.AppSecret);

        // Build full request body
        var requestBody = new Dictionary<string, object?>
        {
            { "type", type },
            { "app_key", credentials.AppKey },
            { "timestamp", timestamp },
            { "access_token", credentials.AccessToken },
            { "data_type", "JSON" },
            { "sign", sign }
        };

        // Merge method-specific parameters
        if (parameters != null)
        {
            var json = JsonSerializer.Serialize(parameters);
            var extraParams = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (extraParams != null)
            {
                foreach (var kvp in extraParams)
                {
                    requestBody[kvp.Key] = kvp.Value;
                }
            }
        }

        var url = credentials.BaseUrl.TrimEnd('/') + RouterPath;
        logger.LogDebug("Temu API call: {Type} → {Url}", type, url);

        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(url, requestBody, ct);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<TemuApiResponse<T>>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Temu API geçersiz yanıt döndürdü.");

        if (!apiResponse.Success)
        {
            logger.LogWarning("Temu API error: [{ErrorCode}] {ErrorMsg} (type={Type})",
                apiResponse.ErrorCode, apiResponse.ErrorMsg, type);
            throw new TemuApiException(apiResponse.ErrorCode, apiResponse.ErrorMsg ?? "Bilinmeyen hata");
        }

        return apiResponse.Result!;
    }

    /// <summary>
    /// MD5 Sign hesaplama — parametreleri alfabetik sırala, app_secret ile wrap et, MD5 hash → UPPERCASE hex.
    /// Public static for testability.
    /// </summary>
    public static string CalculateSign(Dictionary<string, string> parameters, string appSecret)
    {
        // 1. Parametreleri key'e göre alfabetik sırala
        var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal);

        // 2. key1value1key2value2... biçiminde birleştir
        var sb = new StringBuilder();
        sb.Append(appSecret);
        foreach (var kvp in sortedParams)
        {
            sb.Append(kvp.Key);
            sb.Append(kvp.Value);
        }
        sb.Append(appSecret);

        // 3. MD5 hash → UPPERCASE hex
        var inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes); // .NET 5+ returns uppercase by default
    }

    // ── Credential management ───────────────────────────────────────────────────

    private async Task<TemuCredentials> EnsureCredentialsAsync(CancellationToken ct)
    {
        // Fast-path: cache hit
        if (CredentialCache.TryGetValue(TemuMarketPlaceId, out var cached))
            return cached;

        var semaphore = CredentialLocks.GetOrAdd(TemuMarketPlaceId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check inside lock
            if (CredentialCache.TryGetValue(TemuMarketPlaceId, out cached))
                return cached;

            await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
            var marketplace = await dbContext.MarketPlaces
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == TemuMarketPlaceId, ct)
                ?? throw new InvalidOperationException(
                    "Temu marketplace kaydı bulunamadı (Id=9).");

            var appKey = marketplace.ApiKey
                ?? throw new InvalidOperationException(
                    "Temu app_key tanımlı değil. Lütfen MarketPlace kaydına ApiKey (app_key) ekleyin.");

            var appSecret = marketplace.ApiSecret
                ?? throw new InvalidOperationException(
                    "Temu app_secret tanımlı değil. Lütfen MarketPlace kaydına ApiSecret (app_secret) ekleyin.");

            // Temu access_token 3 ay geçerli — MarketPlace.RefreshToken alanında saklanır
            var accessToken = marketplace.RefreshToken ?? string.Empty;

            var credentials = new TemuCredentials(
                appKey,
                appSecret,
                accessToken,
                marketplace.BaseUrl ?? DefaultBaseUrl);

            CredentialCache[TemuMarketPlaceId] = credentials;

            logger.LogInformation(
                "Temu credentials yüklendi (MarketPlaceId={Id})", TemuMarketPlaceId);
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

    /// <summary>
    /// Tenant başına izole credential record.
    /// </summary>
    private sealed record TemuCredentials(
        string AppKey,
        string AppSecret,
        string AccessToken,
        string BaseUrl);
}
