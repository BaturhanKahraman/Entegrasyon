namespace Entegrasyon.Business.Concrete.Kargo;

// ── Configuration ───────────────────────────────────────────────────────────

public sealed record ArasKargoConfig(
    string UserName,
    string Password,
    string CustomerCode,
    string BaseUrl,
    bool IsTestEnvironment);

// ── Request Models ──────────────────────────────────────────────────────────

public sealed record ArasKargoShipmentRequest(
    string IntegrationCode,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverCityName,
    string ReceiverTownName,
    string ReceiverAddress,
    int PieceCount,
    string? Description = null,
    bool IsCod = false,
    decimal CodAmount = 0,
    string? InvoiceNumber = null,
    List<ArasKargoPieceDetail>? PieceDetails = null);

public sealed record ArasKargoPieceDetail(
    string? BarcodeNumber = null,
    decimal? Weight = null,
    decimal? VolumetricWeight = null,
    string? Description = null);

// ── Response Models ─────────────────────────────────────────────────────────

public sealed record ArasKargoOrderResult(
    string ResultCode,
    string ResultMessage,
    string? BarcodeNumber);

public sealed record ArasKargoTrackingResult(
    string IntegrationCode,
    string Status,
    string? ReceiverName,
    string? DeliveryDate,
    string? BarcodeNumber);

public sealed record ArasKargoMovement(
    string Date,
    string Status,
    string Location,
    string? Description);

public sealed record ArasKargoShipmentSummary(
    string IntegrationCode,
    string Status,
    string ReceiverName,
    string? Date);

// ── SOAP Internal Models ────────────────────────────────────────────────────

/// <summary>
/// GetQueryJSON icin QueryType enumeration'i.
/// </summary>
public static class ArasKargoQueryType
{
    public const int CargoInformation = 1;
    public const int CargoMovementDate = 2;
    public const int CargoDeliveryDate = 3;
    public const int CargoUndelivered = 4;
    public const int CargoRedirectDate = 5;
    public const int CargoSenderReturnDate = 6;
    public const int CargoMovementInformation = 7;
    public const int AllBranches = 8;
    public const int CargoWaybillBetweenDate = 9;
    public const int CargoRealInformation = 10;
    public const int CargoInvoice = 11;
    public const int CargoCountToday = 12;
    public const int CampaignCode = 13;
}
