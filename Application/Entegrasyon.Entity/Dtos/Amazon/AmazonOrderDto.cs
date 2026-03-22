using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Amazon;

public sealed record AmazonOrderListResponse(
    [property: JsonPropertyName("payload")] AmazonOrderListPayload? Payload);

public sealed record AmazonOrderListPayload(
    [property: JsonPropertyName("Orders")] List<AmazonOrderDto>? Orders,
    [property: JsonPropertyName("NextToken")] string? NextToken);

public sealed record AmazonOrderDto(
    [property: JsonPropertyName("AmazonOrderId")] string AmazonOrderId,
    [property: JsonPropertyName("OrderStatus")] string OrderStatus,
    [property: JsonPropertyName("OrderTotal")] AmazonMoney? OrderTotal,
    [property: JsonPropertyName("MarketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("PurchaseDate")] string? PurchaseDate,
    [property: JsonPropertyName("FulfillmentChannel")] string? FulfillmentChannel,
    [property: JsonPropertyName("ShippingAddress")] AmazonAddress? ShippingAddress,
    [property: JsonPropertyName("NumberOfItemsShipped")] int NumberOfItemsShipped,
    [property: JsonPropertyName("NumberOfItemsUnshipped")] int NumberOfItemsUnshipped);

public sealed record AmazonMoney(
    [property: JsonPropertyName("CurrencyCode")] string? CurrencyCode,
    [property: JsonPropertyName("Amount")] string? Amount);

public sealed record AmazonAddress(
    [property: JsonPropertyName("Name")] string? Name,
    [property: JsonPropertyName("AddressLine1")] string? AddressLine1,
    [property: JsonPropertyName("City")] string? City,
    [property: JsonPropertyName("StateOrRegion")] string? StateOrRegion,
    [property: JsonPropertyName("PostalCode")] string? PostalCode,
    [property: JsonPropertyName("CountryCode")] string? CountryCode);

public sealed record AmazonOrderItemListResponse(
    [property: JsonPropertyName("payload")] AmazonOrderItemPayload? Payload);

public sealed record AmazonOrderItemPayload(
    [property: JsonPropertyName("OrderItems")] List<AmazonOrderItemDto>? OrderItems);

public sealed record AmazonOrderItemDto(
    [property: JsonPropertyName("ASIN")] string Asin,
    [property: JsonPropertyName("SellerSKU")] string? SellerSku,
    [property: JsonPropertyName("OrderItemId")] string OrderItemId,
    [property: JsonPropertyName("Title")] string? Title,
    [property: JsonPropertyName("QuantityOrdered")] int QuantityOrdered,
    [property: JsonPropertyName("QuantityShipped")] int QuantityShipped,
    [property: JsonPropertyName("ItemPrice")] AmazonMoney? ItemPrice);

public sealed record AmazonConfirmShipmentRequest(
    [property: JsonPropertyName("marketplaceId")] string MarketplaceId,
    [property: JsonPropertyName("packageDetail")] AmazonPackageDetail PackageDetail);

public sealed record AmazonPackageDetail(
    [property: JsonPropertyName("packageReferenceId")] string PackageReferenceId,
    [property: JsonPropertyName("carrierCode")] string CarrierCode,
    [property: JsonPropertyName("trackingNumber")] string TrackingNumber,
    [property: JsonPropertyName("shipDate")] string ShipDate,
    [property: JsonPropertyName("orderItems")] List<AmazonShipmentItem> OrderItems);

public sealed record AmazonShipmentItem(
    [property: JsonPropertyName("orderItemId")] string OrderItemId,
    [property: JsonPropertyName("quantity")] int Quantity);
