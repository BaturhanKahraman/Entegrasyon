using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity.Dtos.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11OrderService için birim testleri.
/// SOAP yanıtlarının doğru parse edildiğini ve hata durumlarında
/// beklenen sonuçların döndürüldüğünü doğrular.
/// </summary>
public class N11OrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<ILogger<N11OrderService>> _loggerMock = new();

    private N11OrderService CreateSut() => new(_soapClientMock.Object, _loggerMock.Object);

    // -----------------------------------------------------------------------
    // Yardımcı metodlar — gerçekçi N11 SOAP yanıtları
    // -----------------------------------------------------------------------

    private static XElement BuildFetchOrdersResponse(IEnumerable<XElement> orderElements)
    {
        return new XElement("DetailedOrderListResponse",
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorCode"),
                new XElement("errorMessage")),
            new XElement("orderList",
                orderElements));
    }

    private static XElement BuildGetOrderDetailResponse(XElement orderElement)
    {
        return new XElement("OrderDetailResponse",
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorCode"),
                new XElement("errorMessage")),
            new XElement("orderDetail",
                orderElement));
    }

    private static XElement BuildFailureResponse(string errorMessage = "N11 sistem hatası")
    {
        return new XElement("DetailedOrderListResponse",
            new XElement("result",
                new XElement("status", "failure"),
                new XElement("errorCode", "ERR-001"),
                new XElement("errorMessage", errorMessage)));
    }

    private static XElement BuildOrderElement(
        long id = 123456L,
        string orderNumber = "N11-MOCK-001",
        string status = "New",
        decimal totalAmount = 299.90m,
        string createDate = "22/03/2026",
        string? citizenshipId = "12345678901",
        string? firstName = "Ahmet",
        string? lastName = "Yılmaz",
        string? email = "ahmet@test.com",
        string? city = "İstanbul",
        string? district = "Kadıköy",
        string? fullAddress = "Test Mah. Test Sk. No:1",
        string? postalCode = "34000")
    {
        return new XElement("order",
            new XElement("id", id),
            new XElement("orderNumber", orderNumber),
            new XElement("status", status),
            new XElement("totalAmount", totalAmount.ToString("F2")),
            new XElement("createDate", createDate),
            new XElement("citizen", citizenshipId),
            new XElement("buyer",
                new XElement("firstName", firstName),
                new XElement("lastName", lastName),
                new XElement("email", email)),
            new XElement("billingAddress",
                new XElement("city", city),
                new XElement("district", district),
                new XElement("fullAddress", fullAddress),
                new XElement("postalCode", postalCode)),
            new XElement("shippingAddress",
                new XElement("city", city),
                new XElement("district", district),
                new XElement("fullAddress", fullAddress),
                new XElement("postalCode", postalCode)),
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", 9001L),
                    new XElement("productId", 5001L),
                    new XElement("productSellerCode", "SKU-001"),
                    new XElement("productName", "Test Ürünü"),
                    new XElement("quantity", 2),
                    new XElement("unitPrice", 149.95m.ToString("F2")),
                    new XElement("discount", 0),
                    new XElement("vatRate", 18),
                    new XElement("status", "New"),
                    new XElement("shipmentInfo",
                        new XElement("shipmentCompany", "Yurtiçi Kargo"),
                        new XElement("trackingNumber", "YK123456"),
                        new XElement("campaignNumber", "CAMP001")))));
    }

    // -----------------------------------------------------------------------
    // Test 1: FetchOrdersAsync — 2 siparişi doğru parse eder
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchOrdersAsync_ShouldParseSoapResponse()
    {
        // Arrange
        var order1 = BuildOrderElement(id: 1001, orderNumber: "N11-001", status: "New", totalAmount: 299.90m);
        var order2 = BuildOrderElement(id: 1002, orderNumber: "N11-002", status: "Completed", totalAmount: 599.00m,
            firstName: "Fatma", lastName: "Demir", email: "fatma@test.com",
            city: "Ankara", district: "Çankaya");

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFetchOrdersResponse([order1, order2]));

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);

        var first = result.Data[0];
        first.Id.Should().Be(1001);
        first.OrderNumber.Should().Be("N11-001");
        first.Status.Should().Be("New");
        first.TotalAmount.Should().Be(299.90m);
        first.Buyer!.FirstName.Should().Be("Ahmet");
        first.Buyer.LastName.Should().Be("Yılmaz");
        first.Buyer.Email.Should().Be("ahmet@test.com");
        first.BillingAddress!.City.Should().Be("İstanbul");
        first.ShippingAddress!.District.Should().Be("Kadıköy");
        first.OrderItems.Should().HaveCount(1);

        var item = first.OrderItems[0];
        item.Id.Should().Be(9001);
        item.ProductId.Should().Be(5001);
        item.ProductSellerCode.Should().Be("SKU-001");
        item.Quantity.Should().Be(2);
        item.Price.Should().Be(149.95m);
        item.VatRate.Should().Be(18m);
        item.Shipment!.TrackingNumber.Should().Be("YK123456");

        var second = result.Data[1];
        second.Id.Should().Be(1002);
        second.Buyer!.FirstName.Should().Be("Fatma");
    }

    // -----------------------------------------------------------------------
    // Test 2: FetchOrdersAsync — boş orderList döndüğünde boş liste dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchOrdersAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFetchOrdersResponse([]));

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Test 3: GetOrderDetailAsync — tek sipariş doğru parse edilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOrderDetailAsync_ShouldReturnOrder()
    {
        // Arrange
        var orderElement = BuildOrderElement(id: 9999, orderNumber: "N11-DETAIL-001", status: "Completed",
            totalAmount: 1500.00m, citizenshipId: "99988877766");

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildGetOrderDetailResponse(orderElement));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderDetailAsync(9999L);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(9999);
        result.Data.OrderNumber.Should().Be("N11-DETAIL-001");
        result.Data.Status.Should().Be("Completed");
        result.Data.TotalAmount.Should().Be(1500.00m);
        result.Data.CitizenshipId.Should().Be("99988877766");
    }

    // -----------------------------------------------------------------------
    // Test 4: FetchOrdersAsync — SOAP failure döndüğünde ErrorDataResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchOrdersAsync_WhenSoapFails_ShouldReturnError()
    {
        // Arrange
        var errorMessage = "Yetkilendirme hatası";
        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFailureResponse(errorMessage));

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    // -----------------------------------------------------------------------
    // Test 5: FetchOrdersAsync — tarih ve status parametreleri SOAP isteğine eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchOrdersAsync_ShouldPassParametersToSoap()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 3, 22, 0, 0, 0, TimeSpan.Zero);
        const string status = "New";

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildFetchOrdersResponse([]));

        var sut = CreateSut();

        // Act
        await sut.FetchOrdersAsync(startDate, endDate, status, page: 2, pageSize: 25);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("startDate").FirstOrDefault()?.Value.Should().Be("01/01/2026");
        capturedRequest.Descendants("endDate").FirstOrDefault()?.Value.Should().Be("22/03/2026");
        capturedRequest.Descendants("status").FirstOrDefault()?.Value.Should().Be(status);
        capturedRequest.Descendants("currentPage").FirstOrDefault()?.Value.Should().Be("2");
        capturedRequest.Descendants("pageSize").FirstOrDefault()?.Value.Should().Be("25");
    }
}
