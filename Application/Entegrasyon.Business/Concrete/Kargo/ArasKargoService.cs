using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Aras Kargo SOAP API entegrasyon servisi.
/// Kargo gonderme, takip, iptal ve sorgulama islemlerini yonetir.
/// </summary>
public sealed class ArasKargoService(
    ArasKargoClient client,
    IApplicationLogManager applicationLogManager,
    ILogger<ArasKargoService> logger) : IArasKargoService
{
    // ── CreateShipmentAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<ArasKargoOrderResult>> CreateShipmentAsync(
        ArasKargoShipmentRequest request, CancellationToken ct = default)
    {
        // Validation
        var validationError = ValidateShipmentRequest(request);
        if (validationError is not null)
        {
            logger.LogWarning("Aras Kargo gonderi olusturma validasyon hatasi: {Error}", validationError);
            return new ErrorDataResult<ArasKargoOrderResult>(null!, validationError);
        }

        try
        {
            await applicationLogManager.AddLog(
                $"Aras Kargo gonderi olusturma basladi: {request.IntegrationCode}",
                LogType.Order, LogAction.Add, request, ct);

            var result = await client.SetOrderAsync(request, ct);

            if (result.ResultCode != "0")
            {
                logger.LogWarning("Aras Kargo gonderi olusturma başarısız: {Code} {Message}",
                    result.ResultCode, result.ResultMessage);
                await applicationLogManager.AddLog(
                    $"Aras Kargo gonderi olusturma başarısız: {result.ResultMessage}",
                    LogType.Order, LogAction.Add, result, ct);
                return new ErrorDataResult<ArasKargoOrderResult>(result, result.ResultMessage);
            }

            logger.LogInformation("Aras Kargo gonderi olusturuldu: {IntegrationCode} -> {Barcode}",
                request.IntegrationCode, result.BarcodeNumber);
            await applicationLogManager.AddLog(
                $"Aras Kargo gonderi basariyla olusturuldu: {request.IntegrationCode}",
                LogType.Order, LogAction.Add, result, ct);

            return new SuccessDataResult<ArasKargoOrderResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo gonderi olusturma hatasi: {IntegrationCode}", request.IntegrationCode);
            return new ErrorDataResult<ArasKargoOrderResult>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── CancelShipmentAsync ─────────────────────────────────────────────────

    public async Task<IResult> CancelShipmentAsync(
        string integrationCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(integrationCode))
        {
            logger.LogWarning("Aras Kargo iptal: integrationCode bos olamaz");
            return new ErrorResult("integrationCode bos olamaz.");
        }

        try
        {
            await applicationLogManager.AddLog(
                $"Aras Kargo gonderi iptal basladi: {integrationCode}",
                LogType.Order, LogAction.Delete, new { integrationCode }, ct);

            var success = await client.CancelDispatchAsync(integrationCode, ct);

            if (!success)
            {
                logger.LogWarning("Aras Kargo gonderi iptal başarısız: {IntegrationCode}", integrationCode);
                await applicationLogManager.AddLog(
                    $"Aras Kargo gonderi iptal başarısız: {integrationCode}",
                    LogType.Order, LogAction.Delete, new { integrationCode }, ct);
                return new ErrorResult("Gonderi iptal edilemedi.");
            }

            logger.LogInformation("Aras Kargo gonderi iptal edildi: {IntegrationCode}", integrationCode);
            await applicationLogManager.AddLog(
                $"Aras Kargo gonderi basariyla iptal edildi: {integrationCode}",
                LogType.Order, LogAction.Delete, new { integrationCode }, ct);

            return new SuccessResult("Gonderi basariyla iptal edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo gonderi iptal hatasi: {IntegrationCode}", integrationCode);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    // ── TrackShipmentAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<ArasKargoTrackingResult>> TrackShipmentAsync(
        string integrationCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(integrationCode))
        {
            logger.LogWarning("Aras Kargo takip: integrationCode bos olamaz");
            return new ErrorDataResult<ArasKargoTrackingResult>(null!, "integrationCode bos olamaz.");
        }

        try
        {
            var result = await client.GetQueryJsonAsync<ArasKargoTrackingResult>(
                ArasKargoQueryType.CargoInformation, integrationCode, ct: ct);

            if (result is null)
            {
                logger.LogWarning("Aras Kargo takip sonucu bos: {IntegrationCode}", integrationCode);
                return new ErrorDataResult<ArasKargoTrackingResult>(null!, "Kargo bilgisi bulunamadı.");
            }

            logger.LogInformation("Aras Kargo takip basarili: {IntegrationCode} -> {Status}",
                integrationCode, result.Status);
            return new SuccessDataResult<ArasKargoTrackingResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo takip hatasi: {IntegrationCode}", integrationCode);
            return new ErrorDataResult<ArasKargoTrackingResult>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetShipmentMovementsAsync ───────────────────────────────────────────

    public async Task<IDataResult<List<ArasKargoMovement>>> GetShipmentMovementsAsync(
        string integrationCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(integrationCode))
        {
            logger.LogWarning("Aras Kargo hareket sorgulama: integrationCode bos olamaz");
            return new ErrorDataResult<List<ArasKargoMovement>>(null!, "integrationCode bos olamaz.");
        }

        try
        {
            var result = await client.GetQueryJsonAsync<List<ArasKargoMovement>>(
                ArasKargoQueryType.CargoMovementInformation, integrationCode, ct: ct);

            if (result is null)
            {
                logger.LogWarning("Aras Kargo hareket bilgisi bos: {IntegrationCode}", integrationCode);
                return new SuccessDataResult<List<ArasKargoMovement>>(new List<ArasKargoMovement>());
            }

            logger.LogInformation("Aras Kargo hareket sorgulama basarili: {IntegrationCode} -> {Count} hareket",
                integrationCode, result.Count);
            return new SuccessDataResult<List<ArasKargoMovement>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo hareket sorgulama hatasi: {IntegrationCode}", integrationCode);
            return new ErrorDataResult<List<ArasKargoMovement>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetShipmentsByDateRangeAsync ────────────────────────────────────────

    public async Task<IDataResult<List<ArasKargoShipmentSummary>>> GetShipmentsByDateRangeAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        if (startDate > endDate)
        {
            logger.LogWarning("Aras Kargo tarih araligi Hatalı: {Start} > {End}", startDate, endDate);
            return new ErrorDataResult<List<ArasKargoShipmentSummary>>(
                null!, "Başlangıç tarihi Bitiş tarihinden buyuk olamaz.");
        }

        try
        {
            var date1 = startDate.ToString("dd-MM-yyyy");
            var date2 = endDate.ToString("dd-MM-yyyy");

            var result = await client.GetQueryJsonAsync<List<ArasKargoShipmentSummary>>(
                ArasKargoQueryType.CargoWaybillBetweenDate, date1: date1, date2: date2, ct: ct);

            if (result is null)
                return new SuccessDataResult<List<ArasKargoShipmentSummary>>(new List<ArasKargoShipmentSummary>());

            logger.LogInformation("Aras Kargo tarih araligi sorgulama: {Count} kargo", result.Count);
            return new SuccessDataResult<List<ArasKargoShipmentSummary>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo tarih araligi sorgulama hatasi");
            return new ErrorDataResult<List<ArasKargoShipmentSummary>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetUndeliveredShipmentsAsync ────────────────────────────────────────

    public async Task<IDataResult<List<ArasKargoShipmentSummary>>> GetUndeliveredShipmentsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetQueryJsonAsync<List<ArasKargoShipmentSummary>>(
                ArasKargoQueryType.CargoUndelivered, ct: ct);

            if (result is null)
                return new SuccessDataResult<List<ArasKargoShipmentSummary>>(new List<ArasKargoShipmentSummary>());

            logger.LogInformation("Aras Kargo teslim edilmemis kargolar: {Count} kargo", result.Count);
            return new SuccessDataResult<List<ArasKargoShipmentSummary>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo teslim edilmemis sorgulama hatasi");
            return new ErrorDataResult<List<ArasKargoShipmentSummary>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── Validation ──────────────────────────────────────────────────────────

    private static string? ValidateShipmentRequest(ArasKargoShipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IntegrationCode))
            return "IntegrationCode bos olamaz.";

        if (string.IsNullOrWhiteSpace(request.ReceiverName))
            return "Alici adi (alici) bos olamaz.";

        if (string.IsNullOrWhiteSpace(request.ReceiverPhone))
            return "Alici telefon numarasi (telefon) bos olamaz.";

        if (string.IsNullOrWhiteSpace(request.ReceiverCityName))
            return "Alici sehir adi bos olamaz.";

        if (string.IsNullOrWhiteSpace(request.ReceiverTownName))
            return "Alici ilce adi bos olamaz.";

        if (string.IsNullOrWhiteSpace(request.ReceiverAddress))
            return "Alici adresi (adres) bos olamaz.";

        if (request.PieceCount <= 0)
            return "Parca Sayısı (parca) 0'dan buyuk olmalidir.";

        return null;
    }
}
