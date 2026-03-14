namespace Entegrasyon.Entity.Dtos.Trendyol;

public sealed record TrendyolCreateProductRequest(List<TrendyolProductItem> Items);

public sealed record TrendyolProductItem(
    string Barcode,
    string Title,
    string ProductMainId,
    int BrandId,
    int CategoryId,
    decimal ListPrice,
    decimal SalePrice,
    int VatRate,
    string StockCode,
    decimal DimensionalWeight,
    string Description,
    int Quantity,
    List<TrendyolProductImage> Images,
    List<TrendyolProductAttribute> Attributes,
    int? ShipmentAddressId = null,
    int? ReturningAddressId = null,
    int? DeliveryDuration = null);

public sealed record TrendyolProductImage(string Url);

public sealed record TrendyolProductAttribute(
    int AttributeId,
    int? AttributeValueId,
    string? CustomAttributeValue);
