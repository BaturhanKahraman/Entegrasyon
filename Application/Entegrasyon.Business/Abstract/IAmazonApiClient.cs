namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Amazon SP-API credential-aware HTTP wrapper.
/// Her istekte OAuth access token alır, x-amz-access-token header ekler.
/// 401'de token invalidate + retry yapar.
/// </summary>
public interface IAmazonApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PatchAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default);

    /// <summary>
    /// Presigned URL'ye content yükler (Feeds API document upload için).
    /// </summary>
    Task<HttpResponseMessage> UploadAsync(string presignedUrl, byte[] content, string contentType, CancellationToken ct = default);
}
