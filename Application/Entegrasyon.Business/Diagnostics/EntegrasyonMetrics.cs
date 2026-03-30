using System.Diagnostics.Metrics;

namespace Entegrasyon.Business.Diagnostics;

/// <summary>
/// Merkezi Meter — tüm custom iş metrikleri buradan oluşturulur.
/// Program.cs'te .AddMeter(EntegrasyonMetrics.MeterName) ile kayıt edilmeli.
/// </summary>
public static class EntegrasyonMetrics
{
    public const string MeterName = "Entegrasyon.Business";
    public static readonly Meter Meter = new(MeterName);

    // ── Sayaçlar (Counters) ──────────────────────────────────────────────────

    /// <summary>Toplam ürün senkronizasyon sayısı (marketplace tag'i ile)</summary>
    public static readonly Counter<long> ProductSyncTotal = Meter.CreateCounter<long>(
        "entegrasyon.product_sync.total",
        unit: "operations",
        description: "Total product sync operations by marketplace");

    /// <summary>Ürün senkronizasyon hatası sayısı</summary>
    public static readonly Counter<long> ProductSyncErrors = Meter.CreateCounter<long>(
        "entegrasyon.product_sync.errors",
        unit: "errors",
        description: "Product sync errors by marketplace");

    /// <summary>Pazaryerinden içe aktarılan sipariş sayısı</summary>
    public static readonly Counter<long> OrdersImported = Meter.CreateCounter<long>(
        "entegrasyon.orders.imported",
        unit: "orders",
        description: "Orders imported from marketplaces");

    // ── Histogramlar (Histograms) ────────────────────────────────────────────

    /// <summary>Ürün senkronizasyonu süresi (milisaniye)</summary>
    public static readonly Histogram<double> ProductSyncDuration = Meter.CreateHistogram<double>(
        "entegrasyon.product_sync.duration",
        unit: "ms",
        description: "Product sync duration in milliseconds");

    /// <summary>Marketplace API çağrı süresi (milisaniye)</summary>
    public static readonly Histogram<double> MarketplaceApiDuration = Meter.CreateHistogram<double>(
        "entegrasyon.marketplace_api.duration",
        unit: "ms",
        description: "Marketplace API call duration in milliseconds");
}
