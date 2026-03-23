using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pttavm;

// ── Upsert Product Request ──────────────────────────────────────────────────

public sealed record PttavmProductRequest(
    [property: JsonPropertyName("categoryId")] int? CategoryId,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("priceWithoutVat")] decimal? PriceWithoutVat,
    [property: JsonPropertyName("vatRate")] int? VatRate,
    [property: JsonPropertyName("priceWithVat")] decimal? PriceWithVat,
    [property: JsonPropertyName("quantity")] int? Quantity,
    [property: JsonPropertyName("desi")] double? Desi,
    [property: JsonPropertyName("variants")] List<PttavmVariantRequest>? Variants,
    [property: JsonPropertyName("images")] List<PttavmImageRequest>? Images,
    [property: JsonPropertyName("noShippingProduct")] bool? NoShippingProduct,
    [property: JsonPropertyName("warrantyDuration")] int? WarrantyDuration,
    [property: JsonPropertyName("basketMaxQuantity")] int? BasketMaxQuantity);

public sealed record PttavmVariantRequest(
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("quantity")] int? Quantity,
    [property: JsonPropertyName("price")] decimal? Price,
    [property: JsonPropertyName("attributes")] List<PttavmVariantAttributeRequest>? Attributes);

public sealed record PttavmVariantAttributeRequest(
    [property: JsonPropertyName("definition")] string Definition,
    [property: JsonPropertyName("value")] string Value);

public sealed record PttavmImageRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("order")] int Order);

// ── Stock/Price Update Request ──────────────────────────────────────────────

public sealed record PttavmStockPriceRequest(
    [property: JsonPropertyName("barcode")] string Barcode,
    [property: JsonPropertyName("active")] bool? Active,
    [property: JsonPropertyName("quantity")] int? Quantity,
    [property: JsonPropertyName("priceWithoutVAT")] decimal? PriceWithoutVat,
    [property: JsonPropertyName("priceWithVAT")] decimal? PriceWithVat,
    [property: JsonPropertyName("vatRate")] int? VatRate,
    [property: JsonPropertyName("discount")] decimal? Discount,
    [property: JsonPropertyName("isCargoFromSupplier")] bool? IsCargoFromSupplier,
    [property: JsonPropertyName("variants")] List<PttavmStockPriceVariantRequest>? Variants);

public sealed record PttavmStockPriceVariantRequest(
    [property: JsonPropertyName("quantity")] int? Quantity,
    [property: JsonPropertyName("price")] decimal? Price,
    [property: JsonPropertyName("attributes")] List<PttavmVariantAttributeRequest>? Attributes);

// ── Product Search Filter ───────────────────────────────────────────────────

public sealed record PttavmProductSearchFilter(
    int? CategoryId = null,
    int? SubCategoryId = null,
    bool? IsActive = null,
    bool? IsInStock = null,
    int? MerchantCategoryId = null,
    int SearchPage = 1);

// ── Product Status Request ──────────────────────────────────────────────────

public sealed record PttavmProductStatusRequest(
    [property: JsonPropertyName("isActive")] bool IsActive);

// ── Faulty Images Request ───────────────────────────────────────────────────

public sealed record PttavmFaultyImagesRequest(
    [property: JsonPropertyName("productBarcodes")] List<string>? ProductBarcodes,
    [property: JsonPropertyName("paginationParameters")] PttavmPaginationParameters? PaginationParameters);

public sealed record PttavmPaginationParameters(
    [property: JsonPropertyName("pageNumber")] int PageNumber,
    [property: JsonPropertyName("pageSize")] int PageSize);

// ── Barcodes Request ────────────────────────────────────────────────────────

public sealed record PttavmGetByBarcodesRequest(
    [property: JsonPropertyName("barcodes")] List<string> Barcodes);
