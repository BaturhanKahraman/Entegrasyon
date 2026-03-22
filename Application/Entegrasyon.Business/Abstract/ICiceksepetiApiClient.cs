namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Çiçeksepeti API'ye x-api-key header ile HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan (Id=8) ApiKey çeker.
/// </summary>
public interface ICiceksepetiApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default);

    /// <summary>
    /// Farklı path prefix gerektiren endpoint'ler için (ör. /Branch/SendInvoiceMail).
    /// Base URL'e /api/v1/ eklemez, verilen path'i doğrudan kullanır.
    /// </summary>
    Task<HttpResponseMessage> SendRawAsync(string absolutePath, HttpMethod method, HttpContent? content = null, CancellationToken ct = default);
}
