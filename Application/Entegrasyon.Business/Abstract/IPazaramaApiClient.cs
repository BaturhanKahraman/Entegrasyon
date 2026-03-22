namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Pazarama API'ye OAuth2 Bearer token ile HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan (Id=4) clientId/clientSecret çeker,
/// token caching ile otomatik yenileme yapar.
/// </summary>
public interface IPazaramaApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl);
}
