namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Trendyol API'ye credential-aware HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan ApiKey/ApiSecret/SellerId/BaseUrl çeker,
/// Basic Auth ve User-Agent header'larını otomatik ekler.
/// </summary>
public interface ITrendyolApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl);
}
