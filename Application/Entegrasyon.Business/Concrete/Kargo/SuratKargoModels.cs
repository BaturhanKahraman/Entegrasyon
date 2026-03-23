namespace Entegrasyon.Business.Concrete.Kargo;

// ── Request Models ──────────────────────────────────────────────────────────

/// <summary>
/// Surat Kargo gonderi olusturma istegi.
/// </summary>
public sealed record SuratKargoShipmentRequest(
    string ReceiverName,
    string ReceiverAddress,
    string ReceiverPhone,
    string ReceiverCityName,
    string ReceiverTownName,
    int PieceCount,
    string? ReferenceNo = null,
    decimal? Weight = null,
    string? ReceiverPhone2 = null,
    string? Description = null,
    bool IsCOD = false,
    decimal? CodAmount = null);

// ── Response Models ─────────────────────────────────────────────────────────

/// <summary>
/// Gonderi olusturma sonucu.
/// </summary>
public sealed record SuratKargoShipmentResult(
    string TrackingNumber,
    string? BarcodeNo,
    int ResultCode,
    string? ResultMessage);

/// <summary>
/// Gonderi sorgulama sonucu.
/// </summary>
public sealed record SuratKargoTrackingResult(
    string TrackingNumber,
    string Status,
    DateTime? DeliveryDate,
    List<SuratKargoMovement> Movements);

/// <summary>
/// Gonderi hareket kaydi.
/// </summary>
public sealed record SuratKargoMovement(
    DateTime Date,
    string Location,
    string Description);

// ── Internal Models ─────────────────────────────────────────────────────────

/// <summary>
/// Surat Kargo API credential bilgileri.
/// </summary>
public sealed record SuratKargoCredentials(
    string UserName,
    string Password,
    string CustomerCode,
    string BaseUrl);
