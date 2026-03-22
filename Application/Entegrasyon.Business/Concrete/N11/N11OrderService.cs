using System.Globalization;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API ile gerçek sipariş sorgulama işlemleri.
/// DetailedOrderList ve OrderDetail SOAP çağrılarını gerçekleştirir.
/// </summary>
public sealed class N11OrderService(
    IN11SoapClient soapClient,
    ILogger<N11OrderService> logger) : IN11OrderService
{
    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";
    private const string DateFormat = "dd/MM/yyyy";

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// N11 SOAP yanıtındaki result/status alanını kontrol eder.
    /// Failure ise ErrorResult, başarı ise SuccessResult döner.
    /// </summary>
    private static IResult CheckN11ResponseStatus(XElement response)
    {
        var status = response.Element("result")?.Element("status")?.Value;
        if (status == "failure")
        {
            var errorMessage = response.Element("result")?.Element("errorMessage")?.Value
                ?? "Bilinmeyen N11 hatası";
            return new ErrorResult(errorMessage);
        }
        return new SuccessResult();
    }

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

        var searchData = new XElement("searchData",
            new XElement("startDate", start.ToString(DateFormat)),
            new XElement("endDate", end.ToString(DateFormat)),
            new XElement("sortForUpdateDate", "true"));

        if (!string.IsNullOrEmpty(status))
            searchData.Add(new XElement("status", status));

        var request = new XElement(Ns + "DetailedOrderListRequest",
            searchData,
            new XElement("pagingData",
                new XElement("currentPage", page),
                new XElement("pageSize", pageSize)));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("OrderService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 FetchOrders SOAP çağrısı başarısız");
            return new ErrorDataResult<List<N11OrderDto>>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 DetailedOrderList başarısız — Message={Message}", statusCheck.Message);
            return new ErrorDataResult<List<N11OrderDto>>(null!, statusCheck.Message);
        }

        var orders = response
            .Element("orderList")?
            .Elements("order")
            .Select(ParseOrder)
            .ToList() ?? [];

        logger.LogInformation("N11 FetchOrders başarılı — {Count} sipariş alındı", orders.Count);
        return new SuccessDataResult<List<N11OrderDto>>(orders);
    }

    // -----------------------------------------------------------------------
    // GetOrderDetailAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId)
    {
        var request = new XElement(Ns + "OrderDetailRequest",
            new XElement("orderRequest",
                new XElement("id", orderId)));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("OrderService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 GetOrderDetail SOAP çağrısı başarısız — OrderId={OrderId}", orderId);
            return new ErrorDataResult<N11OrderDto>(null!, $"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 OrderDetail başarısız — OrderId={OrderId}, Message={Message}",
                orderId, statusCheck.Message);
            return new ErrorDataResult<N11OrderDto>(null!, statusCheck.Message);
        }

        var orderElement = response.Element("orderDetail")?.Element("order")
            ?? response.Element("orderDetail");

        if (orderElement is null)
        {
            logger.LogWarning("N11 OrderDetail yanıtında sipariş elementi bulunamadı — OrderId={OrderId}", orderId);
            return new ErrorDataResult<N11OrderDto>(null!, "N11 yanıtında sipariş bulunamadı.");
        }

        var order = ParseOrder(orderElement);
        logger.LogInformation("N11 GetOrderDetail başarılı — OrderId={OrderId}", orderId);
        return new SuccessDataResult<N11OrderDto>(order);
    }

    // -----------------------------------------------------------------------
    // AcceptOrderItemAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> AcceptOrderItemAsync(long orderItemId, int numberOfPackages = 1)
    {
        var request = new XElement(Ns + "OrderItemAcceptRequest",
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", orderItemId))),
            new XElement("numberOfPackages", numberOfPackages));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("OrderService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 AcceptOrderItem SOAP çağrısı başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 AcceptOrderItem başarısız — OrderItemId={OrderItemId}, Message={Message}",
                orderItemId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 AcceptOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
        return new SuccessResult("Sipariş kalemi kabul edildi.");
    }

    // -----------------------------------------------------------------------
    // RejectOrderItemAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> RejectOrderItemAsync(long orderItemId, string rejectReason, string rejectReasonType)
    {
        var request = new XElement(Ns + "OrderItemRejectRequest",
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", orderItemId))),
            new XElement("rejectReason", rejectReason),
            new XElement("rejectReasonType", rejectReasonType));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("OrderService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 RejectOrderItem SOAP çağrısı başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 RejectOrderItem başarısız — OrderItemId={OrderItemId}, Message={Message}",
                orderItemId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 RejectOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
        return new SuccessResult("Sipariş kalemi reddedildi.");
    }

    // -----------------------------------------------------------------------
    // ShipOrderItemAsync
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IResult> ShipOrderItemAsync(long orderItemId, int shipmentCompanyId, string trackingNumber, int shipmentMethod = 1)
    {
        var request = new XElement(Ns + "MakeOrderItemShipmentRequest",
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", orderItemId),
                    new XElement("shipmentInfo",
                        new XElement("shipmentCompany",
                            new XElement("id", shipmentCompanyId)),
                        new XElement("trackingNumber", trackingNumber),
                        new XElement("shipmentMethod", shipmentMethod)))));

        XElement response;
        try
        {
            response = await soapClient.SendAsync("OrderService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 ShipOrderItem SOAP çağrısı başarısız — OrderItemId={OrderItemId}", orderItemId);
            return new ErrorResult($"N11 bağlantı hatası: {ex.Message}");
        }

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 ShipOrderItem başarısız — OrderItemId={OrderItemId}, Message={Message}",
                orderItemId, statusCheck.Message);
            return statusCheck;
        }

        logger.LogInformation("N11 ShipOrderItem başarılı — OrderItemId={OrderItemId}", orderItemId);
        return new SuccessResult("Sipariş kalemi kargoya verildi.");
    }

    // -----------------------------------------------------------------------
    // XML parsing
    // -----------------------------------------------------------------------

    private static N11OrderDto ParseOrder(XElement order)
    {
        long.TryParse(order.Element("id")?.Value, out var id);
        decimal.TryParse(order.Element("totalAmount")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var totalAmount);

        DateTimeOffset createDate = default;
        var createDateStr = order.Element("createDate")?.Value;
        if (!string.IsNullOrEmpty(createDateStr) &&
            DateTime.TryParseExact(createDateStr, DateFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            createDate = new DateTimeOffset(parsedDate, TimeSpan.Zero);
        }

        return new N11OrderDto
        {
            Id = id,
            OrderNumber = order.Element("orderNumber")?.Value ?? string.Empty,
            Status = order.Element("status")?.Value ?? string.Empty,
            TotalAmount = totalAmount,
            PaymentType = order.Element("paymentType")?.Value,
            CreateDate = createDate,
            CitizenshipId = order.Element("citizen")?.Value,
            Buyer = ParseBuyer(order.Element("buyer")),
            BillingAddress = ParseAddress(order.Element("billingAddress")),
            ShippingAddress = ParseAddress(order.Element("shippingAddress")),
            OrderItems = order.Element("orderItemList")?
                .Elements("orderItem")
                .Select(ParseOrderItem)
                .ToList() ?? []
        };
    }

    private static N11BuyerDto? ParseBuyer(XElement? buyer)
    {
        if (buyer is null) return null;
        return new N11BuyerDto(
            buyer.Element("firstName")?.Value,
            buyer.Element("lastName")?.Value,
            buyer.Element("email")?.Value);
    }

    private static N11AddressDto? ParseAddress(XElement? address)
    {
        if (address is null) return null;
        return new N11AddressDto(
            address.Element("city")?.Value,
            address.Element("district")?.Value,
            address.Element("fullAddress")?.Value,
            address.Element("postalCode")?.Value);
    }

    private static N11OrderItemDto ParseOrderItem(XElement item)
    {
        long.TryParse(item.Element("id")?.Value, out var id);
        long.TryParse(item.Element("productId")?.Value, out var productId);
        int.TryParse(item.Element("quantity")?.Value, out var quantity);
        decimal.TryParse(item.Element("unitPrice")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
        decimal.TryParse(item.Element("discount")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var discountRaw);
        decimal? discount = item.Element("discount") is not null ? discountRaw : null;
        decimal.TryParse(item.Element("vatRate")?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRateRaw);
        decimal? vatRate = item.Element("vatRate") is not null ? vatRateRaw : null;

        return new N11OrderItemDto
        {
            Id = id,
            ProductId = productId,
            ProductSellerCode = item.Element("productSellerCode")?.Value,
            ProductName = item.Element("productName")?.Value,
            Quantity = quantity,
            Price = price,
            Discount = discount,
            VatRate = vatRate,
            Status = item.Element("status")?.Value,
            Shipment = ParseShipment(item.Element("shipmentInfo"))
        };
    }

    private static N11ShipmentDto? ParseShipment(XElement? shipmentInfo)
    {
        if (shipmentInfo is null) return null;
        return new N11ShipmentDto(
            shipmentInfo.Element("shipmentCompany")?.Value,
            shipmentInfo.Element("trackingNumber")?.Value,
            shipmentInfo.Element("campaignNumber")?.Value);
    }
}
