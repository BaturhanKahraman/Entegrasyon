using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada listing sorgulama yanıtı.
/// GET /listings/merchantid/{merchantId}
/// </summary>
public sealed record HepsiburadaListingResponse(
    [property: JsonPropertyName("listings")] List<HepsiburadaListingItem>? Listings,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("offset")] int Offset);

public sealed record HepsiburadaListingItem(
    [property: JsonPropertyName("hepsiburadaSku")] string HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string? MerchantSku,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("availableStock")] int AvailableStock,
    [property: JsonPropertyName("dispatchTime")] int DispatchTime,
    [property: JsonPropertyName("cargoCompany1")] string? CargoCompany1,
    [property: JsonPropertyName("isSalable")] bool IsSalable,
    [property: JsonPropertyName("isLocked")] bool IsLocked,
    [property: JsonPropertyName("lockReasons")] List<string>? LockReasons);

/// <summary>
/// Fiyat güncelleme isteği.
/// POST /listings/merchantid/{merchantId}/price-uploads
/// </summary>
public sealed record HepsiburadaPriceUpdateItem(
    [property: JsonPropertyName("hepsiburadaSku")] string HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string MerchantSku,
    [property: JsonPropertyName("price")] decimal Price);

/// <summary>
/// Stok güncelleme isteği.
/// POST /listings/merchantid/{merchantId}/stock-uploads
/// </summary>
public sealed record HepsiburadaStockUpdateItem(
    [property: JsonPropertyName("hepsiburadaSku")] string HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string MerchantSku,
    [property: JsonPropertyName("availableStock")] int AvailableStock,
    [property: JsonPropertyName("maximumPurchasableQuantity")] int? MaximumPurchasableQuantity);

/// <summary>
/// Teslimat bilgisi güncelleme isteği.
/// POST /listings/merchantid/{merchantId}/shipping-info-uploads
/// </summary>
public sealed record HepsiburadaShippingInfoUpdateItem(
    [property: JsonPropertyName("hepsiburadaSku")] string HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string MerchantSku,
    [property: JsonPropertyName("dispatchTime")] int DispatchTime,
    [property: JsonPropertyName("cargoCompany1")] string CargoCompany1,
    [property: JsonPropertyName("cargoCompany2")] string? CargoCompany2,
    [property: JsonPropertyName("shippingProfileName")] string? ShippingProfileName,
    [property: JsonPropertyName("shippingAddressLabel")] string? ShippingAddressLabel,
    [property: JsonPropertyName("claimAddressLabel")] string? ClaimAddressLabel);

/// <summary>
/// Listing güncelleme yanıtı (fiyat/stok/kargo).
/// </summary>
public sealed record HepsiburadaListingUpdateResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("errors")] List<HepsiburadaListingUpdateError>? Errors);

public sealed record HepsiburadaListingUpdateError(
    [property: JsonPropertyName("elementNo")] int ElementNo,
    [property: JsonPropertyName("hepsiburadaSku")] string? HepsiburadaSku,
    [property: JsonPropertyName("merchantSku")] string? MerchantSku,
    [property: JsonPropertyName("errors")] List<string>? Errors);
