namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Mevcut tenant bağlamını sağlar.
/// Single-tenant modda varsayılan değerler döner.
/// Multi-tenant geçişinde, HTTP request'ten veya auth token'dan tenant bilgisi çekilir.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Mevcut tenant ID. Single-tenant modda 1 döner.
    /// </summary>
    int TenantId { get; }

    /// <summary>
    /// Belirtilen marketplace türü için bu tenant'ın marketplace ID'sini döner.
    /// Single-tenant modda sabit değerler döner (Trendyol=1, N11=2).
    /// Multi-tenant'ta tenant'a özel marketplace kayıtlarından çeker.
    /// </summary>
    int GetMarketPlaceId(string marketplaceName);
}
