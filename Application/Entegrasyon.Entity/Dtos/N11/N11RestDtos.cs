namespace Entegrasyon.Entity.Dtos.N11;

// ─────────────────────────────────────────────────────────────────────────────
// Ürün Oluşturma / Güncelleme
// ─────────────────────────────────────────────────────────────────────────────

public record N11CreateProductRequest(N11ProductPayload Payload);

public record N11ProductPayload(string Integrator, List<N11ProductSku> Skus);

public record N11ProductSku(
    string Title,
    string Description,
    long CategoryId,
    string CurrencyType,
    string ProductMainId,
    int PreparingDay,
    string ShipmentTemplate,
    string StockCode,
    long? CatalogId,
    string? Barcode,
    int Quantity,
    List<N11ImageDto> Images,
    List<N11AttributeDto> Attributes,
    decimal SalePrice,
    decimal ListPrice,
    int VatRate,
    int? MaxPurchaseQuantity = null);

public record N11ImageDto(string Url, int Order);

public record N11AttributeDto(long Id, long? ValueId, string? CustomValue);

// ─────────────────────────────────────────────────────────────────────────────
// Ürün Güncelleme (product-update endpoint)
// ─────────────────────────────────────────────────────────────────────────────

public record N11UpdateProductRequest(N11UpdateProductPayload Payload);

public record N11UpdateProductPayload(string Integrator, List<N11UpdateProductSku> Skus);

public record N11UpdateProductSku(
    string StockCode,
    string? Status = null,
    int? PreparingDay = null,
    string? ShipmentTemplate = null,
    string? Description = null,
    int? VatRate = null,
    List<N11AttributeDto>? Attributes = null);

// ─────────────────────────────────────────────────────────────────────────────
// Fiyat / Stok Güncelleme
// ─────────────────────────────────────────────────────────────────────────────

public record N11PriceStockUpdateRequest(N11PriceStockPayload Payload);

public record N11PriceStockPayload(string Integrator, List<N11PriceStockSku> Skus);

public record N11PriceStockSku(
    string StockCode,
    decimal? ListPrice = null,
    decimal? SalePrice = null,
    int? Quantity = null,
    string? CurrencyType = null);

// ─────────────────────────────────────────────────────────────────────────────
// Task Response (product-create / product-update / price-stock-update)
// ─────────────────────────────────────────────────────────────────────────────

public record N11TaskResponse(long Id, string Type, string Status, List<string>? Reasons);

// ─────────────────────────────────────────────────────────────────────────────
// Task Detail
// ─────────────────────────────────────────────────────────────────────────────

public record N11TaskDetailRequest(long TaskId, N11Pageable Pageable);

public record N11Pageable(int Page, int Size);

public record N11TaskDetailResponse(long TaskId, string Status, N11TaskDetailSkus? Skus);

public record N11TaskDetailSkus(List<N11TaskDetailContent> Content);

public record N11TaskDetailContent(string ItemCode, string Status, List<string>? Reasons);

// ─────────────────────────────────────────────────────────────────────────────
// Sipariş (Shipment Packages)
// ─────────────────────────────────────────────────────────────────────────────

public record N11ShipmentPackagesResponse(int TotalPages, int Page, int Size, List<N11ShipmentPackage>? Content);

public record N11ShipmentPackage(
    long Id,
    string? OrderNumber,
    string? Status,
    string? ShipmentStatusDate,
    decimal TotalPrice,
    N11RestBuyerInfo? Buyer,
    N11RestAddressInfo? ShippingAddress,
    N11RestAddressInfo? BillingAddress,
    List<N11ShipmentLine>? Lines);

public record N11RestBuyerInfo(string? FirstName, string? LastName, string? Email);

public record N11RestAddressInfo(string? City, string? District, string? FullAddress, string? PostalCode);

public record N11ShipmentLine(
    long LineId,
    long? ProductId,
    string? ProductName,
    string? StockCode,
    string? Barcode,
    int Quantity,
    decimal UnitPrice,
    string? Status,
    string? ShipmentCompanyName,
    string? TrackingNumber);

// ─────────────────────────────────────────────────────────────────────────────
// Sipariş Güncelleme
// ─────────────────────────────────────────────────────────────────────────────

public record N11OrderUpdateRequest(List<N11OrderLineUpdate> Lines, string Status);

public record N11OrderLineUpdate(long LineId);

// ─────────────────────────────────────────────────────────────────────────────
// Kategori Ağacı
// ─────────────────────────────────────────────────────────────────────────────

public record N11CategoryTreeResponse(long Id, string Name, List<N11CategoryTreeResponse>? SubCategories);

// ─────────────────────────────────────────────────────────────────────────────
// Kategori Özellikleri
// ─────────────────────────────────────────────────────────────────────────────

public record N11CategoryAttributeResponse(long Id, string Name, List<N11RestCategoryAttribute>? CategoryAttributes);

public record N11RestCategoryAttribute(
    long AttributeId,
    string AttributeName,
    bool IsMandatory,
    bool IsVariant,
    bool IsSlicer,
    bool IsCustomValue,
    List<N11RestAttributeValue>? AttributeValues);

public record N11RestAttributeValue(long Id, string Value);
