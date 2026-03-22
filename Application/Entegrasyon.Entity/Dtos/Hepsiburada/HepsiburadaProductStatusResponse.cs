using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// POST /api/products/import yanıtı — trackingId döner.
/// </summary>
public sealed record HepsiburadaTrackingResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] HepsiburadaTrackingData? Data);

public sealed record HepsiburadaTrackingData(
    [property: JsonPropertyName("trackingId")] string TrackingId);

/// <summary>
/// GET /api/products/status/{trackingId} yanıtı.
/// </summary>
public sealed record HepsiburadaProductStatusResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] HepsiburadaProductStatusData? Data);

public sealed record HepsiburadaProductStatusData(
    [property: JsonPropertyName("totalElements")] int TotalElements,
    [property: JsonPropertyName("content")] List<HepsiburadaProductStatusItem>? Content);

public sealed record HepsiburadaProductStatusItem(
    [property: JsonPropertyName("merchantSku")] string? MerchantSku,
    [property: JsonPropertyName("hbSku")] string? HbSku,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("productStatus")] string? ProductStatus,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("variantGroupId")] string? VariantGroupId,
    [property: JsonPropertyName("importStatus")] string? ImportStatus,
    [property: JsonPropertyName("importMessages")] List<HepsiburadaImportMessage>? ImportMessages,
    [property: JsonPropertyName("validationResults")] List<HepsiburadaValidationResult>? ValidationResults,
    [property: JsonPropertyName("matchedHbProductInfo")] List<HepsiburadaMatchedProductInfo>? MatchedHbProductInfo);

public sealed record HepsiburadaImportMessage(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message);

public sealed record HepsiburadaValidationResult(
    [property: JsonPropertyName("fieldName")] string? FieldName,
    [property: JsonPropertyName("message")] string? Message);

public sealed record HepsiburadaMatchedProductInfo(
    [property: JsonPropertyName("hbSku")] string? HbSku,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl);

/// <summary>
/// POST /api/products/approve-prematch istek gövdesi.
/// </summary>
public sealed record HepsiburadaPreMatchApprovalRequest(
    [property: JsonPropertyName("merchant")] string Merchant,
    [property: JsonPropertyName("merchantSku")] string MerchantSku);
