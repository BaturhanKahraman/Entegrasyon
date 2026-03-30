using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 REST API ile sipariş sorgulama ve onaylama işlemlerini gerçekleştiren servis.
/// Kargolama (ShipOrderItemAsync) ve ret (RejectOrderItemAsync) → SOAP fallback.
/// </summary>
public sealed class N11RestOrderService(
    IN11RestClient restClient,
    IN11SoapClient soapClient,
    ILogger<N11RestOrderService> logger) : IN11OrderService
{
    private static readonly System.Xml.Linq.XNamespace Ns = "http://www.n11.com/ws/schemas";

    // -----------------------------------------------------------------------
    // FetchOrdersAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<List<N11OrderDto>>> FetchOrdersAsync(
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null,
        string? status = null, int page = 0, int pageSize = 50)
    {
        var now = DateTimeOffset.UtcNow;
        var start = startDate ?? now.AddDays(-7);
        var end = endDate ?? now;

        // N11 REST timestamp: milliseconds, GMT+3 (+3 saat farkı)
        var startMs = ToN11Timestamp(start);
        var endMs = ToN11Timestamp(end);

        var path = $"rest/delivery/v1/shipmentPackages?startDate={startMs}&endDate={endMs}&page={page}&size={Math.Min(pageSize, 100)}&orderByField=true&orderByDirection=DESC";

        if (!string.IsNullOrEmpty(status))
            path += $"&status={status}";

        try
        {
            var response = await restClient.GetAsync<N11ShipmentPackagesResponse>(path);

            if (response is null)
            {
                logger.LogError("N11 REST FetchOrders yanıt boş döndü");
                return new ErrorDataResult<List<N11OrderDto>>(null!, "N11 REST API yanıt vermedi.");
            }

            var orders = (response.Content ?? []).Select(MapToOrderDto).ToList();
            logger.LogInformation("N11 REST FetchOrders başarılı — {Count} sipariş alındı", orders.Count);
            return new SuccessDataResult<List<N11OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST FetchOrders başarısız");
            return new ErrorDataResult<List<N11OrderDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // GetOrderDetailAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId)
    {
        var path = $"rest/delivery/v1/shipmentPackages?orderNumber={orderId}";

        try
        {
            var response = await restClient.GetAsync<N11ShipmentPackagesResponse>(path);

            if (response is null || response.Content is null || response.Content.Count == 0)
            {
                logger.LogWarning("N11 REST GetOrderDetail: sipariş bulunamadı — OrderId={OrderId}", orderId);
                return new ErrorDataResult<N11OrderDto>(null!, "N11 REST yanıtında sipariş bulunamadı.");
            }

            var order = MapToOrderDto(response.Content[0]);
            logger.LogInformation("N11 REST GetOrderDetail başarılı — OrderId={OrderId}", orderId);
            return new SuccessDataResult<N11OrderDto>(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST GetOrderDetail başarısız — OrderId={OrderId}", orderId);
            return new ErrorDataResult<N11OrderDto>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // AcceptOrderItemAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> AcceptOrderItemAsync(long orderItemId, int numberOfPackages = 1)
    {
        var request = new N11OrderUpdateRequest(
            Lines: [new N11OrderLineUpdate(orderItemId)],
            Status: "Picking");

        try
        {
            var response = await restClient.PutAsync<N11OrderUpdateRequest, object>(
                "rest/order/v1/update", request);

            // REST endpoint 200 OK dönerse başarı kabul edilir
            logger.LogInformation("N11 REST AcceptOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
            return new SuccessResult("Sipariş kalemi kabul edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 REST AcceptOrderItem başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // RejectOrderItemAsync — SOAP fallback
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> RejectOrderItemAsync(long orderItemId, string rejectReason, string rejectReasonType)
    {
        // N11 REST API'de ret endpoint'i yok — SOAP kullanılır
        var request = new System.Xml.Linq.XElement(Ns + "OrderItemRejectRequest",
            new System.Xml.Linq.XElement("orderItemList",
                new System.Xml.Linq.XElement("orderItem",
                    new System.Xml.Linq.XElement("id", orderItemId))),
            new System.Xml.Linq.XElement("rejectReason", rejectReason),
            new System.Xml.Linq.XElement("rejectReasonType", rejectReasonType));

        try
        {
            var response = await soapClient.SendAsync("OrderService", "", request);
            var status = response.Element("result")?.Element("status")?.Value;

            if (status == "failure")
            {
                var msg = response.Element("result")?.Element("errorMessage")?.Value ?? "N11 SOAP ret hatası";
                logger.LogError("N11 SOAP RejectOrderItem başarısız — OrderItemId={OrderItemId}, Message={Message}",
                    orderItemId, msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("N11 SOAP RejectOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
            return new SuccessResult("Sipariş kalemi reddedildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 SOAP RejectOrderItem başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 SOAP bağlantı hatası: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // ShipOrderItemAsync — SOAP fallback
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ShipOrderItemAsync(long orderItemId, int shipmentCompanyId, string trackingNumber, int shipmentMethod = 1)
    {
        // N11 REST API'de kargolama endpoint'i yok — SOAP kullanılır
        var request = new System.Xml.Linq.XElement(Ns + "MakeOrderItemShipmentRequest",
            new System.Xml.Linq.XElement("orderItemList",
                new System.Xml.Linq.XElement("orderItem",
                    new System.Xml.Linq.XElement("id", orderItemId),
                    new System.Xml.Linq.XElement("shipmentInfo",
                        new System.Xml.Linq.XElement("shipmentCompany",
                            new System.Xml.Linq.XElement("id", shipmentCompanyId)),
                        new System.Xml.Linq.XElement("trackingNumber", trackingNumber),
                        new System.Xml.Linq.XElement("shipmentMethod", shipmentMethod)))));

        try
        {
            var response = await soapClient.SendAsync("OrderService", "", request);
            var status = response.Element("result")?.Element("status")?.Value;

            if (status == "failure")
            {
                var msg = response.Element("result")?.Element("errorMessage")?.Value ?? "N11 SOAP kargo hatası";
                logger.LogError("N11 SOAP ShipOrderItem başarısız — OrderItemId={OrderItemId}, Message={Message}",
                    orderItemId, msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("N11 SOAP ShipOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
            return new SuccessResult("Sipariş kalemi kargoya verildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 SOAP ShipOrderItem başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 SOAP bağlantı hatası: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// DateTimeOffset'i N11 REST API'nin beklediği millisecond timestamp'e dönüştürür (GMT+3).
    /// </summary>
    private static long ToN11Timestamp(DateTimeOffset date)
    {
        // N11 GMT+3 ister
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(date.UtcDateTime, tz);
        return new DateTimeOffset(localTime, TimeSpan.FromHours(3)).ToUnixTimeMilliseconds();
    }

    private static N11OrderDto MapToOrderDto(N11ShipmentPackage package)
    {
        DateTimeOffset createDate = default;
        if (!string.IsNullOrEmpty(package.ShipmentStatusDate) &&
            long.TryParse(package.ShipmentStatusDate, out var ms))
        {
            createDate = DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }

        return new N11OrderDto
        {
            Id = package.Id,
            OrderNumber = package.OrderNumber ?? string.Empty,
            Status = MapStatus(package.Status),
            TotalAmount = package.TotalPrice,
            CreateDate = createDate,
            Buyer = package.Buyer is not null
                ? new N11BuyerDto(package.Buyer.FirstName, package.Buyer.LastName, package.Buyer.Email)
                : null,
            BillingAddress = package.BillingAddress is not null
                ? new N11AddressDto(package.BillingAddress.City, package.BillingAddress.District,
                    package.BillingAddress.FullAddress, package.BillingAddress.PostalCode)
                : null,
            ShippingAddress = package.ShippingAddress is not null
                ? new N11AddressDto(package.ShippingAddress.City, package.ShippingAddress.District,
                    package.ShippingAddress.FullAddress, package.ShippingAddress.PostalCode)
                : null,
            OrderItems = (package.Lines ?? []).Select(line => new N11OrderItemDto
            {
                Id = line.LineId,
                ProductId = line.ProductId ?? 0,
                ProductSellerCode = line.StockCode,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                Price = line.UnitPrice,
                Status = line.Status,
                Shipment = (!string.IsNullOrEmpty(line.ShipmentCompanyName) || !string.IsNullOrEmpty(line.TrackingNumber))
                    ? new N11ShipmentDto(line.ShipmentCompanyName, line.TrackingNumber, null)
                    : null
            }).ToList()
        };
    }

    /// <summary>
    /// N11 REST status string'ini SOAP ile uyumlu statü değerine çevirir.
    /// </summary>
    private static string MapStatus(string? restStatus) => restStatus switch
    {
        "Created" => "2",
        "Picking" => "5",
        "Shipped" => "6",
        "Delivered" => "7",
        "Cancelled" => "4",
        "UnSupplied" => "8",
        _ => restStatus ?? string.Empty
    };
}
