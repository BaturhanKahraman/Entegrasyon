using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Aras Kargo SOAP API ile iletisim kuran low-level HTTP client.
/// SOAP XML envelope olusturur, POST eder ve response parse eder.
/// </summary>
public class ArasKargoClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ArasKargoClient> logger)
{
    private const string SoapNamespace = "http://ArasCargo.com/";
    private const string SoapEnvelopeNs = "http://schemas.xmlsoap.org/soap/envelope/";

    private ArasKargoConfig GetConfig() => new(
        UserName: configuration["ArasKargo:UserName"] ?? "",
        Password: configuration["ArasKargo:Password"] ?? "",
        CustomerCode: configuration["ArasKargo:CustomerCode"] ?? "",
        BaseUrl: configuration["ArasKargo:BaseUrl"]
            ?? "https://customerservicestest.araskargo.com.tr/arascargoservice/arascargoservice.asmx",
        IsTestEnvironment: configuration.GetValue<bool>("ArasKargo:IsTestEnvironment", true));

    // ── SetOrder ────────────────────────────────────────────────────────────

    public virtual async Task<ArasKargoOrderResult> SetOrderAsync(
        ArasKargoShipmentRequest request, CancellationToken ct = default)
    {
        var config = GetConfig();

        var orderXml = BuildSetOrderXml(config, request);
        var responseXml = await SendSoapRequestAsync(config.BaseUrl, "SetOrder", orderXml, ct);

        return ParseSetOrderResponse(responseXml);
    }

    // ── CancelDispatch ──────────────────────────────────────────────────────

    public virtual async Task<bool> CancelDispatchAsync(
        string integrationCode, CancellationToken ct = default)
    {
        var config = GetConfig();

        var bodyXml = BuildCancelDispatchXml(config, integrationCode);
        var responseXml = await SendSoapRequestAsync(config.BaseUrl, "CancelDispatch", bodyXml, ct);

        return ParseCancelDispatchResponse(responseXml);
    }

    // ── GetQueryJSON ────────────────────────────────────────────────────────

    public virtual async Task<T?> GetQueryJsonAsync<T>(
        int queryType,
        string? integrationCode = null,
        string? date1 = null,
        string? date2 = null,
        CancellationToken ct = default)
    {
        var config = GetConfig();

        var bodyXml = BuildGetQueryJsonXml(config, queryType, integrationCode, date1, date2);
        var responseXml = await SendSoapRequestAsync(config.BaseUrl, "GetQueryJSON", bodyXml, ct);

        return ParseGetQueryJsonResponse<T>(responseXml);
    }

    // ── SOAP XML Builders ───────────────────────────────────────────────────

    private static string BuildSetOrderXml(ArasKargoConfig config, ArasKargoShipmentRequest request)
    {
        var pieceDetailsXml = "";
        if (request.PieceDetails is { Count: > 0 })
        {
            var pieces = string.Join("", request.PieceDetails.Select(p =>
                $"""
                <aras:PieceDetail>
                    <aras:BarcodeNumber>{Escape(p.BarcodeNumber ?? "")}</aras:BarcodeNumber>
                    <aras:Weight>{p.Weight ?? 0}</aras:Weight>
                    <aras:VolumetricWeight>{p.VolumetricWeight ?? 0}</aras:VolumetricWeight>
                    <aras:Description>{Escape(p.Description ?? "")}</aras:Description>
                </aras:PieceDetail>
                """));
            pieceDetailsXml = $"<aras:PieceDetails>{pieces}</aras:PieceDetails>";
        }

        return $"""
            <aras:SetOrder xmlns:aras="{SoapNamespace}">
                <aras:orderInfo>
                    <aras:Order>
                        <aras:UserName>{Escape(config.UserName)}</aras:UserName>
                        <aras:Password>{Escape(config.Password)}</aras:Password>
                        <aras:IntegrationCode>{Escape(request.IntegrationCode)}</aras:IntegrationCode>
                        <aras:ReceiverName>{Escape(request.ReceiverName)}</aras:ReceiverName>
                        <aras:ReceiverPhone>{Escape(request.ReceiverPhone)}</aras:ReceiverPhone>
                        <aras:ReceiverCityName>{Escape(request.ReceiverCityName)}</aras:ReceiverCityName>
                        <aras:ReceiverTownName>{Escape(request.ReceiverTownName)}</aras:ReceiverTownName>
                        <aras:ReceiverAddress>{Escape(request.ReceiverAddress)}</aras:ReceiverAddress>
                        <aras:PieceCount>{request.PieceCount}</aras:PieceCount>
                        <aras:IsWorldWide>0</aras:IsWorldWide>
                        <aras:IsCOD>{(request.IsCod ? 1 : 0)}</aras:IsCOD>
                        <aras:CodAmount>{request.CodAmount}</aras:CodAmount>
                        <aras:PayorTypeCode>1</aras:PayorTypeCode>
                        <aras:Description>{Escape(request.Description ?? "")}</aras:Description>
                        <aras:InvoiceNumber>{Escape(request.InvoiceNumber ?? "")}</aras:InvoiceNumber>
                        {pieceDetailsXml}
                    </aras:Order>
                </aras:orderInfo>
            </aras:SetOrder>
            """;
    }

