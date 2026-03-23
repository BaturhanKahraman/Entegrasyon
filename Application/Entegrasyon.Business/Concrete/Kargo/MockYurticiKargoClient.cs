using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Development ve test ortami icin mock SOAP client.
/// Sabit XML response'lari doner.
/// </summary>
public sealed class MockYurticiKargoClient : IYurticiKargoClient
{
    public Task<string> SendSoapRequestAsync(
        string soapAction,
        string soapBody,
        CancellationToken ct = default)
    {
        var xml = soapAction switch
        {
            "createShipment" => CreateShipmentMockResponse(),
            "queryShipment" => QueryShipmentMockResponse(),
            "cancelShipment" => CancelShipmentMockResponse(),
            _ => throw new NotSupportedException($"Unknown SOAP action: {soapAction}")
        };

        return Task.FromResult(xml);
    }

    private static string CreateShipmentMockResponse() =>
        """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <createShipmentReturn>
              <outFlag>1</outFlag>
              <outResult>Gonderi basariyla olusturuldu.</outResult>
              <jobId>MOCK-JOB-001</jobId>
            </createShipmentReturn>
          </soap:Body>
        </soap:Envelope>
        """;

    private static string QueryShipmentMockResponse() =>
        """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <queryShipmentReturn>
              <shippingDeliveryDetailVO>
                <cargoKey>MOCK-CARGO-001</cargoKey>
                <invKeys>MOCK-INV-001</invKeys>
                <operationCode>4</operationCode>
                <operationMessage>Teslim edildi</operationMessage>
                <deliveryDate>2026-03-20T14:30:00</deliveryDate>
                <deliveredTo>Mock Alici</deliveredTo>
                <unitCount>1</unitCount>
              </shippingDeliveryDetailVO>
            </queryShipmentReturn>
          </soap:Body>
        </soap:Envelope>
        """;

    private static string CancelShipmentMockResponse() =>
        """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <cancelShipmentReturn>
              <outFlag>1</outFlag>
              <outResult>Kargo basariyla iptal edildi.</outResult>
              <cargoKey>MOCK-CARGO-001</cargoKey>
            </cancelShipmentReturn>
          </soap:Body>
        </soap:Envelope>
        """;
}
