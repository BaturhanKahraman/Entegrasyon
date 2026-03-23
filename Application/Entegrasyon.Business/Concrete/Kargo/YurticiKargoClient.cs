using System.Text;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Yurtici Kargo SOAP API'sine raw XML istekleri gonderen client.
/// HttpClient ile SOAP envelope gonderir, XML response doner.
/// Multi-tenant: her tenant icin farkli BaseUrl kullanilabilir.
/// </summary>
public sealed class YurticiKargoClient(
    IHttpClientFactory httpClientFactory,
    ILogger<YurticiKargoClient> logger) : IYurticiKargoClient
{
    private const string DefaultBaseUrl = "http://webservices.yurticikargo.com:8080/KOPSWebServices/ShippingOrderDispatcherServices";

    public async Task<string> SendSoapRequestAsync(
        string soapAction,
        string soapBody,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("YurticiKargo");

        var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", soapAction);

        var baseUrl = client.BaseAddress?.ToString() ?? DefaultBaseUrl;

        logger.LogDebug("YurticiKargo SOAP request: {Action} -> {Url}", soapAction, baseUrl);

        var response = await client.PostAsync(baseUrl, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("YurticiKargo SOAP error: {Status} {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"SOAP request failed: {response.StatusCode}");
        }

        return responseBody;
    }
}
