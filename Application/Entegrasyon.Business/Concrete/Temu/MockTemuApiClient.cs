using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Temu;

/// <summary>
/// Mock Temu API client — development ve test için.
/// Gerçek HTTP çağrısı yapmaz, statik JSON yanıtlar döndürür.
/// </summary>
public sealed class MockTemuApiClient(
    ILogger<MockTemuApiClient> logger) : ITemuApiClient
{
    /// <inheritdoc />
    public Task<T> CallAsync<T>(string type, object? parameters, CancellationToken ct)
    {
        logger.LogDebug("[MOCK] Temu API call: {Type}", type);

        string json = type switch
        {
            // TODO: Temu API dokümanı doğrulanınca mock response'lar güncellenecek
            "bg.goods.cats.get" => """
            {
                "cat_list": [
                    {
                        "cat_id": 1,
                        "cat_name": "Mock Elektronik",
                        "parent_cat_id": 0,
                        "leaf": false,
                        "children": [
                            {
                                "cat_id": 11,
                                "cat_name": "Mock Telefon",
                                "parent_cat_id": 1,
                                "leaf": true,
                                "children": []
                            }
                        ]
                    },
                    {
                        "cat_id": 2,
                        "cat_name": "Mock Giyim",
                        "parent_cat_id": 0,
                        "leaf": false,
                        "children": []
                    }
                ]
            }
            """,
            "bg.goods.cat.template.get" => """
            {
                "attributes": []
            }
            """,
            _ => "{}"
        };

        var result = JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"[MOCK] Temu API deserialize failed for type: {type}");

        return Task.FromResult(result);
    }
}
