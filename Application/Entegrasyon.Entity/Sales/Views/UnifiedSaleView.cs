namespace Entegrasyon.Entity.Sales.Views;

/// <summary>
/// vw_unified_sales view'ına bağlı keyless entity.
/// Sale ve Order tablolarını birleşik okuma için kullanılır.
/// </summary>
public sealed class UnifiedSaleView
{
    public Guid Id { get; init; }

    /// <summary>0 = Sale, 1 = Order</summary>
    public int EntityType { get; init; }

    public UnifiedSaleSource Source { get; init; }

    public string? Number { get; init; }

    public DateTimeOffset SaleDate { get; init; }

    public int? CustomerId { get; init; }

    public string? CustomerDisplayName { get; init; }

    public decimal TotalPrice { get; init; }

    public int ItemCount { get; init; }

    /// <summary>Source'a göre yorumlanan raw durum kodu.</summary>
    public int RawStatusCode { get; init; }

    public string? CargoTrackingNumber { get; init; }

    public int? MarketPlaceId { get; init; }
}
