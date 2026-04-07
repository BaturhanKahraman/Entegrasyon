using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pttavm;

// --- Sipariş ---

public sealed record PttavmOrder(
    [property: JsonPropertyName("SiparişNo")] string? SiparişNo,
    [property: JsonPropertyName("SiparişDurumu")] string? SiparişDurumu,
    [property: JsonPropertyName("kdvDahilToplamTutar")] decimal KdvDahilToplamTutar,
    [property: JsonPropertyName("kargoTutari")] decimal KargoTutari,
    [property: JsonPropertyName("SiparişTarihi")] DateTime? SiparişTarihi,
    [property: JsonPropertyName("SiparişUrunler")] List<PttavmOrderItem>? SiparişUrunler,
    [property: JsonPropertyName("faturaMusteriAdi")] string? FaturaMusteriAdi,
    [property: JsonPropertyName("faturaMusteriSoyadi")] string? FaturaMusteriSoyadi);

public sealed record PttavmOrderItem(
    [property: JsonPropertyName("urunId")] int UrunId,
    [property: JsonPropertyName("urunAdi")] string? UrunAdi,
    [property: JsonPropertyName("barkod")] string? Barkod,
    [property: JsonPropertyName("adet")] int Adet,
    [property: JsonPropertyName("birimFiyat")] decimal BirimFiyat,
    [property: JsonPropertyName("toplamFiyat")] decimal ToplamFiyat,
    [property: JsonPropertyName("lineItemId")] int LineItemId);

public sealed record PttavmOrderDetail(
    [property: JsonPropertyName("SiparişNo")] string? SiparişNo,
    [property: JsonPropertyName("SiparişDurumu")] string? SiparişDurumu,
    [property: JsonPropertyName("kdvDahilToplamTutar")] decimal KdvDahilToplamTutar,
    [property: JsonPropertyName("kargoTutari")] decimal KargoTutari,
    [property: JsonPropertyName("SiparişTarihi")] DateTime? SiparişTarihi,
    [property: JsonPropertyName("SiparişUrunler")] List<PttavmOrderItem>? SiparişUrunler,
    [property: JsonPropertyName("faturaMusteriAdi")] string? FaturaMusteriAdi,
    [property: JsonPropertyName("faturaMusteriSoyadi")] string? FaturaMusteriSoyadi,
    [property: JsonPropertyName("faturaAdresi")] string? FaturaAdresi,
    [property: JsonPropertyName("teslimatAdresi")] string? TeslimatAdresi);

// --- Kargo Bilgi ---

public sealed record PttavmCargoInfo(
    [property: JsonPropertyName("productId")] string? ProductId,
    [property: JsonPropertyName("shopId")] int ShopId,
    [property: JsonPropertyName("inCargo")] string? InCargo,
    [property: JsonPropertyName("referenceCode")] string? ReferenceCode,
    [property: JsonPropertyName("currentState")] string? CurrentState,
    [property: JsonPropertyName("deliveryInfo")] string? DeliveryInfo);

// --- Kargo Profil ---

public sealed record PttavmCargoProfileResponse(
    [property: JsonPropertyName("cargoProfiles")] List<PttavmCargoProfile>? CargoProfiles);

public sealed record PttavmCargoProfile(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("type")] string? Type);

// --- Depo ---

public sealed record PttavmWarehouse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("error")] bool Error,
    [property: JsonPropertyName("msg")] string? Msg,
    [property: JsonPropertyName("status")] bool Status);

// --- Barkod Olustur ---

public sealed record PttavmBarcodeRequest(
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("warehouse_id")] int WarehouseId);

public sealed record PttavmBarcodeCreateRequestBody(
    [property: JsonPropertyName("orders")] List<PttavmBarcodeRequest> Orders);

public sealed record PttavmBarcodeCreateResult(
    [property: JsonPropertyName("tracking_id")] string? TrackingId,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("error")] bool Error);

// --- Barkod Status ---

public sealed record PttavmBarcodeStatusRequestBody(
    [property: JsonPropertyName("tracking_id")] string TrackingId);

public sealed record PttavmBarcodeStatusResult(
    [property: JsonPropertyName("tracking_id")] string? TrackingId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] List<PttavmBarcodeStatusData>? Data,
    [property: JsonPropertyName("error")] string? Error);

public sealed record PttavmBarcodeStatusData(
    [property: JsonPropertyName("order_id")] string? OrderId,
    [property: JsonPropertyName("barcodes")] List<string>? Barcodes);

// --- Barkod Etiket ---

public sealed record PttavmBarcodeTagRequest(
    [property: JsonPropertyName("barcode")] string Barcode,
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("type")] string? Type);

// --- No Shipping Order ---

public sealed record PttavmNoShippingOrderRequest(
    [property: JsonPropertyName("order_id")] string OrderId);

public sealed record PttavmNoShippingResult(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("status")] bool Status);

// --- Fatura ---

public sealed record PttavmInvoiceRequest(
    [property: JsonPropertyName("lineItemId")] List<int> LineItemId,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("url")] string? Url);

public sealed record PttavmInvoiceResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error_Message")] string? ErrorMessage);
