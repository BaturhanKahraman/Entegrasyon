namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada API'ye credential-aware HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan BasicAuthUserName/BasicAuthPassword/BaseUrl çeker,
/// Basic Auth ve User-Agent header'larını otomatik ekler.
/// </summary>
public interface IHepsiburadaApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);

    /// <summary>
    /// Hepsiburada ürün gönderiminde multipart/form-data ile JSON dosyası yükler.
    /// </summary>
    Task<HttpResponseMessage> PostMultipartJsonFileAsync(string relativeUrl, string jsonContent, string fileName = "file.json");
}
