using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;

namespace Entegrasyon.MVC.Features.Products.ViewModels.Activity;

/// <summary>
/// Ürün 360° "Aktivite" sekmesi partial'ının modeli (_ActivityTimeline.cshtml).
/// E-ticaret kapalıysa <see cref="EcommerceEnabled"/> false gelir ve <see cref="Items"/> boştur —
/// Designer bu durumda pasif/empty-state gösterir.
/// </summary>
public sealed class ProductActivityTimelineVm
{
    public required Guid ProductId { get; init; }

    /// <summary>Tenant'ın e-ticaret/pazaryeri paketi aktif mi.</summary>
    public bool EcommerceEnabled { get; init; }

    public IReadOnlyList<ProductActivityLog> Items { get; init; } = [];

    /// <summary>"Daha Fazla Yükle" için bir sonraki sayfanın cursor'u (son satırın CreatedAt'i). null → daha fazla yok.</summary>
    public DateTimeOffset? NextCursor { get; init; }

    /// <summary>Keyset tie-break: aynı CreatedAt'li satırları atlamamak için son satırın Id'si. hx-get `?cursorId={NextCursorId}`.</summary>
    public long? NextCursorId { get; init; }

    /// <summary>Bu sayfada pageSize kadar kayıt geldiyse muhtemelen devamı var.</summary>
    public bool HasMore { get; init; }

    public int PageSize { get; init; }

    /// <summary>Filtre durumunu URL/partial yeniden render için yansıtır.</summary>
    public ProductActivityTimelineFilter AppliedFilter { get; init; } = ProductActivityTimelineFilter.Empty;
}
