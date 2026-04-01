namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// SendPricingPanel tarafından kullanılan varyant fiyatlandırma satırı.
/// Mevcut fiyat bilgisi ve kullanıcı override'ının gösterilmesi için tasarlanmıştır.
/// </summary>
public sealed record VariantPricingRowDto(
    /// <summary>Varyantın benzersiz kimliği.</summary>
    Guid ProductVariantId,

    /// <summary>Varyant barkodu (örn: "ABC-S").</summary>
    string Barcode,

    /// <summary>Birleştirilmiş varyant özellikleri (örn: "Siyah / S").</summary>
    string VariantName,

    /// <summary>Liste fiyatı.</summary>
    decimal ListPrice,

    /// <summary>Mevcut satış fiyatı (override'dan önce).</summary>
    decimal SalePrice,

    /// <summary>Mevcut stok miktarı.</summary>
    int Quantity,

    /// <summary>Kullanıcı tarafından ayarlanmış override fiyat. Null ise mevcut fiyat kullanılır.</summary>
    decimal? OverridePrice = null
);

/// <summary>
/// SendPricingPanel'den TrendyolProductSendPage'e geri dönen fiyat override sonuçları.
/// </summary>
public sealed record VariantPriceOverrideDto(
    /// <summary>Varyantın benzersiz kimliği.</summary>
    Guid ProductVariantId,

    /// <summary>Override satış fiyatı. Null ise mevcut fiyat kullanılır.</summary>
    decimal? OverrideSalePrice
);
