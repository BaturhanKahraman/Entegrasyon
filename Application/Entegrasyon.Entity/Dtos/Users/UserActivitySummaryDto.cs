namespace Entegrasyon.Entity.Dtos.Users;

/// <summary>
/// Admin kullanıcı detay sayfasında canlı kullanıcı takibi için özet.
/// Bağlantı durumu (online/offline), son görülme ve ürün/satış aktivite sayıları.
/// Salt-okuma; ApplicationLog (ürün ekleme/güncelleme/silme) + Sales (satış) üzerinden türetilir.
/// </summary>
public sealed record UserActivitySummaryDto(
    Guid Id,
    string FullName,
    string UserName,
    bool IsActive,
    DateTimeOffset? LastSeenAt,
    bool IsOnline,
    int ProductsAdded,
    int ProductsUpdated,
    int ProductsDeleted,
    int SalesCount);
