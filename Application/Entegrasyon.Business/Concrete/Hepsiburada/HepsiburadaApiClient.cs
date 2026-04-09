using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utility.Constants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada API'ye credential-aware HTTP çağrıları yapan client.
/// Her istekte MarketPlace tablosundan (Id=3) credentials çeker,
/// Basic Auth (username:password) + User-Agent header otomatik eklenir.
/// </summary>
public sealed class HepsiburadaApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<HepsiburadaApiClient> logger) : IHepsiburadaApiClient
{
    private const string DefaultBaseUrl = "https://mpop.hepsiburada.com/product";

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Hepsiburada GET: {Url}", relativeUrl);
        return await client.GetAsync(relativeUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Hepsiburada POST: {Url}", relativeUrl);
        return await client.PostAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Hepsiburada PUT: {Url}", relativeUrl);
        return await client.PutAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> PostMultipartJsonFileAsync(
        string relativeUrl, string jsonContent, string fileName = "file.json")
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Hepsiburada POST Multipart: {Url}", relativeUrl);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", fileName);

        return await client.PostAsync(relativeUrl, content);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == HepsiburadaMarketPlaceId)
            ?? throw new InvalidOperationException("Hepsiburada marketplace kaydı bulunamadı (Id=3).");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;

        var client = httpClientFactory.CreateClient(StringConstants.HepsiburadaApi);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        // Basic Auth: base64(BasicAuthUserName:BasicAuthPassword)
        var username = marketplace.BasicAuthUserName ?? marketplace.ApiKey ?? "";
        var password = marketplace.BasicAuthPassword ?? marketplace.ApiSecret ?? "";
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // User-Agent header (entegratör adı)
        var userAgent = marketplace.UserAgentPrefix ?? "SelfIntegration";
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return client;
    }
}
