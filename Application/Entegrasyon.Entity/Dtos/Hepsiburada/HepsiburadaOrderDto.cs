using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada sipariş sorgulama yanıtı.
/// GET /orders/merchantid/{merchantId}
/// </summary>
public sealed record HepsiburadaOrderListResponse(
    [property: JsonPropertyName("content")] List<HepsiburadaOrderDto>? Content,
    [property: JsonPropertyName("totalElements")] int TotalElements,
    [property: JsonPropertyName("totalPages")] int TotalPages);

public sealed record HepsiburadaOrderDto(
    [property: JsonPropertyName("orderNumber")] string OrderNumber,
    [property: JsonPropertyName("orderDate")] string? OrderDate,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("totalPrice")] decimal TotalPrice,
    [property: JsonPropertyName("customer")] HepsiburadaOrderCustomer? Customer,
    [property: JsonPropertyName("lineItems")] List<HepsiburadaOrderLineItem>? LineItems,
    [property: JsonPropertyName("shippingAddress")] HepsiburadaOrderAddress? ShippingAddress);

public sealed record HepsiburadaOrderCustomer(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("surname")] string? Surname,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone);

public sealed record HepsiburadaOrderLineItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("hepsiburadaSku")] string? HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string? MerchantSku,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
    [property: JsonPropertyName("status")] string? Status);

public sealed record HepsiburadaOrderAddress(
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("district")] string? District,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("phone")] string? Phone);

/// <summary>
/// Paketleme isteği.
/// POST /packages/merchantid/{merchantId}
/// </summary>
public sealed record HepsiburadaPackageRequest(
    [property: JsonPropertyName("lineItemRequests")] List<HepsiburadaPackageLineItem> LineItemRequests,
    [property: JsonPropertyName("parcelQuantity")] int ParcelQuantity,
    [property: JsonPropertyName("deci")] decimal Deci);

public sealed record HepsiburadaPackageLineItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("serialNumbers")] List<string>? SerialNumbers);

public sealed record HepsiburadaPackageResponse(
    [property: JsonPropertyName("packageNumber")] string? PackageNumber,
    [property: JsonPropertyName("barcode")] string? Barcode);

/// <summary>
/// Sipariş iptal isteği.
/// POST /lineitems/merchantid/{merchantId}/id/{lineId}/cancelbymerchant
/// </summary>
public sealed record HepsiburadaCancelRequest(
    [property: JsonPropertyName("reasonId")] int ReasonId);

/// <summary>
/// Fatura ekleme isteği.
/// POST /lineitems/merchantid/{merchantId}/id/{lineId}/invoice
/// </summary>
public sealed record HepsiburadaInvoiceRequest(
    [property: JsonPropertyName("invoiceNumber")] string InvoiceNumber,
    [property: JsonPropertyName("invoiceDate")] string InvoiceDate,
    [property: JsonPropertyName("invoiceUrl")] string? InvoiceUrl);
