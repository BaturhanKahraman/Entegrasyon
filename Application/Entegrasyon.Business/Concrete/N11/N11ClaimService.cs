using System.Globalization;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API ile gerçek iptal ve iade talep işlemleri.
/// ClaimCancelService ve ReturnService WSDL uç noktalarına SOAP çağrıları gerçekleştirir.
/// </summary>
public sealed class N11ClaimService(
    IN11SoapClient soapClient,
    ILogger<N11ClaimService> logger) : IN11ClaimService
{
    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// N11 SOAP yanıtındaki result/status alanını kontrol eder.
    /// Failure ise ErrorResult, başarı ise SuccessResult döner.
    /// </summary>
    private static IResult CheckN11ResponseStatus(XElement response)
    {
        var status = response.Element("result")?.Element("status")?.Value;
        if (status == "failure")
        {
            var errorMessage = response.Element("result")?.Element("errorMessage")?.Value
                ?? "Bilinmeyen N11 hatası";
            return new ErrorResult(errorMessage);
        }
        return new SuccessResult();
    }

    // -----------------------------------------------------------------------
    // GetCancelClaimsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ClaimCancelDto>>> GetCancelClaimsAsync(string? status = null, int page = 0)
    {
        var request = new XElement(Ns + "ClaimCancelListRequest",
            new XElement("searchData",
                new XElement("status", status ?? "REQUESTED")),
            new XElement("pagingData",
                new XElement("currentPage", page)));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimCancelService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetCancelClaims SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ClaimCancelDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ClaimCancelList başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ClaimCancelDto>>(null!, statusCheck.Message);
        }

        var claims = response
            .Element("claimCancelList")?
            .Elements("claimCancel")
            .Select(ParseCancelClaim)
            .ToList() ?? [];

        logger.LogInformation("N11 GetCancelClaims başarılı — {Count} iptal talebi alındı", claims.Count);
        return new SuccessDataResult<List<N11ClaimCancelDto>>(claims);
    }

    // -----------------------------------------------------------------------
    // ApproveCancelAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ApproveCancelAsync(long claimCancelId)
    {
        var request = new XElement(Ns + "ClaimCancelApproveRequest",
            new XElement("claimCancelId", claimCancelId));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimCancelService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 ApproveCancelAsync SOAP çağrısı başarısız — ClaimCancelId={ClaimCancelId}", claimCancelId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ApproveCancelAsync başarısız — ClaimCancelId={ClaimCancelId}, Message={Message}",
                claimCancelId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 ApproveCancelAsync başarılı — ClaimCancelId={ClaimCancelId}", claimCancelId);
        return new SuccessResult("İptal talebi onaylandı.");
    }

    // -----------------------------------------------------------------------
    // DenyCancelAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> DenyCancelAsync(long claimCancelId, long denyReasonId, string? denyReasonNote = null)
    {
        var request = new XElement(Ns + "ClaimCancelDenyRequest",
            new XElement("claimCancelId", claimCancelId),
            new XElement("denyReasonId", denyReasonId),
            new XElement("denyReasonNote", denyReasonNote ?? string.Empty));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimCancelService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 DenyCancelAsync SOAP çağrısı başarısız — ClaimCancelId={ClaimCancelId}", claimCancelId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 DenyCancelAsync başarısız — ClaimCancelId={ClaimCancelId}, Message={Message}",
                claimCancelId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 DenyCancelAsync başarılı — ClaimCancelId={ClaimCancelId}", claimCancelId);
        return new SuccessResult("İptal talebi reddedildi.");
    }

    // -----------------------------------------------------------------------
    // GetCancelDenyReasonsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ReasonTypeDto>>> GetCancelDenyReasonsAsync()
    {
        var request = new XElement(Ns + "ClaimCancelDenyReasonTypeRequest");

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimCancelService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetCancelDenyReasonsAsync SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 GetCancelDenyReasonsAsync başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, statusCheck.Message);
        }

        var reasons = response
            .Element("denyReasonTypeDataList")?
            .Elements("denyReasonType")
            .Select(ParseReasonType)
            .ToList() ?? [];

        logger.LogInformation("N11 GetCancelDenyReasonsAsync başarılı — {Count} neden alındı", reasons.Count);
        return new SuccessDataResult<List<N11ReasonTypeDto>>(reasons);
    }

    // -----------------------------------------------------------------------
    // GetReturnClaimsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ClaimReturnDto>>> GetReturnClaimsAsync(string? status = null, int page = 0)
    {
        var request = new XElement(Ns + "ClaimReturnListRequest",
            new XElement("searchData",
                new XElement("status", status ?? "ALL")),
            new XElement("pagingData",
                new XElement("currentPage", page)));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetReturnClaims SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ClaimReturnDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ClaimReturnList başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ClaimReturnDto>>(null!, statusCheck.Message);
        }

        var claims = response
            .Element("claimReturnList")?
            .Elements("claimReturn")
            .Select(ParseReturnClaim)
            .ToList() ?? [];

        logger.LogInformation("N11 GetReturnClaims başarılı — {Count} iade talebi alındı", claims.Count);
        return new SuccessDataResult<List<N11ClaimReturnDto>>(claims);
    }

    // -----------------------------------------------------------------------
    // ApproveReturnAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ApproveReturnAsync(long claimReturnId)
    {
        // Note: N11 API uses claimCancelId field name even for return approve (API naming inconsistency)
        var request = new XElement(Ns + "ClaimReturnApproveRequest",
            new XElement("claimCancelId", claimReturnId));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 ApproveReturnAsync SOAP çağrısı başarısız — ClaimReturnId={ClaimReturnId}", claimReturnId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ApproveReturnAsync başarısız — ClaimReturnId={ClaimReturnId}, Message={Message}",
                claimReturnId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 ApproveReturnAsync başarılı — ClaimReturnId={ClaimReturnId}", claimReturnId);
        return new SuccessResult("İade talebi onaylandı.");
    }

    // -----------------------------------------------------------------------
    // DenyReturnAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> DenyReturnAsync(long claimReturnId, long denyReasonId, string? denyReasonNote = null, string? returnShipmentType = null)
    {
        var request = new XElement(Ns + "ClaimReturnDenyRequest",
            new XElement("claimReturnId", claimReturnId),
            new XElement("denyReasonId", denyReasonId),
            new XElement("denyReasonNote", denyReasonNote ?? string.Empty),
            new XElement("returnShipmentType", returnShipmentType ?? string.Empty));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 DenyReturnAsync SOAP çağrısı başarısız — ClaimReturnId={ClaimReturnId}", claimReturnId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 DenyReturnAsync başarısız — ClaimReturnId={ClaimReturnId}, Message={Message}",
                claimReturnId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 DenyReturnAsync başarılı — ClaimReturnId={ClaimReturnId}", claimReturnId);
        return new SuccessResult("İade talebi reddedildi.");
    }

    // -----------------------------------------------------------------------
    // PendReturnAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> PendReturnAsync(long claimReturnId, long pendingReasonId, int pendingDayCount, string? pendingReasonNote = null)
    {
        var request = new XElement(Ns + "ClaimReturnPendingRequest",
            new XElement("claimReturnId", claimReturnId),
            new XElement("pendingReasonId", pendingReasonId),
            new XElement("pendingDayCount", pendingDayCount),
            new XElement("pendingReasonNote", pendingReasonNote ?? string.Empty));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 PendReturnAsync SOAP çağrısı başarısız — ClaimReturnId={ClaimReturnId}", claimReturnId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 PendReturnAsync başarısız — ClaimReturnId={ClaimReturnId}, Message={Message}",
                claimReturnId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 PendReturnAsync başarılı — ClaimReturnId={ClaimReturnId}", claimReturnId);
        return new SuccessResult("İade talebi beklemeye alındı.");
    }

    // -----------------------------------------------------------------------
    // GetReturnDenyReasonsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnDenyReasonsAsync()
    {
        var request = new XElement(Ns + "ClaimReturnDenyReasonTypesRequest");

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetReturnDenyReasonsAsync SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 GetReturnDenyReasonsAsync başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, statusCheck.Message);
        }

        var reasons = response
            .Element("denyReasonTypeDataList")?
            .Elements("denyReasonType")
            .Select(ParseReasonType)
            .ToList() ?? [];

        logger.LogInformation("N11 GetReturnDenyReasonsAsync başarılı — {Count} neden alındı", reasons.Count);
        return new SuccessDataResult<List<N11ReasonTypeDto>>(reasons);
    }

    // -----------------------------------------------------------------------
    // GetReturnPendingReasonsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnPendingReasonsAsync()
    {
        var request = new XElement(Ns + "ClaimReturnPendingReasonTypesRequest");

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ReturnService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetReturnPendingReasonsAsync SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 GetReturnPendingReasonsAsync başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, statusCheck.Message);
        }

        var reasons = response
            .Element("denyReasonTypeDataList")?
            .Elements("denyReasonType")
            .Select(ParseReasonType)
            .ToList() ?? [];

        logger.LogInformation("N11 GetReturnPendingReasonsAsync başarılı — {Count} neden alındı", reasons.Count);
        return new SuccessDataResult<List<N11ReasonTypeDto>>(reasons);
    }

    // -----------------------------------------------------------------------
    // GetExchangeClaimsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ClaimExchangeDto>>> GetExchangeClaimsAsync(string? status = null, int page = 0)
    {
        var request = new XElement(Ns + "ClaimExchangeListRequest",
            new XElement("searchData",
                new XElement("status", status ?? "REQUESTED")),
            new XElement("pagingData",
                new XElement("currentPage", page)));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetExchangeClaims SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ClaimExchangeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ClaimExchangeList başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ClaimExchangeDto>>(null!, statusCheck.Message);
        }

        var claims = response
            .Element("claimExchangeList")?
            .Elements("claimExchange")
            .Select(ParseExchangeClaim)
            .ToList() ?? [];

        logger.LogInformation("N11 GetExchangeClaims başarılı — {Count} değişim talebi alındı", claims.Count);
        return new SuccessDataResult<List<N11ClaimExchangeDto>>(claims);
    }

    // -----------------------------------------------------------------------
    // ApproveExchangeByTrackingAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ApproveExchangeByTrackingAsync(long claimExchangeId, string trackingNumber)
    {
        var request = new XElement(Ns + "ClaimExchangeApproveByTrackingRequest",
            new XElement("claimExchangeId", claimExchangeId),
            new XElement("trackingNumber", trackingNumber));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 ApproveExchangeByTrackingAsync SOAP çağrısı başarısız — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ApproveExchangeByTrackingAsync başarısız — ClaimExchangeId={ClaimExchangeId}, Message={Message}",
                claimExchangeId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 ApproveExchangeByTrackingAsync başarılı — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
        return new SuccessResult("Değişim talebi kargo takip numarasıyla onaylandı.");
    }

    // -----------------------------------------------------------------------
    // ApproveExchangeByCampaignAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ApproveExchangeByCampaignAsync(long claimExchangeId, int shipmentCompanyId)
    {
        var request = new XElement(Ns + "ClaimExchangeApproveByCampaignRequest",
            new XElement("claimExchangeId", claimExchangeId),
            new XElement("shipmentCompanyId", shipmentCompanyId));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 ApproveExchangeByCampaignAsync SOAP çağrısı başarısız — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ApproveExchangeByCampaignAsync başarısız — ClaimExchangeId={ClaimExchangeId}, Message={Message}",
                claimExchangeId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 ApproveExchangeByCampaignAsync başarılı — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
        return new SuccessResult("Değişim talebi kargo kampanyasıyla onaylandı.");
    }

    // -----------------------------------------------------------------------
    // DenyExchangeAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> DenyExchangeAsync(long claimExchangeId, long denyReasonId, string? denyReasonNote = null)
    {
        var request = new XElement(Ns + "ClaimExchangeDenyRequest",
            new XElement("claimExchangeId", claimExchangeId),
            new XElement("denyReasonId", denyReasonId),
            new XElement("denyReasonNote", denyReasonNote ?? string.Empty));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 DenyExchangeAsync SOAP çağrısı başarısız — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 DenyExchangeAsync başarısız — ClaimExchangeId={ClaimExchangeId}, Message={Message}",
                claimExchangeId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 DenyExchangeAsync başarılı — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
        return new SuccessResult("Değişim talebi reddedildi.");
    }

    // -----------------------------------------------------------------------
    // PendExchangeAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> PendExchangeAsync(long claimExchangeId, long pendingReasonId, int pendingDayCount, string? pendingReasonNote = null)
    {
        var request = new XElement(Ns + "ClaimExchangePendingRequest",
            new XElement("claimExchangeId", claimExchangeId),
            new XElement("pendingReasonId", pendingReasonId),
            new XElement("pendingDayCount", pendingDayCount),
            new XElement("pendingReasonNote", pendingReasonNote ?? string.Empty));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 PendExchangeAsync SOAP çağrısı başarısız — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 PendExchangeAsync başarısız — ClaimExchangeId={ClaimExchangeId}, Message={Message}",
                claimExchangeId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 PendExchangeAsync başarılı — ClaimExchangeId={ClaimExchangeId}", claimExchangeId);
        return new SuccessResult("Değişim talebi beklemeye alındı.");
    }

    // -----------------------------------------------------------------------
    // GetExchangeDenyReasonsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ReasonTypeDto>>> GetExchangeDenyReasonsAsync()
    {
        var request = new XElement(Ns + "ClaimExchangeDenyReasonTypesRequest");

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetExchangeDenyReasonsAsync SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 GetExchangeDenyReasonsAsync başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, statusCheck.Message);
        }

        var reasons = response
            .Element("denyReasonTypeDataList")?
            .Elements("denyReasonType")
            .Select(ParseReasonType)
            .ToList() ?? [];

        logger.LogInformation("N11 GetExchangeDenyReasonsAsync başarılı — {Count} neden alındı", reasons.Count);
        return new SuccessDataResult<List<N11ReasonTypeDto>>(reasons);
    }

    // -----------------------------------------------------------------------
    // GetExchangePendingReasonsAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11ReasonTypeDto>>> GetExchangePendingReasonsAsync()
    {
        var request = new XElement(Ns + "ClaimExchangePendingReasonTypesRequest");

        XElement response;
        try
        {
            response = await soapClient.SendAsync("ClaimExchangeService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetExchangePendingReasonsAsync SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 GetExchangePendingReasonsAsync başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11ReasonTypeDto>>(null!, statusCheck.Message);
        }

        var reasons = response
            .Element("pendingReasonTypeDataList")?
            .Elements("denyReasonType")
            .Select(ParseReasonType)
            .ToList() ?? [];

        logger.LogInformation("N11 GetExchangePendingReasonsAsync başarılı — {Count} neden alındı", reasons.Count);
        return new SuccessDataResult<List<N11ReasonTypeDto>>(reasons);
    }

    // -----------------------------------------------------------------------
    // XML parsing
    // -----------------------------------------------------------------------

    private static N11ClaimCancelDto ParseCancelClaim(XElement element)
    {
        long.TryParse(element.Element("id")?.Value, out var id);
        int.TryParse(element.Element("quantity")?.Value, out var quantity);
        decimal.TryParse(element.Element("unitPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice);

        DateTimeOffset? requestDate = null;
        var requestDateStr = element.Element("requestDate")?.Value;
        if (!string.IsNullOrEmpty(requestDateStr) &&
            DateTime.TryParseExact(requestDateStr, DateTimeFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            requestDate = new DateTimeOffset(parsedDate, TimeSpan.Zero);
        }

        return new N11ClaimCancelDto
        {
            ClaimCancelId = id,
            Status = element.Element("status")?.Value,
            OrderNumber = element.Element("orderNumber")?.Value,
            ProductName = element.Element("productName")?.Value,
            Quantity = quantity,
            UnitPrice = unitPrice,
            CancelReasonType = element.Element("cancelReasonType")?.Value,
            CancelReasonDescription = element.Element("cancelReasonDescription")?.Value,
            BuyerName = element.Element("buyerName")?.Value,
            BuyerEmail = element.Element("buyerEmail")?.Value,
            RequestDate = requestDate
        };
    }

    private static N11ClaimReturnDto ParseReturnClaim(XElement element)
    {
        long.TryParse(element.Element("id")?.Value, out var id);
        int.TryParse(element.Element("quantity")?.Value, out var quantity);
        decimal.TryParse(element.Element("unitPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice);
        decimal.TryParse(element.Element("finalPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var finalPrice);

        DateTimeOffset? requestDate = null;
        var requestDateStr = element.Element("requestDate")?.Value;
        if (!string.IsNullOrEmpty(requestDateStr) &&
            DateTime.TryParseExact(requestDateStr, DateTimeFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            requestDate = new DateTimeOffset(parsedDate, TimeSpan.Zero);
        }

        return new N11ClaimReturnDto
        {
            ClaimReturnId = id,
            Status = element.Element("status")?.Value,
            OrderNumber = element.Element("orderNumber")?.Value,
            ProductName = element.Element("productName")?.Value,
            Quantity = quantity,
            UnitPrice = unitPrice,
            FinalPrice = finalPrice,
            ReturnReasonType = element.Element("returnReasonType")?.Value,
            ReturnReasonDescription = element.Element("returnReasonDescription")?.Value,
            BuyerName = element.Element("buyerName")?.Value,
            BuyerEmail = element.Element("buyerEmail")?.Value,
            RequestDate = requestDate,
            ShipmentCompany = element.Element("shipmentCompany")?.Value,
            TrackingNumber = element.Element("trackingNumber")?.Value
        };
    }

    private static N11ClaimExchangeDto ParseExchangeClaim(XElement element)
    {
        long.TryParse(element.Element("id")?.Value, out var id);
        int.TryParse(element.Element("quantity")?.Value, out var quantity);
        decimal.TryParse(element.Element("unitPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice);
        decimal.TryParse(element.Element("finalPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var finalPrice);

        DateTimeOffset? requestDate = null;
        var requestDateStr = element.Element("requestDate")?.Value;
        if (!string.IsNullOrEmpty(requestDateStr) &&
            DateTime.TryParseExact(requestDateStr, DateTimeFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            requestDate = new DateTimeOffset(parsedDate, TimeSpan.Zero);
        }

        return new N11ClaimExchangeDto
        {
            ClaimExchangeId = id,
            Status = element.Element("status")?.Value,
            OrderNumber = element.Element("orderNumber")?.Value,
            ProductName = element.Element("productName")?.Value,
            Quantity = quantity,
            UnitPrice = unitPrice,
            FinalPrice = finalPrice,
            ExchangeReasonType = element.Element("exchangeReasonType")?.Value,
            ExchangeReasonDescription = element.Element("exchangeReasonDescription")?.Value,
            BuyerName = element.Element("buyerName")?.Value,
            BuyerEmail = element.Element("buyerEmail")?.Value,
            RequestDate = requestDate
        };
    }

    private static N11ReasonTypeDto ParseReasonType(XElement element)
    {
        long.TryParse(element.Element("id")?.Value, out var id);
        var value = element.Element("value")?.Value ?? string.Empty;
        return new N11ReasonTypeDto(id, value);
    }
}