    private static string BuildCancelDispatchXml(ArasKargoConfig config, string integrationCode)
    {
        return $"""
            <aras:CancelDispatch xmlns:aras="{SoapNamespace}">
                <aras:userName>{Escape(config.UserName)}</aras:userName>
                <aras:password>{Escape(config.Password)}</aras:password>
                <aras:integrationCode>{Escape(integrationCode)}</aras:integrationCode>
            </aras:CancelDispatch>
            """;
    }

    private static string BuildGetQueryJsonXml(
        ArasKargoConfig config, int queryType,
        string? integrationCode, string? date1, string? date2)
    {
        return $"""
            <aras:GetQueryJSON xmlns:aras="{SoapNamespace}">
                <aras:loginInfo>
                    <aras:UserName>{Escape(config.UserName)}</aras:UserName>
                    <aras:Password>{Escape(config.Password)}</aras:Password>
                    <aras:CustomerCode>{Escape(config.CustomerCode)}</aras:CustomerCode>
                </aras:loginInfo>
                <aras:queryInfo>
                    <aras:QueryType>{queryType}</aras:QueryType>
                    <aras:IntegrationCode>{Escape(integrationCode ?? "")}</aras:IntegrationCode>
                    <aras:Date1>{Escape(date1 ?? "")}</aras:Date1>
                    <aras:Date2>{Escape(date2 ?? "")}</aras:Date2>
                </aras:queryInfo>
            </aras:GetQueryJSON>
            """;
    }

    // ── HTTP Transport ──────────────────────────────────────────────────────

    private async Task<string> SendSoapRequestAsync(
        string baseUrl, string soapAction, string bodyXml, CancellationToken ct)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="{SoapEnvelopeNs}">
                <soap:Body>
                    {bodyXml}
                </soap:Body>
            </soap:Envelope>
            """;

        using var client = httpClientFactory.CreateClient("ArasKargo");
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"{SoapNamespace}{soapAction}");

        logger.LogDebug("Aras Kargo SOAP request: {Action} -> {Url}", soapAction, baseUrl);

        var response = await client.PostAsync(baseUrl, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Aras Kargo SOAP error: {Status} {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Aras Kargo API hatasi: {response.StatusCode}");
        }

        return responseBody;
    }

    // ── Response Parsers ────────────────────────────────────────────────────

    private static ArasKargoOrderResult ParseSetOrderResponse(string responseXml)
    {
        try
        {
            var doc = XDocument.Parse(responseXml);
            var ns = XNamespace.Get(SoapNamespace);

            var resultInfo = doc.Descendants(ns + "OrderResultInfo").FirstOrDefault()
                ?? doc.Descendants("OrderResultInfo").FirstOrDefault();

            if (resultInfo is null)
                return new ArasKargoOrderResult("1", "Response parse edilemedi", null);

            return new ArasKargoOrderResult(
                ResultCode: resultInfo.Element(ns + "ResultCode")?.Value
                    ?? resultInfo.Element("ResultCode")?.Value ?? "1",
                ResultMessage: resultInfo.Element(ns + "ResultMessage")?.Value
                    ?? resultInfo.Element("ResultMessage")?.Value ?? "",
                BarcodeNumber: resultInfo.Element(ns + "BarcodeNumber")?.Value
                    ?? resultInfo.Element("BarcodeNumber")?.Value);
        }
        catch
        {
            return new ArasKargoOrderResult("1", "XML parse hatasi", null);
        }
    }

    private static bool ParseCancelDispatchResponse(string responseXml)
    {
        try
        {
            var doc = XDocument.Parse(responseXml);
            var ns = XNamespace.Get(SoapNamespace);

            var result = doc.Descendants(ns + "CancelDispatchResult").FirstOrDefault()
                ?? doc.Descendants("CancelDispatchResult").FirstOrDefault();

            return result?.Value?.Contains("true", StringComparison.OrdinalIgnoreCase) == true
                || result?.Value == "1";
        }
        catch
        {
            return false;
        }
    }

    private static T? ParseGetQueryJsonResponse<T>(string responseXml)
    {
        try
        {
            var doc = XDocument.Parse(responseXml);
            var ns = XNamespace.Get(SoapNamespace);

            var jsonResult = doc.Descendants(ns + "GetQueryJSONResult").FirstOrDefault()
                ?? doc.Descendants("GetQueryJSONResult").FirstOrDefault();

            if (jsonResult is null || string.IsNullOrWhiteSpace(jsonResult.Value))
                return default;

            return JsonSerializer.Deserialize<T>(jsonResult.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    private static string Escape(string value) =>
        System.Security.SecurityElement.Escape(value) ?? "";
}
