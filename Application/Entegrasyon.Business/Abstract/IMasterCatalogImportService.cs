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
}
