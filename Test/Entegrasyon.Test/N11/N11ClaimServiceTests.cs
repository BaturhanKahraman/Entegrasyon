using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity.Dtos.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11ClaimService için birim testleri.
/// İptal ve iade talep SOAP çağrılarının doğru WSDL yoluna gittiğini,
/// parametrelerin SOAP isteğine doğru eklendiğini ve yanıtların parse edildiğini doğrular.
/// </summary>
public class N11ClaimServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<ILogger<N11ClaimService>> _loggerMock = new();

    private N11ClaimService CreateSut() => new(_soapClientMock.Object, _loggerMock.Object);

    // -----------------------------------------------------------------------
    // Yardımcı metodlar — gerçekçi N11 SOAP yanıtları
    // -----------------------------------------------------------------------

    private static XElement BuildSuccessResponse(string rootName, XElement? payload = null)
    {
        var response = new XElement(rootName,
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorCode"),
                new XElement("errorMessage")));
        if (payload is not null)
            response.Add(payload);
        return response;
    }

    private static XElement BuildFailureResponse(string rootName, string errorMessage = "N11 sistem hatası")
    {
        return new XElement(rootName,
            new XElement("result",
                new XElement("status", "failure"),
                new XElement("errorCode", "ERR-001"),
                new XElement("errorMessage", errorMessage)));
    }

    private static XElement BuildCancelClaimElement(
        long claimCancelId = 1001L,
        string status = "REQUESTED",
        string orderNumber = "N11-CANCEL-001",
        string productName = "Test Ürünü",
        int quantity = 1,
        decimal unitPrice = 299.90m,
        string cancelReasonType = "PRODUCT_DEFECT",
        string cancelReasonDescription = "Ürün bozuk geldi",
        string buyerName = "Ahmet Yılmaz",
        string buyerEmail = "ahmet@test.com")
    {
        return new XElement("claimCancel",
            new XElement("id", claimCancelId),
            new XElement("status", status),
            new XElement("orderNumber", orderNumber),
            new XElement("productName", productName),
            new XElement("quantity", quantity),
            new XElement("unitPrice", unitPrice.ToString("F2")),
            new XElement("cancelReasonType", cancelReasonType),
            new XElement("cancelReasonDescription", cancelReasonDescription),
            new XElement("buyerName", buyerName),
            new XElement("buyerEmail", buyerEmail),
            new XElement("requestDate", "22/03/2026 10:00:00"));
    }

    private static XElement BuildReturnClaimElement(
        long claimReturnId = 2001L,
        string status = "REQUESTED",
        string orderNumber = "N11-RETURN-001",
        string productName = "İade Ürünü",
        int quantity = 2,
        decimal unitPrice = 149.95m,
        decimal finalPrice = 299.90m,
        string returnReasonType = "WRONG_PRODUCT",
        string returnReasonDescription = "Yanlış ürün gönderildi",
        string buyerName = "Fatma Demir",
        string buyerEmail = "fatma@test.com",
        string shipmentCompany = "Yurtiçi Kargo",
        string trackingNumber = "YK987654")
    {
        return new XElement("claimReturn",
            new XElement("id", claimReturnId),
            new XElement("status", status),
            new XElement("orderNumber", orderNumber),
            new XElement("productName", productName),
            new XElement("quantity", quantity),
            new XElement("unitPrice", unitPrice.ToString("F2")),
            new XElement("finalPrice", finalPrice.ToString("F2")),
            new XElement("returnReasonType", returnReasonType),
            new XElement("returnReasonDescription", returnReasonDescription),
            new XElement("buyerName", buyerName),
            new XElement("buyerEmail", buyerEmail),
            new XElement("requestDate", "22/03/2026 11:30:00"),
            new XElement("shipmentCompany", shipmentCompany),
            new XElement("trackingNumber", trackingNumber));
    }

    private static XElement BuildReasonTypeElement(long id, string value)
        => new XElement("denyReasonType",
            new XElement("id", id),
            new XElement("value", value));

    // -----------------------------------------------------------------------
    // Test 1: GetCancelClaimsAsync — ClaimCancelService WSDL'ine gider
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCancelClaimsAsync_ShouldCallClaimCancelService()
    {
        // Arrange
        var cancelClaim = BuildCancelClaimElement();
        var payload = new XElement("claimCancelList", cancelClaim);

        string? capturedWsdl = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdl = wsdl)
            .ReturnsAsync(BuildSuccessResponse("ClaimCancelListResponse", payload));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCancelClaimsAsync();

        // Assert
        capturedWsdl.Should().Be("ClaimCancelService");
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var claim = result.Data[0];
        claim.ClaimCancelId.Should().Be(1001);
        claim.Status.Should().Be("REQUESTED");
        claim.OrderNumber.Should().Be("N11-CANCEL-001");
        claim.ProductName.Should().Be("Test Ürünü");
        claim.Quantity.Should().Be(1);
        claim.UnitPrice.Should().Be(299.90m);
        claim.CancelReasonType.Should().Be("PRODUCT_DEFECT");
        claim.BuyerName.Should().Be("Ahmet Yılmaz");
        claim.BuyerEmail.Should().Be("ahmet@test.com");
    }

    // -----------------------------------------------------------------------
    // Test 2: ApproveCancelAsync — claimCancelId SOAP isteğine eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveCancelAsync_ShouldSendClaimCancelId()
    {
        // Arrange
        const long claimCancelId = 5001L;
        XElement? capturedRequest = null;
        string? capturedWsdl = null;

        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, req) =>
            {
                capturedWsdl = wsdl;
                capturedRequest = req;
            })
            .ReturnsAsync(BuildSuccessResponse("ClaimCancelApproveResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.ApproveCancelAsync(claimCancelId);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdl.Should().Be("ClaimCancelService");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("claimCancelId").FirstOrDefault()?.Value.Should().Be("5001");
    }

    // -----------------------------------------------------------------------
    // Test 3: DenyCancelAsync — denyReasonId ve denyReasonNote eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DenyCancelAsync_ShouldIncludeReasonIdAndNote()
    {
        // Arrange
        const long claimCancelId = 6001L;
        const long denyReasonId = 3L;
        const string denyReasonNote = "Talep koşulları sağlanmıyor";

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("ClaimCancelService", It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("ClaimCancelDenyResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.DenyCancelAsync(claimCancelId, denyReasonId, denyReasonNote);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("claimCancelId").FirstOrDefault()?.Value.Should().Be("6001");
        capturedRequest.Descendants("denyReasonId").FirstOrDefault()?.Value.Should().Be("3");
        capturedRequest.Descendants("denyReasonNote").FirstOrDefault()?.Value.Should().Be(denyReasonNote);
    }

    // -----------------------------------------------------------------------
    // Test 4: GetReturnClaimsAsync — ReturnService WSDL'ine gider
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetReturnClaimsAsync_ShouldCallReturnService()
    {
        // Arrange
        var returnClaim = BuildReturnClaimElement();
        var payload = new XElement("claimReturnList", returnClaim);

        string? capturedWsdl = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdl = wsdl)
            .ReturnsAsync(BuildSuccessResponse("ClaimReturnListResponse", payload));

        var sut = CreateSut();

        // Act
        var result = await sut.GetReturnClaimsAsync();

        // Assert
        capturedWsdl.Should().Be("ReturnService");
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var claim = result.Data[0];
        claim.ClaimReturnId.Should().Be(2001);
        claim.Status.Should().Be("REQUESTED");
        claim.OrderNumber.Should().Be("N11-RETURN-001");
        claim.ProductName.Should().Be("İade Ürünü");
        claim.Quantity.Should().Be(2);
        claim.UnitPrice.Should().Be(149.95m);
        claim.FinalPrice.Should().Be(299.90m);
        claim.ReturnReasonType.Should().Be("WRONG_PRODUCT");
        claim.BuyerName.Should().Be("Fatma Demir");
        claim.ShipmentCompany.Should().Be("Yurtiçi Kargo");
        claim.TrackingNumber.Should().Be("YK987654");
    }

    // -----------------------------------------------------------------------
    // Test 5: ApproveReturnAsync — ReturnService WSDL'ine gider
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveReturnAsync_ShouldCallReturnService()
    {
        // Arrange
        const long claimReturnId = 7001L;
        string? capturedWsdl = null;
        XElement? capturedRequest = null;

        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, req) =>
            {
                capturedWsdl = wsdl;
                capturedRequest = req;
            })
            .ReturnsAsync(BuildSuccessResponse("ClaimReturnApproveResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.ApproveReturnAsync(claimReturnId);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdl.Should().Be("ReturnService");
        capturedRequest.Should().NotBeNull();
        // N11 API uses claimCancelId field name for return approve (naming inconsistency)
        capturedRequest!.Descendants("claimCancelId").FirstOrDefault()?.Value.Should().Be("7001");
    }

    // -----------------------------------------------------------------------
    // Test 6: GetCancelDenyReasonsAsync — denyReasonTypeDataList parse edilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCancelDenyReasonsAsync_ShouldParseReasonTypes()
    {
        // Arrange
        var reasonList = new XElement("denyReasonTypeDataList",
            BuildReasonTypeElement(1L, "Stok yok"),
            BuildReasonTypeElement(2L, "Ürün hasarlı"),
            BuildReasonTypeElement(3L, "Talep geçersiz"));

        _soapClientMock
            .Setup(s => s.SendAsync("ClaimCancelService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(BuildSuccessResponse("ClaimCancelDenyReasonTypeResponse", reasonList));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCancelDenyReasonsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);

        result.Data[0].Id.Should().Be(1);
        result.Data[0].Value.Should().Be("Stok yok");
        result.Data[1].Id.Should().Be(2);
        result.Data[1].Value.Should().Be("Ürün hasarlı");
        result.Data[2].Id.Should().Be(3);
        result.Data[2].Value.Should().Be("Talep geçersiz");
    }

    // -----------------------------------------------------------------------
    // Test 7: PendReturnAsync — pendingReasonId, pendingDayCount ve note eklenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PendReturnAsync_ShouldIncludePendingFields()
    {
        // Arrange
        const long claimReturnId = 8001L;
        const long pendingReasonId = 2L;
        const int pendingDayCount = 5;
        const string pendingReasonNote = "Ürün inceleniyor";

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("ReturnService", It.IsAny<string>(), It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("ClaimReturnPendingResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.PendReturnAsync(claimReturnId, pendingReasonId, pendingDayCount, pendingReasonNote);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("claimReturnId").FirstOrDefault()?.Value.Should().Be("8001");
        capturedRequest.Descendants("pendingReasonId").FirstOrDefault()?.Value.Should().Be("2");
        capturedRequest.Descendants("pendingDayCount").FirstOrDefault()?.Value.Should().Be("5");
        capturedRequest.Descendants("pendingReasonNote").FirstOrDefault()?.Value.Should().Be(pendingReasonNote);
    }

    // -----------------------------------------------------------------------
    // Test 8: GetCancelClaimsAsync — SOAP failure durumunda ErrorDataResult döner
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCancelClaimsAsync_WhenSoapFails_ShouldReturnError()
    {
        // Arrange
        const string errorMessage = "Yetkilendirme hatası";
        _soapClientMock
            .Setup(s => s.SendAsync("ClaimCancelService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(BuildFailureResponse("ClaimCancelListResponse", errorMessage));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCancelClaimsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }
}
