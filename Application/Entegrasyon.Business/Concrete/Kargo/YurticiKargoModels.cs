namespace Entegrasyon.Business.Concrete.Kargo;

// ── Request Modelleri ────────────────────────────────────────────────────────

/// <summary>
/// Yurtici Kargo createShipment istegi icin DTO.
/// </summary>
public sealed record YurticiCreateShipmentRequest(
    string CargoKey,
    string InvoiceKey,
    string ReceiverCustName,
    string ReceiverAddress,
    string CityName,
    string TownName,
    string ReceiverPhone1,
    int CargoCount = 1,
    string? ReceiverPhone2 = null,
    string? ReceiverPhone3 = null,
    string? EmailAddress = null,
    decimal? Desi = null,
    decimal? Kg = null,
    string? Description = null,
    decimal? TtInvoiceAmount = null,
    int? TtCollectionType = null,
    string? TtDocumentId = null,
    int? TtDocumentSaveType = null,
    int? DcSelectedCredit = null,
    int? DcCreditRule = null);

/// <summary>
/// Yurtici Kargo queryShipment istegi icin DTO.
/// </summary>
public sealed record YurticiQueryShipmentRequest(
    string[] Keys,
    int KeyType = 0,
    bool AddHistoricalData = false,
    bool OnlyTracking = false);

// ── Response Modelleri ───────────────────────────────────────────────────────

/// <summary>
/// createShipment response'u.
/// OutFlag: "0" = başarısız, "1" = basarili.
/// </summary>
public sealed record YurticiCreateShipmentResponse(
    string OutFlag,
    string OutResult,
    string? JobId);

/// <summary>
/// queryShipment response'undaki gonderi bilgisi.
/// </summary>
public sealed record YurticiShipmentInfo(
    string CargoKey,
    string? InvoiceKey,
    int OperationCode,
    string? OperationMessage,
    DateTime? DeliveryDate,
    string? DeliveredTo,
    int? UnitCount);

/// <summary>
/// cancelShipment response'u.
/// </summary>
public sealed record YurticiCancelShipmentResponse(
    string OutFlag,
    string OutResult,
    string? CargoKey);
