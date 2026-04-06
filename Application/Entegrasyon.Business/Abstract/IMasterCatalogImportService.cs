using Entegrasyon.Entity.Dtos.MasterCatalog;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Master catalog (AdminPanelDb) içeriğini tenant'ın IntegrationDb'sine aktarır.
/// </summary>
public interface IMasterCatalogImportService
{
    /// <summary>
    /// Seçili master kategorileri (ve alt ağaçlarını) tenant DB'sine kopyalar.
    /// </summary>
    Task<ImportResultDto> ImportFromMasterAsync(
        int tenantId,
        IList<int> masterCategoryIds,
        CancellationToken ct = default);

    /// <summary>Master kategori ağacının tamamını getirir (UI'da göstermek için).</summary>
    Task<IList<MasterCategoryTreeDto>> GetMasterCategoryTreeAsync(CancellationToken ct = default);

    /// <summary>Tüm aktif sektör paketlerini getirir.</summary>
    Task<IList<SectorPackageDto>> GetSectorPackagesAsync(CancellationToken ct = default);

    /// <summary>Belirtilen sektör paketine bağlı master kategori ID'lerini getirir.</summary>
    Task<IList<int>> GetSectorPackageCategoryIdsAsync(int sectorPackageId, CancellationToken ct = default);

    /// <summary>
    /// Seçili master markaları tenant DB'sine kopyalar.
    /// masterBrandIds boşsa tüm aktif markalar içe aktarılır.
    /// </summary>
    Task<ImportResultDto> ImportBrandsFromMasterAsync(
        int tenantId,
        IList<int>? masterBrandIds = null,
        CancellationToken ct = default);

    /// <summary>Tüm aktif master markaları getirir (UI'da seçim için).</summary>
    Task<IList<MasterBrandDto>> GetMasterBrandsAsync(CancellationToken ct = default);

    /// <summary>Master markaları isme göre arar (sayfalı, UI arama için).</summary>
    Task<IList<MasterBrandDto>> SearchMasterBrandsAsync(string query, int limit = 50, CancellationToken ct = default);
}
