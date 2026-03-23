using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

/// <summary>
/// Trendyol e-Faturam platformu HTTP client.
/// 2-asamali auth: partner sign-in → customer sign-in.
/// Multi-tenant ready: tenant basina ayri token cache.
/// </summary>
public sealed class TrendyolEFaturaApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolEFaturaApiClient> logger) : ITrendyolEFaturaApiClient
{
    // Multi-tenant token cache — key: marketplaceId (tenant proxy)
    private static readonly ConcurrentDictionary<int, EFaturaTokenCache> TokenCaches = new();

    private const string DefaultBaseUrl = "https://apigateway.trendyolecozum.com";

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClientAsync(ct);
        logger.LogDebug("e-Fatura GET: {Url}", relativeUrl);
        return await client.GetAsync(relativeUrl, ct);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClientAsync(ct);
        logger.LogDebug("e-Fatura POST: {Url}", relativeUrl);
        return await client.PostAsJsonAsync(relativeUrl, body, ct);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken ct)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId, ct)
            ?? throw new InvalidOperationException("Trendyol marketplace kaydi bulunamadi (Id=1).");

        var settings = await dbContext.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key!.StartsWith("TrendyolEFatura:"))
            .ToListAsync(ct);

        var baseUrl = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:BaseUrl")?.Value ?? DefaultBaseUrl;
        var partnerEmail = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:PartnerEmail")?.Value;
        var partnerPassword = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:PartnerPassword")?.Value;
        var customerEmail = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:CustomerEmail")?.Value;
        var customerPassword = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:CustomerPassword")?.Value;
        var customerTaxId = settings.FirstOrDefault(s => s.Key == "TrendyolEFatura:CustomerTaxId")?.Value;

        // Token cache kontrolu
        var cache = TokenCaches.GetOrAdd(TrendyolMarketPlaceId, _ => new EFaturaTokenCache());

        if (cache.IsExpired || string.IsNullOrEmpty(cache.CustomerAccessToken))
        {
            // Partner sign-in
            if (!string.IsNullOrEmpty(partnerEmail) && !string.IsNullOrEmpty(partnerPassword))
            {
                var partnerClient = httpClientFactory.CreateClient();
                partnerClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

                var partnerResponse = await partnerClient.PostAsJsonAsync("api/auth/signin",
                    new EFaturaPartnerSignInRequest(partnerEmail, partnerPassword), ct);

                if (partnerResponse.IsSuccessStatusCode)
                {
                    // Token header'dan alinir
                    cache.PartnerAccessToken = partnerResponse.Headers.TryGetValues("Authorization", out var vals)
                        ? vals.FirstOrDefault()?.Replace("Bearer ", "")
                        : await partnerResponse.Content.ReadAsStringAsync(ct);

                    // Customer sign-in
                    if (!string.IsNullOrEmpty(customerEmail) && !string.IsNullOrEmpty(customerPassword)
                        && !string.IsNullOrEmpty(customerTaxId))
                    {
                        partnerClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", cache.PartnerAccessToken);

                        var customerResponse = await partnerClient.PostAsJsonAsync(
                            "api/invoice/partners/customer/signin",
                            new EFaturaCustomerSignInRequest(customerEmail, customerPassword, customerTaxId), ct);

                        if (customerResponse.IsSuccessStatusCode)
                        {
                            var customerResult = await customerResponse.Content
                                .ReadFromJsonAsync<EFaturaCustomerSignInResponse>(cancellationToken: ct);
                            if (customerResult is not null)
                            {
                                cache.CustomerAccessToken = customerResult.AccessToken;
                                cache.UserId = customerResult.UserId;
                                cache.CompanyId = customerResult.CompanyId;
                                cache.PartnerCustomerId = customerResult.PartnerCustomerId;
                                cache.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
                            }
                        }
                        else
                        {
                            logger.LogError("e-Fatura customer sign-in failed: {Status}", customerResponse.StatusCode);
                        }
                    }
                }
                else
                {
                    logger.LogError("e-Fatura partner sign-in failed: {Status}", partnerResponse.StatusCode);
                }
            }
        }

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(cache.CustomerAccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", cache.CustomerAccessToken);
        }

        return client;
    }

    /// <summary>Cached customer bilgilerini dondurur (companyId, userId vb.)</summary>
    public static EFaturaTokenCache? GetCachedTokenInfo(int marketplaceId = TrendyolMarketPlaceId)
    {
        return TokenCaches.TryGetValue(marketplaceId, out var cache) ? cache : null;
    }
}

/// <summary>
/// Token cache — multi-tenant ready.
/// </summary>
public sealed class EFaturaTokenCache
{
    public string? PartnerAccessToken { get; set; }
    public string? CustomerAccessToken { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int PartnerCustomerId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.MinValue;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
