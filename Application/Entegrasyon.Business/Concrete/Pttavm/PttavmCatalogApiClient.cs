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

public sealed class PttavmCatalogApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PttavmCatalogApiClient> logger) : IPttavmCatalogApiClient
{
    private static readonly ConcurrentDictionary<int, CachedCredentials> CredentialCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    internal static void ClearCacheForTesting() => CredentialCache.Clear();

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        return await client.GetAsync(relativeUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(relativeUrl, content);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(relativeUrl, content);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        var credentials = await GetOrLoadCredentialsAsync();
        var client = httpClientFactory.CreateClient(StringConstants.PttavmCatalogApi);
        client.BaseAddress = new Uri(credentials.BaseUrl);
        client.DefaultRequestHeaders.Add("Api-Key", credentials.ApiKey);
        client.DefaultRequestHeaders.Add("Access-Token", credentials.AccessToken);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        return client;
    }

    private async Task<CachedCredentials> GetOrLoadCredentialsAsync()
    {
        var marketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;

        if (CredentialCache.TryGetValue(marketPlaceId, out var cached) && cached.IsValid(CacheTtl))
            return cached;

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketPlace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(mp => mp.Id == marketPlaceId);

        if (marketPlace is null || string.IsNullOrWhiteSpace(marketPlace.BaseUrl))
            throw new InvalidOperationException($"PttAVM marketplace (Id={marketPlaceId}) not found or BaseUrl is empty.");

        var credentials = new CachedCredentials(
            marketPlace.ApiKey ?? string.Empty,
            marketPlace.ApiSecret ?? string.Empty,
            marketPlace.BaseUrl,
            DateTimeOffset.UtcNow);

        CredentialCache[marketPlaceId] = credentials;

        logger.LogDebug("PttAVM credentials loaded from DB and cached for marketplace {MarketPlaceId}", marketPlaceId);

        return credentials;
    }

    internal sealed record CachedCredentials(
        string ApiKey,
        string AccessToken,
        string BaseUrl,
        DateTimeOffset CachedAt)
    {
        public bool IsValid(TimeSpan ttl) => DateTimeOffset.UtcNow - CachedAt < ttl;
    }
}
