using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Kargo;

/// <summary>
/// YurticiKargoService unit testleri.
/// </summary>
public class YurticiKargoServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IYurticiKargoClient> _mockClient = new();
    private readonly Mock<ILogger<YurticiKargoService>> _mockLogger = new();

    private YurticiKargoService CreateSut() => new(
        _mockClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static YurticiCreateShipmentRequest ValidCreateRequest() => new(
        CargoKey: "CARGO-001",
        InvoiceKey: "INV-001",
        ReceiverCustName: "Ahmet Yilmaz",
        ReceiverAddress: "Ataturk Cad. No:1 Kadikoy",
        CityName: "Istanbul",
        TownName: "Kadikoy",
        ReceiverPhone1: "5321234567");

    // ── CreateShipmentAsync Tests ────────────────────────────────────────────

    [Fact]
    public async Task CreateShipmentAsync_NullRequest_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(null!);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyCargoKey_ReturnsError()
    {
        var sut = CreateSut();
        var request = ValidCreateRequest() with { CargoKey = "" };
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("CargoKey");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyReceiverName_ReturnsError()
    {
        var sut = CreateSut();
        var request = ValidCreateRequest() with { ReceiverCustName = "" };
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Alici");
    }

    [Fact]
    public async Task CreateShipmentAsync_SuccessfulResponse_ReturnsSuccess()
    {
        var responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <createShipmentReturn>
                  <outFlag>1</outFlag>
                  <outResult>Gonderi basariyla olusturuldu.</outResult>
                  <jobId>JOB-12345</jobId>
                </createShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        _mockClient
            .Setup(x => x.SendSoapRequestAsync("createShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(ValidCreateRequest());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.OutFlag.Should().Be("1");
        result.Data.JobId.Should().Be("JOB-12345");
    }

    [Fact]
    public async Task CreateShipmentAsync_ApiFailure_ReturnsError()
    {
        var responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <createShipmentReturn>
                  <outFlag>0</outFlag>
                  <outResult>Gecersiz kargo anahtari.</outResult>
                </createShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        _mockClient
            .Setup(x => x.SendSoapRequestAsync("createShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(ValidCreateRequest());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz");
    }

    [Fact]
    public async Task CreateShipmentAsync_ClientException_ReturnsError()
    {
        _mockClient
            .Setup(x => x.SendSoapRequestAsync("createShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(ValidCreateRequest());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── QueryShipmentAsync Tests ─────────────────────────────────────────────

    [Fact]
    public async Task QueryShipmentAsync_NullRequest_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.QueryShipmentAsync(null!);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task QueryShipmentAsync_EmptyKeys_ReturnsError()
    {
        var sut = CreateSut();
        var request = new YurticiQueryShipmentRequest(Keys: []);
        var result = await sut.QueryShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task QueryShipmentAsync_SuccessfulResponse_ReturnsList()
    {
        var responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <queryShipmentReturn>
                  <shippingDeliveryDetailVO>
                    <cargoKey>CARGO-001</cargoKey>
                    <invKeys>INV-001</invKeys>
                    <operationCode>4</operationCode>
                    <operationMessage>Teslim edildi</operationMessage>
                    <deliveryDate>2026-03-20T14:30:00</deliveryDate>
                    <deliveredTo>Ahmet Yilmaz</deliveredTo>
                    <unitCount>1</unitCount>
                  </shippingDeliveryDetailVO>
                </queryShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        _mockClient
            .Setup(x => x.SendSoapRequestAsync("queryShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();
        var request = new YurticiQueryShipmentRequest(Keys: ["CARGO-001"]);
        var result = await sut.QueryShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data[0].CargoKey.Should().Be("CARGO-001");
        result.Data[0].OperationCode.Should().Be(4);
        result.Data[0].DeliveredTo.Should().Be("Ahmet Yilmaz");
    }

    [Fact]
    public async Task QueryShipmentAsync_ClientException_ReturnsError()
    {
        _mockClient
            .Setup(x => x.SendSoapRequestAsync("queryShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var sut = CreateSut();
        var request = new YurticiQueryShipmentRequest(Keys: ["CARGO-001"]);
        var result = await sut.QueryShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Timeout");
    }

    // ── CancelShipmentAsync Tests ────────────────────────────────────────────

    [Fact]
    public async Task CancelShipmentAsync_EmptyCargoKey_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("CargoKey");
    }

    [Fact]
    public async Task CancelShipmentAsync_SuccessfulResponse_ReturnsSuccess()
    {
        var responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <cancelShipmentReturn>
                  <outFlag>1</outFlag>
                  <outResult>Kargo basariyla iptal edildi.</outResult>
                  <cargoKey>CARGO-001</cargoKey>
                </cancelShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        _mockClient
            .Setup(x => x.SendSoapRequestAsync("cancelShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("CARGO-001");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelShipmentAsync_ApiFailure_ReturnsError()
    {
        var responseXml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <cancelShipmentReturn>
                  <outFlag>0</outFlag>
                  <outResult>Kargo zaten teslim edilmis.</outResult>
                </cancelShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        _mockClient
            .Setup(x => x.SendSoapRequestAsync("cancelShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("CARGO-001");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("teslim");
    }

    [Fact]
    public async Task CancelShipmentAsync_ClientException_ReturnsError()
    {
        _mockClient
            .Setup(x => x.SendSoapRequestAsync("cancelShipment", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("CARGO-001");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── SOAP Body Builder Tests ──────────────────────────────────────────────

    [Fact]
    public void BuildCreateShipmentSoapBody_ContainsAllRequiredFields()
    {
        var request = ValidCreateRequest();
        var xml = YurticiKargoService.BuildCreateShipmentSoapBody(request);

        xml.Should().Contain("<cargoKey>CARGO-001</cargoKey>");
        xml.Should().Contain("<invoiceKey>INV-001</invoiceKey>");
        xml.Should().Contain("<receiverCustName>Ahmet Yilmaz</receiverCustName>");
        xml.Should().Contain("<cityName>Istanbul</cityName>");
        xml.Should().Contain("<townName>Kadikoy</townName>");
        xml.Should().Contain("<receiverPhone1>5321234567</receiverPhone1>");
        xml.Should().Contain("<cargoCount>1</cargoCount>");
        xml.Should().Contain("soapenv:Envelope");
        xml.Should().Contain("createShipment");
    }

    [Fact]
    public void BuildCreateShipmentSoapBody_OptionalFields_IncludedWhenSet()
    {
        var request = ValidCreateRequest() with
        {
            Desi = 2.5m,
            Kg = 1.3m,
            Description = "Test gonderi",
            EmailAddress = "test@example.com"
        };

        var xml = YurticiKargoService.BuildCreateShipmentSoapBody(request);

        xml.Should().Contain("<desi>2.5</desi>");
        xml.Should().Contain("<kg>1.3</kg>");
        xml.Should().Contain("<description>Test gonderi</description>");
        xml.Should().Contain("<emailAddress>test@example.com</emailAddress>");
    }

    [Fact]
    public void BuildCreateShipmentSoapBody_XmlEscaping_HandlesSpecialCharacters()
    {
        var request = ValidCreateRequest() with
        {
            ReceiverCustName = "Ali & Veli <Test>"
        };

        var xml = YurticiKargoService.BuildCreateShipmentSoapBody(request);

        xml.Should().Contain("Ali &amp; Veli &lt;Test&gt;");
        xml.Should().NotContain("Ali & Veli <Test>");
    }

    [Fact]
    public void BuildQueryShipmentSoapBody_ContainsKeys()
    {
        var request = new YurticiQueryShipmentRequest(
            Keys: ["KEY-1", "KEY-2"],
            KeyType: 0);

        var xml = YurticiKargoService.BuildQueryShipmentSoapBody(request);

        xml.Should().Contain("<keys>KEY-1</keys>");
        xml.Should().Contain("<keys>KEY-2</keys>");
        xml.Should().Contain("<keyType>0</keyType>");
        xml.Should().Contain("queryShipment");
    }

    [Fact]
    public void BuildCancelShipmentSoapBody_ContainsCargoKey()
    {
        var xml = YurticiKargoService.BuildCancelShipmentSoapBody("CARGO-999");

        xml.Should().Contain("<cargoKeys>CARGO-999</cargoKeys>");
        xml.Should().Contain("cancelShipment");
    }

    // ── XML Parser Tests ─────────────────────────────────────────────────────

    [Fact]
    public void ParseCreateShipmentResponse_ValidXml_ReturnsCorrectValues()
    {
        var xml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <createShipmentReturn>
                  <outFlag>1</outFlag>
                  <outResult>Basarili</outResult>
                  <jobId>JOB-999</jobId>
                </createShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        var result = YurticiKargoService.ParseCreateShipmentResponse(xml);

        result.OutFlag.Should().Be("1");
        result.OutResult.Should().Be("Basarili");
        result.JobId.Should().Be("JOB-999");
    }

    [Fact]
    public void ParseQueryShipmentResponse_MultipleShipments_ReturnsAll()
    {
        var xml = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <queryShipmentReturn>
                  <shippingDeliveryDetailVO>
                    <cargoKey>CARGO-A</cargoKey>
                    <operationCode>2</operationCode>
                    <operationMessage>Subede</operationMessage>
                  </shippingDeliveryDetailVO>
                  <shippingDeliveryDetailVO>
                    <cargoKey>CARGO-B</cargoKey>
                    <operationCode>4</operationCode>
                    <operationMessage>Teslim edildi</operationMessage>
                  </shippingDeliveryDetailVO>
                </queryShipmentReturn>
              </soap:Body>
            </soap:Envelope>
            """;

        var result = YurticiKargoService.ParseQueryShipmentResponse(xml);

        result.Should().HaveCount(2);
        result[0].CargoKey.Should().Be("CARGO-A");
        result[0].OperationCode.Should().Be(2);
        result[1].CargoKey.Should().Be("CARGO-B");
        result[1].OperationCode.Should().Be(4);
    }

    // ── Mock Service Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task MockService_CreateShipment_ReturnsSuccess()
    {
        var mock = new MockYurticiKargoService();
        var result = await mock.CreateShipmentAsync(ValidCreateRequest());

        result.Success.Should().BeTrue();
        result.Data.OutFlag.Should().Be("1");
    }

    [Fact]
    public async Task MockService_CreateShipment_NullRequest_ReturnsError()
    {
        var mock = new MockYurticiKargoService();
        var result = await mock.CreateShipmentAsync(null!);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_QueryShipment_ReturnsShipments()
    {
        var mock = new MockYurticiKargoService();
        var request = new YurticiQueryShipmentRequest(Keys: ["KEY-1", "KEY-2"]);
        var result = await mock.QueryShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task MockService_QueryShipment_EmptyKeys_ReturnsError()
    {
        var mock = new MockYurticiKargoService();
        var request = new YurticiQueryShipmentRequest(Keys: []);
        var result = await mock.QueryShipmentAsync(request);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MockService_CancelShipment_ReturnsSuccess()
    {
        var mock = new MockYurticiKargoService();
        var result = await mock.CancelShipmentAsync("CARGO-001");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_CancelShipment_EmptyKey_ReturnsError()
    {
        var mock = new MockYurticiKargoService();
        var result = await mock.CancelShipmentAsync("");

        result.Success.Should().BeFalse();
    }
}
