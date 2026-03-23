using System.Net;
using System.Text;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Pttavm;

public sealed class MockPttavmCatalogApiClient : IPttavmCatalogApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var json = relativeUrl switch
        {
            var u when u.Contains("categories/main") => """
            {
              "success": true,
              "main_category": [
                {"id": "1", "name": "Elektronik", "updated_at": "2026-01-01T00:00:00"},
                {"id": "2", "name": "Giyim", "updated_at": "2026-01-01T00:00:00"},
                {"id": "3", "name": "Ev & Yasam", "updated_at": "2026-01-01T00:00:00"}
              ],
              "error": null
            }
            """,
            var u when u.Contains("categories/") => """
            {
              "success": true,
              "category": {
                "id": "1",
                "name": "Elektronik",
                "parent_id": null,
                "updated_at": "2026-01-01T00:00:00",
                "children": [
                  {"id": "11", "name": "Telefon", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []},
                  {"id": "12", "name": "Bilgisayar", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []}
                ]
              },
              "error": null
            }
            """,
            _ => """{"success": true, "error": null}"""
        };

        return Task.FromResult(CreateResponse(json));
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
        => Task.FromResult(CreateResponse("""{"success": true, "error": null}"""));

    public Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
        => Task.FromResult(CreateResponse("""{"success": true, "error": null}"""));

    private static HttpResponseMessage CreateResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
