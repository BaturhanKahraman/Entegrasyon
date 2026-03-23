using System.Globalization;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Yurtici Kargo SOAP API uzerinden kargo olusturma, sorgulama ve iptal islemleri.
/// Tum marketplace siparisleri icin genel kargo servisi.
/// </summary>
public sealed class YurticiKargoService(
    IYurticiKargoClient client,
    IApplicationLogManager applicationLogManager,
    ILogger<YurticiKargoService> logger) : IYurticiKargoService
{
    private const string SoapNamespace = "http://kargo.yurtici.com/";

    // ── CreateShipmentAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<YurticiCreateShipmentResponse>> CreateShipmentAsync(
        YurticiCreateShipmentRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            const string msg = "Kargo olusturma istegi bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorDataResult<YurticiCreateShipmentResponse>(null!, msg);
        }

        if (string.IsNullOrWhiteSpace(request.CargoKey))
        {
            const string msg = "CargoKey bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorDataResult<YurticiCreateShipmentResponse>(null!, msg);
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverCustName))
        {
            const string msg = "Alici adi bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorDataResult<YurticiCreateShipmentResponse>(null!, msg);
        }

        try
        {
            var soapBody = BuildCreateShipmentSoapBody(request);
            var responseXml = await client.SendSoapRequestAsync("createShipment", soapBody, ct);

            var result = ParseCreateShipmentResponse(responseXml);

            if (result.OutFlag != "1")
            {
                logger.LogWarning("YurticiKargo createShipment failed: {OutResult}", result.OutResult);
                await applicationLogManager.AddLog(
                    $"Yurtici Kargo gonderi olusturma basarisiz: {result.OutResult}",
                    LogType.Order, LogAction.Update, null, ct);
                return new ErrorDataResult<YurticiCreateShipmentResponse>(result, result.OutResult);
            }

            logger.LogInformation("YurticiKargo shipment created: CargoKey={CargoKey}, JobId={JobId}",
                request.CargoKey, result.JobId);
            return new SuccessDataResult<YurticiCreateShipmentResponse>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YurticiKargo createShipment exception for CargoKey={CargoKey}", request.CargoKey);
            return new ErrorDataResult<YurticiCreateShipmentResponse>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── QueryShipmentAsync ───────────────────────────────────────────────────

    public async Task<IDataResult<List<YurticiShipmentInfo>>> QueryShipmentAsync(
        YurticiQueryShipmentRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            const string msg = "Kargo sorgulama istegi bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorDataResult<List<YurticiShipmentInfo>>(null!, msg);
        }

        if (request.Keys is null || request.Keys.Length == 0)
        {
            const string msg = "Sorgu anahtarlari bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorDataResult<List<YurticiShipmentInfo>>(null!, msg);
        }

        try
        {
            var soapBody = BuildQueryShipmentSoapBody(request);
            var responseXml = await client.SendSoapRequestAsync("queryShipment", soapBody, ct);

            var shipments = ParseQueryShipmentResponse(responseXml);

            logger.LogInformation("YurticiKargo query: {Count} shipment(s) found", shipments.Count);
            return new SuccessDataResult<List<YurticiShipmentInfo>>(shipments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YurticiKargo queryShipment exception");
            return new ErrorDataResult<List<YurticiShipmentInfo>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── CancelShipmentAsync ──────────────────────────────────────────────────

    public async Task<IResult> CancelShipmentAsync(
        string cargoKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cargoKey))
        {
            const string msg = "CargoKey bos olamaz.";
            logger.LogWarning("YurticiKargoService: {Message}", msg);
            return new ErrorResult(msg);
        }

        try
        {
            var soapBody = BuildCancelShipmentSoapBody(cargoKey);
            var responseXml = await client.SendSoapRequestAsync("cancelShipment", soapBody, ct);

            var result = ParseCancelShipmentResponse(responseXml);

            if (result.OutFlag != "1")
            {
                logger.LogWarning("YurticiKargo cancelShipment failed: {OutResult}", result.OutResult);
                await applicationLogManager.AddLog(
                    $"Yurtici Kargo iptal basarisiz: {result.OutResult}",
                    LogType.Order, LogAction.Update, null, ct);
                return new ErrorResult(result.OutResult);
            }

            logger.LogInformation("YurticiKargo shipment cancelled: CargoKey={CargoKey}", cargoKey);
            return new SuccessResult("Kargo basariyla iptal edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YurticiKargo cancelShipment exception for CargoKey={CargoKey}", cargoKey);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    // ── SOAP Body Builders ───────────────────────────────────────────────────

    internal static string BuildCreateShipmentSoapBody(YurticiCreateShipmentRequest req)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ship=\"");
        sb.Append(SoapNamespace);
        sb.Append("\">");
        sb.Append("<soapenv:Body>");
        sb.Append("<ship:createShipment>");
        sb.Append("<ShippingOrderVO>");
        sb.AppendFormat("<cargoKey>{0}</cargoKey>", EscapeXml(req.CargoKey));
        sb.AppendFormat("<invoiceKey>{0}</invoiceKey>", EscapeXml(req.InvoiceKey));
        sb.AppendFormat("<receiverCustName>{0}</receiverCustName>", EscapeXml(req.ReceiverCustName));
        sb.AppendFormat("<receiverAddress>{0}</receiverAddress>", EscapeXml(req.ReceiverAddress));
        sb.AppendFormat("<cityName>{0}</cityName>", EscapeXml(req.CityName));
        sb.AppendFormat("<townName>{0}</townName>", EscapeXml(req.TownName));
        sb.AppendFormat("<receiverPhone1>{0}</receiverPhone1>", EscapeXml(req.ReceiverPhone1));
        sb.AppendFormat("<cargoCount>{0}</cargoCount>", req.CargoCount);

        if (!string.IsNullOrWhiteSpace(req.ReceiverPhone2))
            sb.AppendFormat("<receiverPhone2>{0}</receiverPhone2>", EscapeXml(req.ReceiverPhone2));
        if (!string.IsNullOrWhiteSpace(req.ReceiverPhone3))
            sb.AppendFormat("<receiverPhone3>{0}</receiverPhone3>", EscapeXml(req.ReceiverPhone3));
        if (!string.IsNullOrWhiteSpace(req.EmailAddress))
            sb.AppendFormat("<emailAddress>{0}</emailAddress>", EscapeXml(req.EmailAddress));
        if (req.Desi.HasValue)
            sb.AppendFormat(CultureInfo.InvariantCulture, "<desi>{0}</desi>", req.Desi.Value);
        if (req.Kg.HasValue)
            sb.AppendFormat(CultureInfo.InvariantCulture, "<kg>{0}</kg>", req.Kg.Value);
        if (!string.IsNullOrWhiteSpace(req.Description))
            sb.AppendFormat("<description>{0}</description>", EscapeXml(req.Description));
        if (req.TtInvoiceAmount.HasValue)
            sb.AppendFormat(CultureInfo.InvariantCulture, "<ttInvoiceAmount>{0}</ttInvoiceAmount>", req.TtInvoiceAmount.Value);
        if (req.TtCollectionType.HasValue)
            sb.AppendFormat("<ttCollectionType>{0}</ttCollectionType>", req.TtCollectionType.Value);
        if (!string.IsNullOrWhiteSpace(req.TtDocumentId))
            sb.AppendFormat("<ttDocumentId>{0}</ttDocumentId>", EscapeXml(req.TtDocumentId));
        if (req.TtDocumentSaveType.HasValue)
            sb.AppendFormat("<ttDocumentSaveType>{0}</ttDocumentSaveType>", req.TtDocumentSaveType.Value);
        if (req.DcSelectedCredit.HasValue)
            sb.AppendFormat("<dcSelectedCredit>{0}</dcSelectedCredit>", req.DcSelectedCredit.Value);
        if (req.DcCreditRule.HasValue)
            sb.AppendFormat("<dcCreditRule>{0}</dcCreditRule>", req.DcCreditRule.Value);

        sb.Append("</ShippingOrderVO>");
        sb.Append("</ship:createShipment>");
        sb.Append("</soapenv:Body>");
        sb.Append("</soapenv:Envelope>");

        return sb.ToString();
    }

    internal static string BuildQueryShipmentSoapBody(YurticiQueryShipmentRequest req)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ship=\"");
        sb.Append(SoapNamespace);
        sb.Append("\">");
        sb.Append("<soapenv:Body>");
        sb.Append("<ship:queryShipment>");

        foreach (var key in req.Keys)
            sb.AppendFormat("<keys>{0}</keys>", EscapeXml(key));

        sb.AppendFormat("<keyType>{0}</keyType>", req.KeyType);
        sb.AppendFormat("<addHistoricalData>{0}</addHistoricalData>", req.AddHistoricalData.ToString().ToLowerInvariant());
        sb.AppendFormat("<onlyTracking>{0}</onlyTracking>", req.OnlyTracking.ToString().ToLowerInvariant());

        sb.Append("</ship:queryShipment>");
        sb.Append("</soapenv:Body>");
        sb.Append("</soapenv:Envelope>");

        return sb.ToString();
    }

    internal static string BuildCancelShipmentSoapBody(string cargoKey)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ship=\"");
        sb.Append(SoapNamespace);
        sb.Append("\">");
        sb.Append("<soapenv:Body>");
        sb.Append("<ship:cancelShipment>");
        sb.AppendFormat("<cargoKeys>{0}</cargoKeys>", EscapeXml(cargoKey));
        sb.Append("</ship:cancelShipment>");
        sb.Append("</soapenv:Body>");
        sb.Append("</soapenv:Envelope>");

        return sb.ToString();
    }

    // ── XML Response Parsers ─────────────────────────────────────────────────

    internal static YurticiCreateShipmentResponse ParseCreateShipmentResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ns = XNamespace.Get(SoapNamespace);

        // SOAP response'unda createShipmentReturn veya return elementi aranir
        var returnEl = doc.Descendants(ns + "createShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("createShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("return").FirstOrDefault();

        if (returnEl is null)
            return new YurticiCreateShipmentResponse("0", "Response parse edilemedi.", null);

        var outFlag = GetElementValue(returnEl, "outFlag") ?? "0";
        var outResult = GetElementValue(returnEl, "outResult") ?? "";
        var jobId = GetElementValue(returnEl, "jobId");

        return new YurticiCreateShipmentResponse(outFlag, outResult, jobId);
    }

    internal static List<YurticiShipmentInfo> ParseQueryShipmentResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ns = XNamespace.Get(SoapNamespace);
        var result = new List<YurticiShipmentInfo>();

        var returnEl = doc.Descendants(ns + "queryShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("queryShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("return").FirstOrDefault();

        if (returnEl is null)
            return result;

        // shippingDeliveryDetailVO elementlerini bul
        var details = returnEl.Descendants("shippingDeliveryDetailVO")
            .Concat(returnEl.Descendants(ns + "shippingDeliveryDetailVO"));

        foreach (var detail in details)
        {
            var cargoKey = GetElementValue(detail, "cargoKey") ?? "";
            var invoiceKey = GetElementValue(detail, "invKeys");
            var opCode = int.TryParse(GetElementValue(detail, "operationCode"), out var oc) ? oc : 0;
            var opMessage = GetElementValue(detail, "operationMessage");

            DateTime? deliveryDate = null;
            var ddStr = GetElementValue(detail, "deliveryDate");
            if (!string.IsNullOrWhiteSpace(ddStr) && DateTime.TryParse(ddStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd))
                deliveryDate = dd;

            var deliveredTo = GetElementValue(detail, "deliveredTo");
            int? unitCount = null;
            if (int.TryParse(GetElementValue(detail, "unitCount"), out var uc))
                unitCount = uc;

            result.Add(new YurticiShipmentInfo(cargoKey, invoiceKey, opCode, opMessage, deliveryDate, deliveredTo, unitCount));
        }

        return result;
    }

    internal static YurticiCancelShipmentResponse ParseCancelShipmentResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ns = XNamespace.Get(SoapNamespace);

        var returnEl = doc.Descendants(ns + "cancelShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("cancelShipmentReturn").FirstOrDefault()
            ?? doc.Descendants("return").FirstOrDefault();

        if (returnEl is null)
            return new YurticiCancelShipmentResponse("0", "Response parse edilemedi.", null);

        var outFlag = GetElementValue(returnEl, "outFlag") ?? "0";
        var outResult = GetElementValue(returnEl, "outResult") ?? "";
        var cargoKey = GetElementValue(returnEl, "cargoKey");

        return new YurticiCancelShipmentResponse(outFlag, outResult, cargoKey);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetElementValue(XElement parent, string localName)
    {
        var el = parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        return el?.Value;
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
