using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utility.Constants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol API'ye credential-aware HTTP cagrilari yapan client.
/// Her istekte MarketPlace tablosundan (Id=1) credentials ceker,
/// Basic Auth + User-Agent header otomatik eklenir.
/// </summary>
public sealed class TrendyolApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolApiClient> logger) : ITrendyolApiClient
{
    private const string DefaultBaseUrl = "https://apigw.trendyol.com";

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Trendyol GET: {Url}", relativeUrl);
        return await client.GetAsync(relativeUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Trendyol POST: {Url}", relativeUrl);
        return await client.PostAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Trendyol PUT: {Url}", relativeUrl);
        return await client.PutAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Trendyol DELETE: {Url}", relativeUrl);
        return await client.DeleteAsync(relativeUrl);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId)
            ?? throw new InvalidOperationException("Trendyol marketplace kaydi bulunamadı (Id=1).");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;

        var client = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        // Basic Auth: base64(apiKey:apiSecret)
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{marketplace.ApiKey}:{marketplace.ApiSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // User-Agent: "{SellerId} - SelfIntegration"
        var userAgent = marketplace.UserAgentPrefix
            ?? (marketplace.SellerId is not null ? $"{marketplace.SellerId} - SelfIntegration" : "SelfIntegration");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return client;
    }
}
