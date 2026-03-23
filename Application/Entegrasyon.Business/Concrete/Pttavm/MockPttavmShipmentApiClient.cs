using System.Net;
using System.Text;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM kargo API client — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmShipmentApiClient : IPttavmShipmentApiClient
{
    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var json = relativeUrl switch
        {
            var u when u.Contains("get-warehouse") => """
            [
              {"id": 100301619, "name": "Ana Depo", "error": false, "msg": "", "status": true}
            ]
            """,
            var u when u.Contains("create-barcode") => """
            {
              "tracking_id": "mock-tracking-id-123",
              "count": 1,
              "code": 200,
              "success": true,
              "message": "",
              "error": false
            }
            """,
            var u when u.Contains("barcode-status") => """
            {
              "tracking_id": "mock-tracking-id-123",
              "status": "completed",
              "data": [{"order_id": "MOCK-ORD-001", "barcodes": ["MOCK-BC-001"]}],
              "error": ""
            }
            """,
            var u when u.Contains("get-barcode-tag") => "<html><body>Mock Etiket</body></html>",
            var u when u.Contains("update-no-shipping-order") => """{"message": "Basarili", "status": true}""",
            _ => """{"success": true}"""
        };

        var contentType = relativeUrl.Contains("get-barcode-tag") ? "text/html" : "application/json";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, contentType)
        });
    }
}
