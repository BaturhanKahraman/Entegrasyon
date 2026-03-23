using System.Globalization;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Surat Kargo is mantigi servisi.
/// SuratKargoClient uzerinden SOAP istekleri gonderir, response'lari parse eder.
/// </summary>
public sealed class SuratKargoService(
    ISuratKargoClient soapClient,
    IApplicationLogManager applicationLogManager,
    ILogger<SuratKargoService> logger) : ISuratKargoService
{
    private const string SoapActionBase = "http://tempuri.org/";

    // ── CreateShipmentAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<SuratKargoShipmentResult>> CreateShipmentAsync(
        SuratKargoShipmentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            const string msg = "Alıcı adı boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<SuratKargoShipmentResult>(null!, msg);
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverPhone))
        {
            const string msg = "Alıcı telefon numarası boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<SuratKargoShipmentResult>(null!, msg);
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverAddress))
        {
            const string msg = "Alıcı adresi boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<SuratKargoShipmentResult>(null!, msg);
        }

        if (request.PieceCount <= 0)
        {
            const string msg = "Parça sayısı sıfırdan büyük olmalıdır.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<SuratKargoShipmentResult>(null!, msg);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["receiverName"] = request.ReceiverName,
                ["receiverAddress"] = request.ReceiverAddress,
                ["receiverPhone1"] = request.ReceiverPhone,
                ["receiverCityName"] = request.ReceiverCityName,
                ["receiverTownName"] = request.ReceiverTownName,
                ["pieceCount"] = request.PieceCount.ToString()
            };

            if (!string.IsNullOrWhiteSpace(request.ReferenceNo))
                parameters["referenceNo"] = request.ReferenceNo;

            if (request.Weight.HasValue)
                parameters["weight"] = request.Weight.Value.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(request.ReceiverPhone2))
                parameters["receiverPhone2"] = request.ReceiverPhone2;

            if (!string.IsNullOrWhiteSpace(request.Description))
                parameters["description"] = request.Description;

            if (request.IsCOD)
            {
                parameters["isCOD"] = "true";
                if (request.CodAmount.HasValue)
                    parameters["codAmount"] = request.CodAmount.Value.ToString(CultureInfo.InvariantCulture);
            }

            var response = await soapClient.SendAsync(
                $"{SoapActionBase}CreateShipment", "CreateShipment", parameters, ct);

            var resultCode = ParseInt(response, "resultCode");
            var resultMessage = ParseString(response, "resultMessage");
            var trackingNumber = ParseString(response, "shippingOrderNo");
            var barcodeNo = ParseString(response, "barcodeNo");

            if (resultCode != 0 || string.IsNullOrWhiteSpace(trackingNumber))
            {
                var errorMsg = $"Gönderi oluşturulamadı: {resultMessage ?? "Bilinmeyen hata"}";
                logger.LogWarning("SuratKargo CreateShipment failed: code={Code}, msg={Msg}", resultCode, resultMessage);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Add, null, ct);
                return new ErrorDataResult<SuratKargoShipmentResult>(null!, errorMsg);
            }

            var result = new SuratKargoShipmentResult(trackingNumber, barcodeNo, resultCode, resultMessage);
            logger.LogInformation("SuratKargo shipment created: tracking={Tracking}, ref={Ref}",
                trackingNumber, request.ReferenceNo);

            return new SuccessDataResult<SuratKargoShipmentResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuratKargo CreateShipment exception");
            return new ErrorDataResult<SuratKargoShipmentResult>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── QueryShipmentAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<SuratKargoTrackingResult>> QueryShipmentAsync(
        string trackingNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            const string msg = "Takip numarası boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<SuratKargoTrackingResult>(null!, msg);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["shippingOrderNo"] = trackingNumber
            };

            var response = await soapClient.SendAsync(
                $"{SoapActionBase}QueryShipmentInfo", "QueryShipmentInfo", parameters, ct);

            var resultCode = ParseInt(response, "resultCode");
            var resultMessage = ParseString(response, "resultMessage");

            if (resultCode != 0)
            {
                var errorMsg = $"Gönderi sorgulanamadı: {resultMessage ?? "Bilinmeyen hata"}";
                logger.LogWarning("SuratKargo QueryShipment failed: code={Code}, msg={Msg}", resultCode, resultMessage);
                return new ErrorDataResult<SuratKargoTrackingResult>(null!, errorMsg);
            }

            var status = ParseString(response, "shipmentStatus") ?? "Bilinmiyor";
            var deliveryDateStr = ParseString(response, "deliveryDate");
            DateTime? deliveryDate = null;
            if (!string.IsNullOrWhiteSpace(deliveryDateStr) &&
                DateTime.TryParse(deliveryDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                deliveryDate = dt;
            }

            var movements = ParseMovements(response);

            var result = new SuratKargoTrackingResult(trackingNumber, status, deliveryDate, movements);
            logger.LogInformation("SuratKargo tracking: {Tracking} status={Status}", trackingNumber, status);

            return new SuccessDataResult<SuratKargoTrackingResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuratKargo QueryShipment exception for {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<SuratKargoTrackingResult>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── CancelShipmentAsync ─────────────────────────────────────────────────

    public async Task<IResult> CancelShipmentAsync(
        string trackingNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            const string msg = "Takip numarası boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorResult(msg);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["shippingOrderNo"] = trackingNumber
            };

            var response = await soapClient.SendAsync(
                $"{SoapActionBase}CancelShipment", "CancelShipment", parameters, ct);

            var resultCode = ParseInt(response, "resultCode");
            var resultMessage = ParseString(response, "resultMessage");

            if (resultCode != 0)
            {
                var errorMsg = $"Gönderi iptal edilemedi: {resultMessage ?? "Bilinmeyen hata"}";
                logger.LogWarning("SuratKargo CancelShipment failed: code={Code}, msg={Msg}", resultCode, resultMessage);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Update, null, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("SuratKargo shipment cancelled: {Tracking}", trackingNumber);
            await applicationLogManager.AddLog(
                $"Sürat Kargo gönderi iptal edildi: {trackingNumber}",
                LogType.Order, LogAction.Update, null, ct);
            return new SuccessResult("Gönderi başarıyla iptal edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuratKargo CancelShipment exception for {TrackingNumber}", trackingNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    // ── GetBarcodeAsync ─────────────────────────────────────────────────────

    public async Task<IDataResult<string>> GetBarcodeAsync(
        string referenceNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            const string msg = "Referans numarası boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<string>(null!, msg);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["referenceNo"] = referenceNo
            };

            var response = await soapClient.SendAsync(
                $"{SoapActionBase}GetBarcodeByReferenceNo", "GetBarcodeByReferenceNo", parameters, ct);

            var resultCode = ParseInt(response, "resultCode");
            var barcodeData = ParseString(response, "barcodeBase64")
                              ?? ParseString(response, "barcodeNo");

            if (resultCode != 0 || string.IsNullOrWhiteSpace(barcodeData))
            {
                var resultMessage = ParseString(response, "resultMessage");
                var errorMsg = $"Barkod alınamadı: {resultMessage ?? "Bilinmeyen hata"}";
                logger.LogWarning("SuratKargo GetBarcode failed: code={Code}", resultCode);
                return new ErrorDataResult<string>(null!, errorMsg);
            }

            logger.LogInformation("SuratKargo barcode retrieved for ref={Ref}", referenceNo);
            return new SuccessDataResult<string>(barcodeData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuratKargo GetBarcode exception for {ReferenceNo}", referenceNo);
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetShipmentLabelAsync ───────────────────────────────────────────────

    public async Task<IDataResult<string>> GetShipmentLabelAsync(
        string trackingNumber, string? labelFormat = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            const string msg = "Takip numarası boş olamaz.";
            logger.LogWarning("SuratKargoService: {Message}", msg);
            return new ErrorDataResult<string>(null!, msg);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["shippingOrderNo"] = trackingNumber
            };

            if (!string.IsNullOrWhiteSpace(labelFormat))
                parameters["labelFormat"] = labelFormat;

            var response = await soapClient.SendAsync(
                $"{SoapActionBase}GetShipmentLabel", "GetShipmentLabel", parameters, ct);

            var resultCode = ParseInt(response, "resultCode");
            var labelData = ParseString(response, "labelData");

            if (resultCode != 0 || string.IsNullOrWhiteSpace(labelData))
            {
                var resultMessage = ParseString(response, "resultMessage");
                var errorMsg = $"Etiket alınamadı: {resultMessage ?? "Bilinmeyen hata"}";
                logger.LogWarning("SuratKargo GetLabel failed: code={Code}", resultCode);
                return new ErrorDataResult<string>(null!, errorMsg);
            }

            logger.LogInformation("SuratKargo label retrieved for tracking={Tracking}", trackingNumber);
            return new SuccessDataResult<string>(labelData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuratKargo GetLabel exception for {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── Helper Methods ──────────────────────────────────────────────────────

    private static string? ParseString(XElement parent, string localName)
    {
        // Namespace-aware search, fallback to local name only
        var element = parent.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
        return element?.Value;
    }

    private static int ParseInt(XElement parent, string localName)
    {
        var value = ParseString(parent, localName);
        return int.TryParse(value, out var result) ? result : -1;
    }

    private static List<SuratKargoMovement> ParseMovements(XElement parent)
    {
        var movements = new List<SuratKargoMovement>();

        var movementElements = parent.Descendants()
            .Where(e => e.Name.LocalName.Equals("movement", StringComparison.OrdinalIgnoreCase)
                     || e.Name.LocalName.Equals("ShipmentMovement", StringComparison.OrdinalIgnoreCase));

        foreach (var element in movementElements)
        {
            var dateStr = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals("date", StringComparison.OrdinalIgnoreCase))?.Value;

            var location = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals("location", StringComparison.OrdinalIgnoreCase))?.Value ?? "";

            var description = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals("description", StringComparison.OrdinalIgnoreCase))?.Value ?? "";

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                movements.Add(new SuratKargoMovement(date, location, description));
            }
        }

        return movements;
    }
}
