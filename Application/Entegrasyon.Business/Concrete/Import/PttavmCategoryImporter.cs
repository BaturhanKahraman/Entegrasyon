using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// PttAVM kategori import servisi.
/// Lazy-loading tree: once ana kategorileri yukler, expand'de alt kategorileri getirir.
/// </summary>
public sealed class PttavmCategoryImporter : BaseCategoryImporterService
{
    private readonly IPttavmCatalogApiClient _apiClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PttavmCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IPttavmCatalogApiClient apiClient,
        ILogger<PttavmCategoryImporter> logger) : base(contextFactory, logger)
    {
        _apiClient = apiClient;
    }

    public override ImportSource Source => ImportSource.Pttavm;

    /// <summary>
    /// Ana kategorileri yukler (lazy-loading icin ilk adim).
    /// Her ana kategori HasChildren=true olarak isaretlenir.
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetAsync("/api/v1/categories/main");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PttavmMainCategoryResponse>(json, JsonOptions);

            if (result is null || !result.Success)
            {
                var errorMsg = result?.Error?.ErrorMessage ?? "Bilinmeyen hata";
                Logger.LogError("PttAVM ana kategori yuklemesi basarisiz: {Error}", errorMsg);
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>([], errorMsg);
            }

            var categories = (result.MainCategory ?? []).Select(c => new ExternalCategoryDto
            {
                ExternalId = c.Id ?? "",
                Name = c.Name ?? "",
                HasChildren = true
            }).ToList();

            Logger.LogInformation("PttAVM {Count} ana kategori yuklendi", categories.Count);
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PttAVM ana kategori yuklenirken hata");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>([], ex.Message);
        }
    }

    /// <summary>
    /// Belirli bir kategorinin alt kategorilerini yukler (lazy-loading).
    /// UI'da dugum expand edildiginde cagirilir.
    /// </summary>
    public async Task<List<ExternalCategoryDto>> LoadChildrenAsync(
        string parentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetAsync($"/api/v1/categories/{parentId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PttavmCategoryDetailResponse>(json, JsonOptions);

            if (result?.Category?.Children is null)
                return [];

            return result.Category.Children.Select(c => new ExternalCategoryDto
            {
                ExternalId = c.Id ?? "",
                Name = c.Name ?? "",
                ParentExternalId = parentId,
                HasChildren = c.Children is { Count: > 0 }
            }).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PttAVM alt kategoriler yuklenirken hata (parentId={ParentId})", parentId);
            return [];
        }
    }

    /// <summary>
    /// Override: marketplace'i yukleyip base import'u cagir.
    /// Base class ImportCategoriesAsync LoadMarketPlaceAsync cagirmaz,
    /// bu yuzden PttAVM marketplace linklerinin olusturulmasi icin zorunlu.
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, "PttAVM", cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri basariyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri basariyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata olustu", Source);
            return new ErrorResult($"Import sirasinda hata: {ex.Message}");
        }
    }
}
