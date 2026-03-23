namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Trendyol e-Faturam platformuna HTTP cagrilari yapan client.
/// Marketplace API'sinden (ITrendyolApiClient) tamamen bagimsizdir.
/// 2-asamali auth: partner sign-in → customer sign-in.
/// </summary>
public interface ITrendyolEFaturaApiClient
{
    /// <summary>Authenticated GET istegi</summary>
    Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default);

    /// <summary>Authenticated POST istegi</summary>
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
}
