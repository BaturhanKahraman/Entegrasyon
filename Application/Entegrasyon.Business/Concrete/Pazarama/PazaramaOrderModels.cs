using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

// --- Money helper ---
public sealed record PazaramaMoneyDto(
    [property: JsonPropertyName("value")] decimal Value,
    [property: JsonPropertyName("valueInt")] int ValueInt,
    [property: JsonPropertyName("valueString")] string? ValueString,
    [property: JsonPropertyName("currency")] string? Currency);

// --- Order fetch request ---
public sealed record PazaramaOrderFetchRequest(
    [property: JsonPropertyName("orderNumber")] long? OrderNumber,
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("pageNumber")] int PageNumber);

// --- Order response ---
public sealed record PazaramaOrderDto(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("orderNumber")] long OrderNumber,
    [property: JsonPropertyName("orderDate")] string? OrderDate,
    [property: JsonPropertyName("orderAmount")] decimal OrderAmount,
    [property: JsonPropertyName("shipmentAmount")] decimal ShipmentAmount,
    [property: JsonPropertyName("discountAmount")] decimal DiscountAmount,
    [property: JsonPropertyName("discountDescription")] string? DiscountDescription,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("paymentType")] int PaymentType,
    [property: JsonPropertyName("orderStatus")] int OrderStatus,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("customerName")] string? CustomerName,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("shipmentAddress")] PazaramaOrderAddressDto? ShipmentAddress,
    [property: JsonPropertyName("billingAddress")] PazaramaOrderBillingAddressDto? BillingAddress,
    [property: JsonPropertyName("items")] List<PazaramaOrderItemDto>? Items);

public sealed record PazaramaOrderAddressDto(
    [property: JsonPropertyName("addressId")] string? AddressId,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("nameSurname")] string? NameSurname,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("cityName")] string? CityName,
    [property: JsonPropertyName("districtName")] string? DistrictName,
    [property: JsonPropertyName("neighborhoodName")] string? NeighborhoodName,
    [property: JsonPropertyName("addressDetail")] string? AddressDetail,
    [property: JsonPropertyName("displayAddressText")] string? DisplayAddressText,
    [property: JsonPropertyName("phoneNumber")] string? PhoneNumber);

public sealed record PazaramaOrderBillingAddressDto(
    [property: JsonPropertyName("addressId")] string? AddressId,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("nameSurname")] string? NameSurname,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("cityName")] string? CityName,
    [property: JsonPropertyName("districtName")] string? DistrictName,
    [property: JsonPropertyName("neighborhoodName")] string? NeighborhoodName,
    [property: JsonPropertyName("addressDetail")] string? AddressDetail,
    [property: JsonPropertyName("displayAddressText")] string? DisplayAddressText,
    [property: JsonPropertyName("phoneNumber")] string? PhoneNumber,
    [property: JsonPropertyName("identityNumber")] string? IdentityNumber,
    [property: JsonPropertyName("invoiceType")] int? InvoiceType,
    [property: JsonPropertyName("companyName")] string? CompanyName,
    [property: JsonPropertyName("taxNumber")] string? TaxNumber,
    [property: JsonPropertyName("taxOffice")] string? TaxOffice,
    [property: JsonPropertyName("isEInvoiceObliged")] bool? IsEInvoiceObliged);

public sealed record PazaramaOrderItemDto(
    [property: JsonPropertyName("orderItemId")] string OrderItemId,
    [property: JsonPropertyName("orderItemStatus")] int OrderItemStatus,
    [property: JsonPropertyName("shipmentCode")] string? ShipmentCode,
    [property: JsonPropertyName("shipmentCost")] PazaramaMoneyDto? ShipmentCost,
    [property: JsonPropertyName("deliveryType")] int DeliveryType,
    [property: JsonPropertyName("deliveryDetail")] PazaramaDeliveryDetailDto? DeliveryDetail,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("listPrice")] PazaramaMoneyDto? ListPrice,
    [property: JsonPropertyName("salePrice")] PazaramaMoneyDto? SalePrice,
    [property: JsonPropertyName("taxAmount")] PazaramaMoneyDto? TaxAmount,
    [property: JsonPropertyName("shipmentAmount")] PazaramaMoneyDto? ShipmentAmount,
    [property: JsonPropertyName("totalPrice")] PazaramaMoneyDto? TotalPrice,
    [property: JsonPropertyName("discountAmount")] PazaramaMoneyDto? DiscountAmount,
    [property: JsonPropertyName("discountDescription")] string? DiscountDescription,
    [property: JsonPropertyName("taxIncluded")] bool TaxIncluded,
    [property: JsonPropertyName("cargo")] PazaramaCargoDto? Cargo,
    [property: JsonPropertyName("product")] PazaramaOrderProductDto? Product);

