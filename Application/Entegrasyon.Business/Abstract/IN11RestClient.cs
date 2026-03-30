namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 REST API'ye credential-aware HTTP çağrıları yapan istemci.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret çeker
/// ve request header'larına ekler.
/// </summary>
public interface IN11RestClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
}
