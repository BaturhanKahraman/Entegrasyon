using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Ürün bazlı aktivite logları — timeline görünümü için.
/// Her Trendyol işlemi bu servise bir kayıt ekler.
/// </summary>
public interface IProductActivityLogger
{
    Task LogAsync(Guid productId, ProductActivityType activityType, string message,
        ProductActivityStatus status = ProductActivityStatus.Info,
        string? detail = null, string? marketplaceName = null, string? referenceId = null);

    Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(Guid productId, int limit = 50);

    /// <summary>
    /// Ürün 360° aktivite sayfası için filtreli + cursor-based pagination'lı timeline.
    /// IX_ProductActivityLogs_ProductId_CreatedAt composite index üzerinden çalışır;
    /// ek filtreler (pazaryeri/tip/durum/tarih) tek ürünün satırlarına daraltılmış küme üzerinde uygulanır.
    /// </summary>
    Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(
        Guid productId, ProductActivityTimelineFilter filter, int pageSize = 20);
}
