using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11 sipariş aksiyonları için birim testleri.
/// Accept, Reject ve Ship işlemlerinin doğru SOAP çağrısı yapıp yapmadığını
/// ve hata durumlarını doğru ele alıp almadığını doğrular.
/// </summary>
public class N11OrderActionsTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<ILogger<N11OrderService>> _loggerMock = new();

    private N11OrderService CreateSut() => new(_soapClientMock.Object, _loggerMock.Object);

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    private static XElement BuildSuccessResponse(string rootName = "Response") =>
        new XElement(rootName,
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorCode"),
                new XElement("errorMessage")));

    private static XElement BuildFailureResponse(string errorMessage = "N11 sistem hatası", string rootName = "Response") =>
        new XElement(rootName,
            new XElement("result",
                new XElement("status", "failure"),
                new XElement("errorCode", "ERR-001"),
                new XElement("errorMessage", errorMessage)));

    // -----------------------------------------------------------------------
    // Test 1: AcceptOrderItemAsync — OrderService WSDL'e çağrı yapılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AcceptOrderItemAsync_ShouldCallOrderService()
    {
        // Arrange
        const long orderItemId = 7001L;
        const int numberOfPackages = 2;

        XElement? capturedRequest = null;
        string? capturedWsdl = null;

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, req) =>
            {
                capturedWsdl = wsdl;
                capturedRequest = req;
            })
            .ReturnsAsync(BuildSuccessResponse("OrderItemAcceptResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.AcceptOrderItemAsync(orderItemId, numberOfPackages);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdl.Should().Be("OrderService");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("id").FirstOrDefault()?.Value.Should().Be(orderItemId.ToString());
        capturedRequest.Descendants("numberOfPackages").FirstOrDefault()?.Value.Should().Be(numberOfPackages.ToString());
    }

    // -----------------------------------------------------------------------
    // Test 2: RejectOrderItemAsync — rejectReason ve rejectReasonType SOAP gövdesine eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RejectOrderItemAsync_ShouldIncludeReasonInSoapBody()
    {
        // Arrange
        const long orderItemId = 7002L;
        const string rejectReason = "Ürün stokta yok";
        const string rejectReasonType = "OUT_OF_STOCK";

        XElement? capturedRequest = null;

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("OrderItemRejectResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.RejectOrderItemAsync(orderItemId, rejectReason, rejectReasonType);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("id").FirstOrDefault()?.Value.Should().Be(orderItemId.ToString());
        capturedRequest.Descendants("rejectReason").FirstOrDefault()?.Value.Should().Be(rejectReason);
        capturedRequest.Descendants("rejectReasonType").FirstOrDefault()?.Value.Should().Be(rejectReasonType);
    }

    // -----------------------------------------------------------------------
    // Test 3: ShipOrderItemAsync — trackingNumber, companyId ve shipmentMethod eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ShipOrderItemAsync_ShouldIncludeShipmentInfo()
    {
        // Arrange
        const long orderItemId = 7003L;
        const int shipmentCompanyId = 5;
        const string trackingNumber = "YK987654321";
        const int shipmentMethod = 1;

        XElement? capturedRequest = null;

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("MakeOrderItemShipmentResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.ShipOrderItemAsync(orderItemId, shipmentCompanyId, trackingNumber, shipmentMethod);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("id").FirstOrDefault()?.Value.Should().Be(orderItemId.ToString());
        capturedRequest.Descendants("trackingNumber").FirstOrDefault()?.Value.Should().Be(trackingNumber);
        capturedRequest.Descendants("shipmentMethod").FirstOrDefault()?.Value.Should().Be(shipmentMethod.ToString());

        // shipmentCompany/id alanını doğrula
        var shipmentCompanyElement = capturedRequest
            .Descendants("shipmentCompany")
            .FirstOrDefault();
        shipmentCompanyElement.Should().NotBeNull();
        shipmentCompanyElement!.Element("id")?.Value.Should().Be(shipmentCompanyId.ToString());
    }

    // -----------------------------------------------------------------------
    // Test 4: AcceptOrderItemAsync — SOAP failure döndüğünde ErrorResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AcceptOrderItemAsync_WhenFailure_ShouldReturnError()
    {
        // Arrange
        const long orderItemId = 7004L;
        const string errorMessage = "Sipariş öğesi bulunamadı";

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFailureResponse(errorMessage, "OrderItemAcceptResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.AcceptOrderItemAsync(orderItemId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    // -----------------------------------------------------------------------
    // Test 5: ShipOrderItemAsync — varsayılan shipmentMethod=1 (Kargo) kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ShipOrderItemAsync_DefaultShipmentMethod_ShouldBeCargo()
    {
        // Arrange
        const long orderItemId = 7005L;

        XElement? capturedRequest = null;

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("MakeOrderItemShipmentResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.ShipOrderItemAsync(orderItemId, shipmentCompanyId: 3, trackingNumber: "ABC123");

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest!.Descendants("shipmentMethod").FirstOrDefault()?.Value.Should().Be("1");
    }

    // -----------------------------------------------------------------------
    // Test 6: AcceptOrderItemAsync — varsayılan numberOfPackages=1 kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AcceptOrderItemAsync_DefaultNumberOfPackages_ShouldBeOne()
    {
        // Arrange
        const long orderItemId = 7006L;

        XElement? capturedRequest = null;

        _soapClientMock
            .Setup(s => s.SendAsync("OrderService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("OrderItemAcceptResponse"));

        var sut = CreateSut();

        // Act
        await sut.AcceptOrderItemAsync(orderItemId);

        // Assert
        capturedRequest!.Descendants("numberOfPackages").FirstOrDefault()?.Value.Should().Be("1");
    }
}
