using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Kargo;

/// <summary>
/// SuratKargoService unit testleri.
/// </summary>
public class SuratKargoServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ISuratKargoClient> _mockSoapClient = new();
    private readonly Mock<ILogger<SuratKargoService>> _mockLogger = new();

    private SuratKargoService CreateSut() => new(
        _mockSoapClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static readonly XNamespace TempUri = "http://tempuri.org/";

    private static XElement CreateSuccessResponse(string operationName, Dictionary<string, string> fields)
    {
        var responseElement = new XElement(TempUri + $"{operationName}Response");
        var resultElement = new XElement(TempUri + $"{operationName}Result");

        foreach (var field in fields)
        {
            resultElement.Add(new XElement(TempUri + field.Key, field.Value));
        }

        responseElement.Add(resultElement);
        return responseElement;
    }

    // ── CreateShipment Tests ────────────────────────────────────────────────

    [Fact]
    public async Task CreateShipmentAsync_EmptyReceiverName_ReturnsError()
    {
        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "",
            ReceiverAddress: "Test adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Alıcı adı");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyPhone_ReturnsError()
    {
        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "Test adres",
            ReceiverPhone: "",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("telefon");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyAddress_ReturnsError()
    {
        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("adres");
    }

    [Fact]
    public async Task CreateShipmentAsync_ZeroPieceCount_ReturnsError()
    {
        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "Test adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 0);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Parça sayısı");
    }

    [Fact]
    public async Task CreateShipmentAsync_SuccessfulResponse_ReturnsTrackingNumber()
    {
        var response = CreateSuccessResponse("CreateShipment", new Dictionary<string, string>
        {
            ["resultCode"] = "0",
            ["resultMessage"] = "Basarili",
            ["shippingOrderNo"] = "SK-123456",
            ["barcodeNo"] = "BC-789"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.Is<string>(s => s.Contains("CreateShipment")),
                It.Is<string>(s => s == "CreateShipment"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "Ataturk Cad. No:123",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1,
            ReferenceNo: "ORD-001",
            Weight: 1.5m);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TrackingNumber.Should().Be("SK-123456");
        result.Data.BarcodeNo.Should().Be("BC-789");
    }

    [Fact]
    public async Task CreateShipmentAsync_ApiReturnsError_ReturnsError()
    {
        var response = CreateSuccessResponse("CreateShipment", new Dictionary<string, string>
        {
            ["resultCode"] = "1",
            ["resultMessage"] = "Musteri kodu gecersiz"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "Adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Musteri kodu gecersiz");
    }

    [Fact]
    public async Task CreateShipmentAsync_SoapException_ReturnsError()
    {
        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Ali Yilmaz",
            ReceiverAddress: "Adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Kadikoy",
            PieceCount: 1);

        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── QueryShipment Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task QueryShipmentAsync_EmptyTracking_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.QueryShipmentAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Takip numarası");
    }

    [Fact]
    public async Task QueryShipmentAsync_SuccessfulResponse_ReturnsStatus()
    {
        var response = CreateSuccessResponse("QueryShipmentInfo", new Dictionary<string, string>
        {
            ["resultCode"] = "0",
            ["resultMessage"] = "Basarili",
            ["shipmentStatus"] = "Teslim Edildi",
            ["deliveryDate"] = "2026-03-22T14:30:00"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.Is<string>(s => s.Contains("QueryShipmentInfo")),
                It.Is<string>(s => s == "QueryShipmentInfo"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.QueryShipmentAsync("SK-123456");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Status.Should().Be("Teslim Edildi");
        result.Data.DeliveryDate.Should().NotBeNull();
        result.Data.TrackingNumber.Should().Be("SK-123456");
    }

    [Fact]
    public async Task QueryShipmentAsync_ApiError_ReturnsError()
    {
        var response = CreateSuccessResponse("QueryShipmentInfo", new Dictionary<string, string>
        {
            ["resultCode"] = "1",
            ["resultMessage"] = "Gonderi bulunamadi"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.QueryShipmentAsync("INVALID");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gonderi bulunamadi");
    }

    // ── CancelShipment Tests ────────────────────────────────────────────────

    [Fact]
    public async Task CancelShipmentAsync_EmptyTracking_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Takip numarası");
    }

    [Fact]
    public async Task CancelShipmentAsync_SuccessfulResponse_ReturnsSuccess()
    {
        var response = CreateSuccessResponse("CancelShipment", new Dictionary<string, string>
        {
            ["resultCode"] = "0",
            ["resultMessage"] = "Basarili"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.Is<string>(s => s.Contains("CancelShipment")),
                It.Is<string>(s => s == "CancelShipment"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("SK-123456");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelShipmentAsync_ApiError_ReturnsError()
    {
        var response = CreateSuccessResponse("CancelShipment", new Dictionary<string, string>
        {
            ["resultCode"] = "1",
            ["resultMessage"] = "Gonderi zaten teslim edilmis"
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("SK-123456");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("teslim edilmis");
    }

    // ── GetBarcode Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBarcodeAsync_EmptyReference_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetBarcodeAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Referans numarası");
    }

    [Fact]
    public async Task GetBarcodeAsync_SuccessfulResponse_ReturnsBarcodeData()
    {
        var response = CreateSuccessResponse("GetBarcodeByReferenceNo", new Dictionary<string, string>
        {
            ["resultCode"] = "0",
            ["barcodeBase64"] = "SGVsbG8gV29ybGQ="
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.Is<string>(s => s.Contains("GetBarcodeByReferenceNo")),
                It.Is<string>(s => s == "GetBarcodeByReferenceNo"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetBarcodeAsync("ORD-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
    }

    // ── GetShipmentLabel Tests ──────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentLabelAsync_EmptyTracking_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetShipmentLabelAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Takip numarası");
    }

    [Fact]
    public async Task GetShipmentLabelAsync_SuccessfulResponse_ReturnsLabelData()
    {
        var response = CreateSuccessResponse("GetShipmentLabel", new Dictionary<string, string>
        {
            ["resultCode"] = "0",
            ["labelData"] = "JVBER..."
        });

        _mockSoapClient
            .Setup(x => x.SendAsync(
                It.Is<string>(s => s.Contains("GetShipmentLabel")),
                It.Is<string>(s => s == "GetShipmentLabel"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetShipmentLabelAsync("SK-123456", "PDF");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
    }

    // ── MockSuratKargoService Tests ─────────────────────────────────────────

    [Fact]
    public async Task MockService_CreateShipment_ReturnsFakeTracking()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var request = new SuratKargoShipmentRequest(
            ReceiverName: "Test User",
            ReceiverAddress: "Test Adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Besiktas",
            PieceCount: 1);

        var result = await mock.CreateShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TrackingNumber.Should().StartWith("MOCK-SK-");
    }

    [Fact]
    public async Task MockService_CreateShipment_EmptyName_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var request = new SuratKargoShipmentRequest(
            ReceiverName: "",
            ReceiverAddress: "Test Adres",
            ReceiverPhone: "05551234567",
            ReceiverCityName: "Istanbul",
            ReceiverTownName: "Besiktas",
            PieceCount: 1);

        var result = await mock.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_QueryShipment_ReturnsFakeStatus()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.QueryShipmentAsync("MOCK-SK-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Status.Should().NotBeNullOrWhiteSpace();
        result.Data.Movements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MockService_QueryShipment_EmptyTracking_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.QueryShipmentAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_CancelShipment_ReturnsSuccess()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.CancelShipmentAsync("MOCK-SK-001");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_CancelShipment_EmptyTracking_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.CancelShipmentAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_GetBarcode_ReturnsBase64()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.GetBarcodeAsync("ORD-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MockService_GetBarcode_EmptyRef_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.GetBarcodeAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_GetLabel_ReturnsBase64()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.GetShipmentLabelAsync("MOCK-SK-001", "PDF");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MockService_GetLabel_EmptyTracking_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<MockSuratKargoService>>();
        var mock = new MockSuratKargoService(mockLogger.Object);

        var result = await mock.GetShipmentLabelAsync("");

        result.Success.Should().BeFalse();
    }
}
