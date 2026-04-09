using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// Amazon SP-API credential-aware HTTP client.
/// OAuth access token + user-agent header otomatik eklenir.
/// 401'de token invalidate + retry (1 kez).
/// </summary>
public sealed class AmazonApiClient(
    IAmazonTokenManager tokenManager,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<AmazonApiClient> logger) : IAmazonApiClient
{
    private const string DefaultBaseUrl = "https://sellingpartnerapi-eu.amazon.com";

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async client =>
        {
            logger.LogDebug("Amazon GET: {Url}", relativeUrl);
            return await client.GetAsync(relativeUrl, ct);
        }, ct);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async client =>
        {
            logger.LogDebug("Amazon POST: {Url}", relativeUrl);
            return await client.PostAsJsonAsync(relativeUrl, body, ct);
        }, ct);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async client =>
        {
            logger.LogDebug("Amazon PUT: {Url}", relativeUrl);
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PutAsync(relativeUrl, content, ct);
        }, ct);
    }

    public async Task<HttpResponseMessage> PatchAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async client =>
        {
            logger.LogDebug("Amazon PATCH: {Url}", relativeUrl);
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.PatchAsync(relativeUrl, content, ct);
        }, ct);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async client =>
        {
            logger.LogDebug("Amazon DELETE: {Url}", relativeUrl);
            return await client.DeleteAsync(relativeUrl, ct);
        }, ct);
    }

    public async Task<HttpResponseMessage> UploadAsync(
        string presignedUrl, byte[] content, string contentType, CancellationToken ct = default)
    {
        // Presigned URL'ye direct PUT — auth header gerekmez
        var client = httpClientFactory.CreateClient(StringConstants.AmazonApi);
        var byteContent = new ByteArrayContent(content);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        logger.LogDebug("Amazon UPLOAD: {Url} ({Size} bytes)", presignedUrl, content.Length);
        return await client.PutAsync(presignedUrl, byteContent, ct);
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpClient, Task<HttpResponseMessage>> action, CancellationToken ct)
    {
        var client = await CreateConfiguredClientAsync(ct);
        var response = await action(client);

        // 401 → token invalidate + retry (1 kez)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Amazon 401 received, invalidating token and retrying");
            tokenManager.InvalidateToken();
            client = await CreateConfiguredClientAsync(ct);
            response = await action(client);
        }

        return response;
    }

    private async Task<HttpClient> CreateConfiguredClientAsync(CancellationToken ct)
    {
        var accessToken = await tokenManager.GetAccessTokenAsync(ct);

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MarketPlaceConstants.AmazonMarketPlaceId, ct)
            ?? throw new InvalidOperationException("Amazon marketplace kaydı bulunamadı (Id=5).");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;

        var client = httpClientFactory.CreateClient(StringConstants.AmazonApi);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        // x-amz-access-token header
        client.DefaultRequestHeaders.Add("x-amz-access-token", accessToken);

        // User-Agent header
        var userAgent = marketplace.UserAgentPrefix ?? "SelfIntegration/1.0";
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return client;
    }
}
