using Entegrasyon.Entity.Logs;

namespace Entegrasyon.Entity.Dtos.Product.Activity;

/// <summary>
/// Ürün 360° aktivite timeline'ı için filtre + cursor-based pagination parametreleri.
/// Tüm alanlar opsiyonel; null/boş geçilirse o filtre uygulanmaz.
/// </summary>
public sealed record ProductActivityTimelineFilter(
    IReadOnlyList<string>? MarketplaceNames = null,
    IReadOnlyList<ProductActivityType>? ActivityTypes = null,
    IReadOnlyList<ProductActivityStatus>? Statuses = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    DateTimeOffset? Cursor = null,
    long? CursorId = null)
{
    /// <summary>Hiçbir filtre/cursor uygulanmayan boş filtre.</summary>
    public static ProductActivityTimelineFilter Empty { get; } = new();
}
