using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Temu;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Temu pazaryerinden kategori ağacını import eder.
/// API router pattern: type=bg.goods.cats.get ile kategori ağacı çekilir,
/// recursive Children yapısı ExternalCategoryDto'ya dönüştürülür.
/// </summary>
// TODO: Temu API dokümanı doğrulanınca endpoint ve response yapısı güncellenecek
public class TemuCategoryImporter : BaseCategoryImporterService
{
    private readonly ITemuApiClient _apiClient;
    private const string MarketplaceName = "Temu";

    public override ImportSource Source => ImportSource.Temu;

    public TemuCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        ITemuApiClient apiClient,
        ILogger<TemuCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Temu API'sinden kategori ağacını çeker ve ExternalCategoryDto listesine dönüştürür.
    /// type: bg.goods.cats.get — recursive ağaç yapısı döner (children listeli).
    /// </summary>
    // TODO: Temu API dokümanı doğrulanınca endpoint güncellenecek
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.CallAsync<TemuCategoryListResponse>(
                "bg.goods.cats.get", null, cancellationToken);

            var catList = response.CatList;

            var roots = catList
                .Select(MapToExternalCategory)
                .ToList();

            Logger.LogInformation(
                "Temu'dan {Count} kök kategori oluşturuldu",
                roots.Count);

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(roots);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Temu kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Import işleminden önce Temu marketplace kaydını yükler, ardından temel sınıfın
    /// transaction döngüsünü çalıştırır.
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri başarıyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata oluştu", Source);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// TemuCategoryDto'yu ExternalCategoryDto'ya recursive olarak dönüştürür.
    /// Leaf: Children listesi boş ve Leaf=true.
    /// </summary>
    private static ExternalCategoryDto MapToExternalCategory(TemuCategoryDto dto)
    {
        var children = dto.Children
            .Select(MapToExternalCategory)
            .ToList();

        return new ExternalCategoryDto
        {
            ExternalId = dto.CatId.ToString(),
            Name = dto.CatName,
            ParentExternalId = dto.ParentCatId > 0 ? dto.ParentCatId.ToString() : null,
            HasChildren = !dto.Leaf,
            Children = children
        };
    }
}
