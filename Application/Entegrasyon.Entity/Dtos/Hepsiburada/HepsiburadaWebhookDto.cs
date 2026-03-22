using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada webhook — yeni sipariş event'i.
/// POST {baseUrl}/orders
/// </summary>
public sealed record HepsiburadaWebhookOrderEvent(
    [property: JsonPropertyName("orderNumber")] string? OrderNumber,
    [property: JsonPropertyName("merchantId")] string? MerchantId,
    [property: JsonPropertyName("orderDate")] string? OrderDate,
    [property: JsonPropertyName("lineItems")] List<HepsiburadaWebhookLineItem>? LineItems);

public sealed record HepsiburadaWebhookLineItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("hepsiburadaSku")] string? HepsiburadaSku,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice);

/// <summary>
/// Hepsiburada webhook — paket event'i (intransit, deliver, undeliver, unpack).
/// </summary>
public sealed record HepsiburadaWebhookPackageEvent(
    [property: JsonPropertyName("packageNumber")] string? PackageNumber,
    [property: JsonPropertyName("status")] string? Status);

/// <summary>
/// Hepsiburada webhook — sipariş iptal event'i.
/// PUT {baseUrl}/lineitems/{lineitemid}/cancel
/// </summary>
public sealed record HepsiburadaWebhookCancelEvent(
    [property: JsonPropertyName("lineItemId")] string? LineItemId,
    [property: JsonPropertyName("reason")] string? Reason);

/// <summary>
/// Hepsiburada Q&A soru bilgisi.
/// GET /api/v1.0/issues
/// </summary>
public sealed record HepsiburadaQuestionDto(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("question")] string? Question,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("hepsiburadaSku")] string? HepsiburadaSku,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("createdDate")] string? CreatedDate);

/// <summary>
/// Hepsiburada Claim (iade/değişim) bilgisi.
/// GET /claims/merchantId/{merchantId}
/// </summary>
public sealed record HepsiburadaClaimDto(
    [property: JsonPropertyName("claimNumber")] string ClaimNumber,
    [property: JsonPropertyName("orderNumber")] string? OrderNumber,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("claimType")] string? ClaimType,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("lineItems")] List<HepsiburadaClaimLineItem>? LineItems);

public sealed record HepsiburadaClaimLineItem(
    [property: JsonPropertyName("hepsiburadaSku")] string? HepsiburadaSku,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice);

/// <summary>
/// Q&A cevap isteği.
/// POST /api/v1.0/issues/{number}/answer
/// </summary>
public sealed record HepsiburadaAnswerRequest(
    [property: JsonPropertyName("answer")] string Answer);
