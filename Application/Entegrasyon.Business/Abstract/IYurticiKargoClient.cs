namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Yurtici Kargo SOAP API'sine raw XML istekleri gonderen low-level client.
/// </summary>
public interface IYurticiKargoClient
{
    /// <summary>
    /// SOAP istegi gonderir ve response XML'ini string olarak doner.
    /// </summary>
    Task<string> SendSoapRequestAsync(
        string soapAction,
        string soapBody,
        CancellationToken ct = default);
}
