using System.Collections.Concurrent;
using System.Text;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Surat Kargo SOAP API client'i.
/// Her istekte userName/password'u SOAP Body icinde parametre olarak gonderir.
/// Multi-tenant uyumlu: credential cache tenant basina izole.
/// </summary>
public interface ISuratKargoClient
{
    /// <summary>
    /// Surat Kargo SOAP endpoint'ine istek gonderir.
    /// Auth parametreleri (userName, password) otomatik olarak eklenir.
    /// </summary>
    /// <param name="soapAction">SOAP Action header degeri</param>
    /// <param name="operationName">SOAP operasyon adi</param>
    /// <param name="parameters">Ek parametreler (auth disinda)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response SOAP Body'nin ilk child elementi</returns>
    Task<XElement> SendAsync(string soapAction, string operationName,
        Dictionary<string, string> parameters, CancellationToken ct = default);
}

/// <summary>
/// Surat Kargo SOAP client implementasyonu.
/// </summary>
public sealed class SuratKargoClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ITenantContext tenantContext,
    ILogger<SuratKargoClient> logger) : ISuratKargoClient
{
    private const string DefaultBaseUrl = "http://webservices.suratkargo.com.tr/services.asmx";

    private static readonly XNamespace SoapEnv =
        "http://schemas.xmlsoap.org/soap/envelope/";

    private static readonly XNamespace TempUri =
        "http://tempuri.org/";

    // Multi-tenant credential cache: tenantId -> (credentials, cachedAt)
    private static readonly ConcurrentDictionary<int, (SuratKargoCredentials Creds, DateTime CachedAt)> CredentialCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<XElement> SendAsync(
        string soapAction, string operationName,
        Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var credentials = GetCredentials();

        // SOAP Body olustur
        var operationElement = new XElement(TempUri + operationName);
        operationElement.Add(new XElement(TempUri + "userName", credentials.UserName));
        operationElement.Add(new XElement(TempUri + "password", credentials.Password));
        operationElement.Add(new XElement(TempUri + "customerCode", credentials.CustomerCode));

        foreach (var param in parameters)
        {
            operationElement.Add(new XElement(TempUri + param.Key, param.Value));
        }

        // SOAP Envelope
        var envelope = new XElement(SoapEnv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", SoapEnv.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "tem", TempUri.NamespaceName),
            new XElement(SoapEnv + "Header"),
            new XElement(SoapEnv + "Body", operationElement));

        var xmlString = envelope.ToString(SaveOptions.DisableFormatting);

        // HTTP SOAP istegi gonder
        var baseUrl = credentials.BaseUrl;
        var client = httpClientFactory.CreateClient();
        var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
        request.Headers.Add("SOAPAction", $"\"{soapAction}\"");

        logger.LogDebug("SuratKargo SOAP request to {Url}, action={Action}", baseUrl, soapAction);

        var response = await client.SendAsync(request, ct);
        var responseXml = await response.Content.ReadAsStringAsync(ct);

        logger.LogDebug("SuratKargo SOAP response: {Body}", responseXml);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("SuratKargo SOAP error (HTTP {StatusCode}): {ResponseBody}",
                (int)response.StatusCode, responseXml);
            throw new HttpRequestException(
                $"Sürat Kargo SOAP isteği başarısız (HTTP {(int)response.StatusCode}).");
        }

        // Response parse — Body'nin ilk child'i
        var responseDoc = XDocument.Parse(responseXml);
        var body = responseDoc.Descendants(SoapEnv + "Body").FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP response'ta Body bulunamadı.");

        // Check for SOAP Fault
        var fault = body.Element(SoapEnv + "Fault");
        if (fault is not null)
        {
            var faultString = fault.Element("faultstring")?.Value ?? "Bilinmeyen SOAP hatası";
            logger.LogError("SuratKargo SOAP Fault: {FaultString}", faultString);
            throw new InvalidOperationException($"SOAP Fault: {faultString}");
        }

        var firstChild = body.Elements().FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP Body boş — response içerik yok.");

        return firstChild;
    }

    private SuratKargoCredentials GetCredentials()
    {
        var tenantId = tenantContext.TenantId;

        if (CredentialCache.TryGetValue(tenantId, out var cached) &&
            DateTime.UtcNow - cached.CachedAt < CacheTtl)
        {
            return cached.Creds;
        }

        // Configuration'dan oku
        var section = configuration.GetSection("SuratKargo");
        var creds = new SuratKargoCredentials(
            UserName: section["UserName"] ?? throw new InvalidOperationException("SuratKargo:UserName yapılandırılmamış."),
            Password: section["Password"] ?? throw new InvalidOperationException("SuratKargo:Password yapılandırılmamış."),
            CustomerCode: section["CustomerCode"] ?? throw new InvalidOperationException("SuratKargo:CustomerCode yapılandırılmamış."),
            BaseUrl: section["BaseUrl"] ?? DefaultBaseUrl);

        CredentialCache[tenantId] = (creds, DateTime.UtcNow);
        return creds;
    }
}
