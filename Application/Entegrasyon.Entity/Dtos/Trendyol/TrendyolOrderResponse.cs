namespace Entegrasyon.Entity.Dtos.Trendyol;

/// <summary>
/// GET /integration/order/sellers/{sellerId}/orders — getShipmentPackages response.
/// </summary>
public sealed record TrendyolOrderListResponse(
    int Page,
    int Size,
    int TotalPages,
    int TotalElements,
    List<TrendyolShipmentPackage>? Content);

public sealed record TrendyolShipmentPackage(
    long ShipmentPackageId,
    string? OrderNumber,
    string? OrderDate,
    string? Status,
    decimal GrossAmount,
    decimal TotalDiscount,
    decimal TotalPrice,
    bool Micro,
    bool FastDelivery,
    string? EstimatedDeliveryEndDate,
    TrendyolCargoInfo? CargoProviderInfo,
    TrendyolCustomerInfo? CustomerInfo,
    TrendyolAddressInfo? ShipmentAddress,
    TrendyolAddressInfo? InvoiceAddress,
    List<TrendyolOrderLine>? Lines);

public sealed record TrendyolOrderLine(
    long LineId,
    int Quantity,
    decimal Price,
    decimal Discount,
    string? Barcode,
    string? MerchantSku,
    string? ProductName,
    string? ProductColor,
    string? ProductSize,
    int? MerchantId);

public sealed record TrendyolCargoInfo(
    string? CargoProviderName,
    string? CargoTrackingNumber,
    string? CargoTrackingLink);

public sealed record TrendyolCustomerInfo(
    string? FirstName,
    string? LastName,
    string? Email);

public sealed record TrendyolAddressInfo(
    string? City,
    string? District,
    string? FullAddress,
    string? PostalCode,
    string? CountryCode);

/// <summary>
/// Sipariş sorgulama parametreleri.
/// </summary>
public sealed record TrendyolOrderQueryParams(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    string? Status = null,
    int Page = 0,
    int Size = 50,
    string? OrderByField = "PackageLastModifiedDate",
    string? OrderByDirection = "DESC");
