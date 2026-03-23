using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM kargo API client — Basic Auth, shipment.pttavm.com.
/// Multi-tenant: ConcurrentDictionary ile credential cache.
/// </summary>
public sealed class PttavmShipmentApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PttavmShipmentApiClient> logger) : IPttavmShipmentApiClient
{
    private static readonly ConcurrentDictionary<int, CachedShipmentCredentials> CredentialCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string BaseUrl = "https://shipment.pttavm.com";

    internal static void ClearCacheForTesting() => CredentialCache.Clear();

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(relativeUrl, content);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        var credentials = await GetOrLoadCredentialsAsync();
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(BaseUrl);

        var authValue = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{credentials.Username}:{credentials.Password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authValue);

        return client;
    }

    private async Task<CachedShipmentCredentials> GetOrLoadCredentialsAsync()
    {
        var marketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;

        if (CredentialCache.TryGetValue(marketPlaceId, out var cached) && cached.IsValid(CacheTtl))
            return cached;

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketPlace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(mp => mp.Id == marketPlaceId);

        if (marketPlace is null)
            throw new InvalidOperationException($"PttAVM marketplace (Id={marketPlaceId}) not found.");

        // Kargo API icin BasicAuth alanlari kullanilir
        var credentials = new CachedShipmentCredentials(
            marketPlace.BasicAuthUserName ?? string.Empty,
            marketPlace.BasicAuthPassword ?? string.Empty,
            DateTimeOffset.UtcNow);

        CredentialCache[marketPlaceId] = credentials;

        logger.LogDebug("PttAVM shipment credentials loaded from DB and cached for marketplace {MarketPlaceId}", marketPlaceId);

        return credentials;
    }

    internal sealed record CachedShipmentCredentials(
        string Username,
        string Password,
        DateTimeOffset CachedAt)
    {
        public bool IsValid(TimeSpan ttl) => DateTimeOffset.UtcNow - CachedAt < ttl;
    }
}
