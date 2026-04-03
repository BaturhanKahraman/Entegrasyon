using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Templates;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Templates;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hazır eşleştirilmiş entity paketlerinin listelenmesi, çakışma tespiti ve import işlemleri.
/// </summary>
public interface IMatchedEntityImportManager
{
    /// <summary>
    /// Yayınlanmış paketleri listeler.
    /// </summary>
    Task<IDataResult<Pageable<MatchedEntityPackageDto>>> GetAvailablePackagesAsync(
        MatchedEntityPackagePaginatedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Paket detayını getirir (tüm hiyerarşi + marketplace mapping'ler dahil).
    /// </summary>
    Task<IDataResult<MatchedEntityPackageDetailDto>> GetPackageDetailAsync(
        int packageId, CancellationToken ct = default);

    /// <summary>
    /// Paket import edildiğinde oluşacak çakışmaları tespit eder.
    /// </summary>
    Task<IDataResult<List<ImportConflictDto>>> DetectConflictsAsync(
        int packageId, CancellationToken ct = default);

    /// <summary>
    /// Paketi import eder. Çakışma çözüm kararlarını içerir.
    /// </summary>
    Task<IResult> ImportPackageAsync(
        ImportPackageRequest request, CancellationToken ct = default);
}
