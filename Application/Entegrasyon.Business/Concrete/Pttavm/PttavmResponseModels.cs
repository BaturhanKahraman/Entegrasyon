using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pttavm;

// --- Kategori ---

public sealed record PttavmMainCategoryResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("main_category")] List<PttavmCategoryDto>? MainCategory,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryTreeResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category_tree")] List<PttavmCategoryTreeDto>? CategoryTree,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDetailResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category")] PttavmCategoryTreeDto? Category,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt);

public sealed record PttavmCategoryTreeDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("parent_id")] string? ParentId,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("children")] List<PttavmCategoryTreeDto>? Children);

// --- Urun Upsert/StockPrice Response ---

public sealed record PttavmUpsertResult(
    [property: JsonPropertyName("countOfProductsToBeProcessed")] int CountOfProductsToBeProcessed,
    [property: JsonPropertyName("trackingId")] string? TrackingId,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message);

// --- Tracking Result Response ---

public sealed record PttavmTrackingResult(
    [property: JsonPropertyName("trackingId")] string? TrackingId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("progress")] int Progress,
    [property: JsonPropertyName("createdAt")] DateTime? CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt,
    [property: JsonPropertyName("productsSubTrackingResult")] PttavmSubTrackingResult? ProductsSubTrackingResult);

public sealed record PttavmSubTrackingResult(
    [property: JsonPropertyName("countOfTotalProducts")] int CountOfTotalProducts,
    [property: JsonPropertyName("countOfWaitingProducts")] int CountOfWaitingProducts,
    [property: JsonPropertyName("countOfInProgressProducts")] int CountOfInProgressProducts,
    [property: JsonPropertyName("countOfCompletedProducts")] int CountOfCompletedProducts,
    [property: JsonPropertyName("countOfCancelledProducts")] int CountOfCancelledProducts,
    [property: JsonPropertyName("productBasedInfos")] List<PttavmProductTrackingInfo>? ProductBasedInfos);

public sealed record PttavmProductTrackingInfo(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("failureReasons")] List<string>? FailureReasons,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("productId")] int? ProductId);

// --- Product Info Response ---

public sealed record PttavmProductInfo(
    [property: JsonPropertyName("urunId")] int UrunId,
    [property: JsonPropertyName("barkod")] string? Barkod,
    [property: JsonPropertyName("urunAdi")] string? UrunAdi,
    [property: JsonPropertyName("miktar")] int Miktar,
    [property: JsonPropertyName("kdVsiz")] decimal KdvSiz,
    [property: JsonPropertyName("kdVli")] decimal KdvLi,
    [property: JsonPropertyName("kdvOran")] int KdvOran,
    [property: JsonPropertyName("aktif")] bool Aktif,
    [property: JsonPropertyName("mevcut")] bool Mevcut,
    [property: JsonPropertyName("iskonto")] decimal Iskonto,
    [property: JsonPropertyName("anaKategoriId")] int AnaKategoriId,
    [property: JsonPropertyName("altKategoriId")] int AltKategoriId,
    [property: JsonPropertyName("resimListesi")] List<PttavmImageDto>? ResimListesi,
    [property: JsonPropertyName("variantListesi")] List<PttavmVariantDto>? VariantListesi);

public sealed record PttavmImageDto(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("sira")] int Sira);

public sealed record PttavmVariantDto(
    [property: JsonPropertyName("barkod")] string? Barkod,
    [property: JsonPropertyName("miktar")] int Miktar,
    [property: JsonPropertyName("fiyat")] decimal Fiyat);

// --- Product Status Response ---

public sealed record PttavmBaseResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("errorCode")] string? ErrorCode);

// --- Faulty Images Response ---

public sealed record PttavmFaultyImagesResult(
    [property: JsonPropertyName("productImagesWithErrorList")] List<PttavmFaultyImageProduct>? ProductImagesWithErrorList,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message);

public sealed record PttavmFaultyImageProduct(
    [property: JsonPropertyName("productBarcode")] string? ProductBarcode,
    [property: JsonPropertyName("infos")] List<PttavmFaultyImageInfo>? Infos);

public sealed record PttavmFaultyImageInfo(
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("errorReason")] string? ErrorReason,
    [property: JsonPropertyName("occurredAt")] DateTime? OccurredAt);

// --- Ortak ---

public sealed record PttavmError(
    [property: JsonPropertyName("error_code")] string? ErrorCode,
    [property: JsonPropertyName("error_message")] string? ErrorMessage);
