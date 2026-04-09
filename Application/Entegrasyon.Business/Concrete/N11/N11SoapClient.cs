using System.Text;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utility.Constants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API'ye credential-aware XML çağrıları gönderen istemci.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret çeker
/// ve SOAP Envelope içindeki &lt;auth&gt; bloğu olarak body'ye ekler.
/// </summary>
public sealed class N11SoapClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<N11SoapClient> logger,
    ITenantContext tenantContext) : IN11SoapClient
{
    private const string DefaultBaseUrl = "https://api.n11.com/ws/";

    private static readonly XNamespace SoapEnv =
        "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Belirtilen WSDL endpoint'ine SOAP isteği gönderir.
    /// DB'den N11 kimlik bilgilerini çeker, &lt;auth&gt; bloğunu otomatik olarak
    /// request body'sinin ilk child elementi olarak ekler ve yanıtın SOAP Body
    /// içeriğini XElement olarak döner.
    /// </summary>
    /// <param name="wsdlPath">Servis yolu, örneğin "CategoryService"</param>
    /// <param name="soapAction">SOAP action başlığı (N11 için boş string gönderilebilir)</param>
    /// <param name="bodyContent">SOAP Body içindeki XML elementi (auth otomatik eklenir, kaynak mutate edilmez)</param>
    /// <returns>Response SOAP Body'sinin ilk child elementi</returns>
    public async Task<XElement> SendAsync(string wsdlPath, string soapAction, XElement bodyContent)
    {
        // 1. DB'den N11 credentials'ını çek
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var n11MarketPlaceId = tenantContext.GetMarketPlaceId("N11");
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == n11MarketPlaceId)
            ?? throw new InvalidOperationException($"N11 marketplace kaydı bulunamadı (Id={n11MarketPlaceId}).");

        // 2. bodyContent'in defensive kopyasını oluştur (caller'ın XElement'ini mutate etmemek için)
        var requestBody = new XElement(bodyContent);

        // 3. Auth elementini ilk child olarak inject et
        var authElement = new XElement("auth",
            new XElement("appKey", marketplace.ApiKey),
            new XElement("appSecret", marketplace.ApiSecret));

        requestBody.AddFirst(authElement);

        // 4. SOAP Envelope oluştur
        var envelope = new XElement(SoapEnv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "env", SoapEnv.NamespaceName),
            new XElement(SoapEnv + "Header"),
            new XElement(SoapEnv + "Body", requestBody));

        var xmlString = envelope.ToString(SaveOptions.DisableFormatting);

        // 5. HTTP SOAP isteği gönder
        var baseUrl = (marketplace.BaseUrl ?? DefaultBaseUrl).TrimEnd('/') + "/";
        var requestUrl = $"{baseUrl}{wsdlPath}";

        var client = httpClientFactory.CreateClient(StringConstants.N11SoapApi);
        var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
        if (!string.IsNullOrEmpty(soapAction))
            request.Headers.Add("SOAPAction", $"\"{soapAction}\"");

        logger.LogDebug("N11 SOAP request to {Url}: {Body}", requestUrl, xmlString);

        var response = await client.SendAsync(request);
        var responseXml = await response.Content.ReadAsStringAsync();

        logger.LogDebug("N11 SOAP response: {Body}", responseXml);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("N11 SOAP error (HTTP {StatusCode}): {ResponseBody}",
                (int)response.StatusCode, responseXml);
            throw new HttpRequestException(
                $"N11 SOAP isteği başarısız (HTTP {(int)response.StatusCode}). Response: {responseXml}");
        }

        // 6. Response parse et — Body'nin ilk child'ını dön
        var responseDoc = XDocument.Parse(responseXml);
        var body = responseDoc.Descendants(SoapEnv + "Body").FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP response'ta Body bulunamadı.");

        var firstChild = body.Elements().FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP Body boş — response içerik yok.");

        return firstChild;
    }
}