public sealed record PazaramaDeliveryDetailDto(
    [property: JsonPropertyName("phoneNumber")] string? PhoneNumber,
    [property: JsonPropertyName("email")] string? Email);

public sealed record PazaramaCargoDto(
    [property: JsonPropertyName("companyName")] string? CompanyName,
    [property: JsonPropertyName("trackingNumber")] string? TrackingNumber,
    [property: JsonPropertyName("trackingUrl")] string? TrackingUrl);

public sealed record PazaramaOrderProductDto(
    [property: JsonPropertyName("productId")] string? ProductId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("imageURL")] string? ImageUrl,
    [property: JsonPropertyName("variantOptionDisplay")] string? VariantOptionDisplay,
    [property: JsonPropertyName("stockCode")] string? StockCode,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("vatRate")] int VatRate);

// --- Order status update ---
public sealed record PazaramaOrderItemUpdate(
    [property: JsonPropertyName("orderItemId")] string OrderItemId,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("deliveryType")] int? DeliveryType = null,
    [property: JsonPropertyName("shippingTrackingNumber")] string? ShippingTrackingNumber = null,
    [property: JsonPropertyName("trackingUrl")] string? TrackingUrl = null,
    [property: JsonPropertyName("cargoCompanyId")] string? CargoCompanyId = null);

public sealed record PazaramaOrderStatusUpdateRequest(
    [property: JsonPropertyName("orderNumber")] long OrderNumber,
    [property: JsonPropertyName("item")] PazaramaOrderItemUpdate Item);

public sealed record PazaramaBulkOrderStatusRequest(
    [property: JsonPropertyName("orderNumber")] long OrderNumber,
    [property: JsonPropertyName("status")] int Status);

// --- Refund ---
public sealed record PazaramaRefundFetchRequest(
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("pageNumber")] int PageNumber,
    [property: JsonPropertyName("refundStatus")] int? RefundStatus,
    [property: JsonPropertyName("requestStartDate")] string RequestStartDate,
    [property: JsonPropertyName("requestEndDate")] string RequestEndDate);

public sealed record PazaramaRefundListResponse(
    [property: JsonPropertyName("responsePage")] PazaramaRefundPageInfo? ResponsePage,
    [property: JsonPropertyName("pageReport")] PazaramaRefundPageReport? PageReport,
    [property: JsonPropertyName("refundList")] List<PazaramaRefundDto>? RefundList);

public sealed record PazaramaRefundPageInfo(
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("pageIndex")] int PageIndex,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("totalPages")] int TotalPages);

public sealed record PazaramaRefundPageReport(
    [property: JsonPropertyName("totalRefundCount")] int TotalRefundCount,
    [property: JsonPropertyName("totalWaitingRefundCount")] int TotalWaitingRefundCount,
    [property: JsonPropertyName("totalApprovedRefundCount")] int TotalApprovedRefundCount,
    [property: JsonPropertyName("totalRejectedRefundCount")] int TotalRejectedRefundCount);

public sealed record PazaramaRefundDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("refundId")] string RefundId,
    [property: JsonPropertyName("orderNumber")] long OrderNumber,
    [property: JsonPropertyName("orderDate")] string? OrderDate,
    [property: JsonPropertyName("refundNumber")] long RefundNumber,
    [property: JsonPropertyName("refundType")] string? RefundType,
    [property: JsonPropertyName("refundStatus")] int RefundStatus,
    [property: JsonPropertyName("refundStatusName")] string? RefundStatusName,
    [property: JsonPropertyName("paymentType")] string? PaymentType,
    [property: JsonPropertyName("refundDate")] string? RefundDate,
    [property: JsonPropertyName("totalAmount")] PazaramaMoneyDto? TotalAmount,
    [property: JsonPropertyName("refundAmount")] PazaramaMoneyDto? RefundAmount,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("customerName")] string? CustomerName,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("customerPhoneNumber")] string? CustomerPhoneNumber,
    [property: JsonPropertyName("customerAddress")] string? CustomerAddress,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("productCode")] string? ProductCode,
    [property: JsonPropertyName("productStockCode")] string? ProductStockCode,
    [property: JsonPropertyName("shipmentCompanyName")] string? ShipmentCompanyName,
    [property: JsonPropertyName("shipmentCode")] long? ShipmentCode,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("boDescription")] string? BoDescription,
    [property: JsonPropertyName("quantity")] int? Quantity);

public sealed record PazaramaRefundUpdateRequest(
    [property: JsonPropertyName("refundId")] string RefundId,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("RefundRejectType")] int? RefundRejectType = null);

public sealed record PazaramaCancelUpdateRequest(
    [property: JsonPropertyName("refundId")] string RefundId,
    [property: JsonPropertyName("status")] int Status);
