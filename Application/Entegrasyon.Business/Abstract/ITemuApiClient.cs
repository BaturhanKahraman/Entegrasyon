namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Temu API'ye MD5 Sign + Access Token ile HTTP çağrıları yapan client.
/// Tüm API çağrıları tek router endpoint'e (POST /openapi/router) gider,
/// <c>type</c> parametresi ile method belirlenir.
/// MarketPlace tablosundan (Id=9) AppKey/AppSecret/AccessToken çeker.
/// </summary>
public interface ITemuApiClient
{
    /// <summary>
    /// Temu router endpoint'ine API çağrısı yapar.
    /// </summary>
    /// <typeparam name="T">Beklenen result tipi</typeparam>
    /// <param name="type">API method adı (ör: bg.goods.cats.get)</param>
    /// <param name="parameters">Method-specific parametreler (nullable)</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Deserialize edilmiş result nesnesi</returns>
    Task<T> CallAsync<T>(string type, object? parameters, CancellationToken ct);
}
