using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada ürün JSON formatı.
/// POST /api/products/import multipart/form-data JSON dosyası olarak gönderilir.
/// </summary>
public sealed record HepsiburadaProductItem(
    [property: JsonPropertyName("categoryId")] int CategoryId,
    [property: JsonPropertyName("merchant")] string Merchant,
    [property: JsonPropertyName("attributes")] Dictionary<string, object> Attributes);

/// <summary>
/// Hepsiburada hızlı yükleme isteği (Faz 2 — şimdilik kullanılmıyor).
/// </summary>
public sealed record HepsiburadaFastListingItem(
    [property: JsonPropertyName("merchant")] string Merchant,
    [property: JsonPropertyName("merchantSku")] string MerchantSku,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("barcode")] string Barcode,
    [property: JsonPropertyName("hbSku")] string HbSku,
    [property: JsonPropertyName("stock")] string Stock,
    [property: JsonPropertyName("price")] string Price);
