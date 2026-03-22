using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed record PazaramaCreateProductRequest(
    [property: JsonPropertyName("products")] List<PazaramaProductItem> Products);

public sealed record PazaramaProductItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("brandId")] string BrandId,
    [property: JsonPropertyName("desi")] int Desi,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("groupCode")] string GroupCode,
    [property: JsonPropertyName("stockCode")] string StockCode,
    [property: JsonPropertyName("stockCount")] int StockCount,
    [property: JsonPropertyName("vatRate")] int VatRate,
    [property: JsonPropertyName("listPrice")] decimal ListPrice,
    [property: JsonPropertyName("salePrice")] decimal SalePrice,
    [property: JsonPropertyName("categoryId")] string CategoryId,
    [property: JsonPropertyName("currencyType")] string CurrencyType,
    [property: JsonPropertyName("images")] List<PazaramaProductImage> Images,
    [property: JsonPropertyName("attributes")] List<PazaramaProductAttribute> Attributes);

public sealed record PazaramaProductImage(
    [property: JsonPropertyName("imageurl")] string ImageUrl);

public sealed record PazaramaProductAttribute(
    [property: JsonPropertyName("attributeId")] string AttributeId,
    [property: JsonPropertyName("attributeValueId")] string AttributeValueId);

public sealed record PazaramaStockUpdateRequest(
    [property: JsonPropertyName("items")] List<PazaramaStockUpdateItem> Items);

public sealed record PazaramaStockUpdateItem(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("stockCount")] int StockCount);

public sealed record PazaramaPriceUpdateRequest(
    [property: JsonPropertyName("items")] List<PazaramaPriceUpdateItem> Items);

public sealed record PazaramaPriceUpdateItem(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("listPrice")] decimal ListPrice,
    [property: JsonPropertyName("salePrice")] decimal SalePrice);
