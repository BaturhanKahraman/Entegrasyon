using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Log;

namespace Entegrasyon.MVC.Features.IntegrationHealth.ViewModels;

public class IntegrationHealthVm
{
    public int RecentSyncCount { get; set; }
    public int RecentErrorCount { get; set; }
    public DateTimeOffset? LastSyncTime { get; set; }
    public List<ApplicationLogDetailDto> RecentErrors { get; set; } = [];

    /// <summary>
    /// Pazaryeri bazında credential durumu (#81 ile tutarlı). Eksik anahtarlı pazaryeri
    /// "Yapılandırılmamış" gösterilir. Global özet kartlarının altında tablo olarak render edilir.
    /// </summary>
    public List<MarketplaceHealthRow> Marketplaces { get; set; } = [];
}

/// <summary>
/// Entegrasyon sağlığı sayfasındaki tek pazaryeri satırı.
/// </summary>
/// <param name="MarketPlaceId">Pazaryeri Id'si (satır linki / hedef için).</param>
/// <param name="Name">Pazaryeri adı.</param>
/// <param name="HasCredentials">API anahtarları tam yapılandırılmış mı (MarketPlace.IsCredentialComplete()).</param>
public record MarketplaceHealthRow(int MarketPlaceId, string Name, bool HasCredentials);

public static class IntegrationHealthMapper
{
    /// <summary>
    /// Silinmemiş pazaryerlerini sağlık satırlarına çevirir. Credential durumu tek kaynaktan
    /// (<see cref="MarketPlaceCredentialExtensions.IsCredentialComplete"/>) hesaplanır — #81 ile aynı.
    /// </summary>
    public static List<MarketplaceHealthRow> BuildMarketplaceRows(IEnumerable<MarketPlace> marketplaces) =>
        marketplaces
            .Where(mp => !mp.IsDeleted)
            .Select(mp => new MarketplaceHealthRow(mp.Id, mp.Name, mp.IsCredentialComplete()))
            .ToList();
}
