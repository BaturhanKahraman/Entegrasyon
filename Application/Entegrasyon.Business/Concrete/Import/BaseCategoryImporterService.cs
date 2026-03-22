using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Kategori import islemleri icin temel sinif.
/// Ortak transaction, loglama ve veritabani islemlerini yonetir.
/// </summary>
public abstract class BaseCategoryImporterService : ICategoryImporterService
{
    protected readonly IDbContextFactory<IntegrationDbContext> ContextFactory;
    protected readonly ILogger Logger;
    protected MarketPlace? MarketPlace;

    public abstract ImportSource Source { get; }

    protected BaseCategoryImporterService(IDbContextFactory<IntegrationDbContext> contextFactory, ILogger logger)
    {
        ContextFactory = contextFactory;
        Logger = logger;
    }

    /// <summary>
    /// Marketplace'i yukler
    /// </summary>
    protected async Task LoadMarketPlaceAsync(IntegrationDbContext dbContext, string marketplaceName, CancellationToken cancellationToken = default)
    {
        MarketPlace = await dbContext.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Name == marketplaceName, cancellationToken);
    }

    /// <summary>
    /// Harici kategorileri ceker - her marketplace kendi implementasyonunu saglar
    /// </summary>
    public abstract Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toplu kategori import islemi
    /// </summary>
    public virtual async Task<IResult> ImportCategoriesAsync(IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
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

    /// <summary>
    /// Tekil kategori import islemi
    /// </summary>
    public virtual async Task<IResult> ImportCategoryAsync(ExternalCategoryImportRequest category, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SuccessResult($"Kategori '{category.Name}' basariyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Kategori import edilirken hata: {CategoryName}", category.Name);
            return new ErrorResult($"Import sirasinda hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Kategoriyi veritabanina ekler veya gunceller
    /// </summary>
    protected virtual async Task<Category> ImportCategoryInternalAsync(
        IntegrationDbContext dbContext,
        ExternalCategoryImportRequest importRequest,
        Category? parentCategory,
        CancellationToken cancellationToken = default)
    {
        // Mevcut kategoriyi bul veya yeni olustur
        var existingCategory = await dbContext.Categories
            .AsTracking()
            .Include(c => c.CategoryAttributes)
            .FirstOrDefaultAsync(c =>
                c.ImportSource == Source &&
                c.ExternalCategoryId == importRequest.ExternalId,
                cancellationToken);

        Category category;
        bool isNew = existingCategory == null;

        if (existingCategory != null)
        {
            // Guncelle
            existingCategory.Name = importRequest.Name;
            existingCategory.SuperCategory = parentCategory;
            existingCategory.UpdatedAt = DateTimeOffset.UtcNow;
            category = existingCategory;
        }
        else
        {
            // Yeni olustur
            category = new Category
            {
                Name = importRequest.Name,
                IsImported = true,
                ImportSource = Source,
                ExternalCategoryId = importRequest.ExternalId,
                SuperCategory = parentCategory,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Backward compatibility icin ImportId'yi de set et (Trendyol icin)
            if (Source == ImportSource.Trendyol && int.TryParse(importRequest.ExternalId, out var importId))
            {
                #pragma warning disable CS0618
                category.ImportId = importId;
                #pragma warning restore CS0618
            }

            await dbContext.Categories.AddAsync(category, cancellationToken);
        }

        // Marketplace eslesmesi olustur
        if (isNew && MarketPlace != null)
        {
            await CreateMarketplaceLinkAsync(dbContext, category, importRequest, cancellationToken);
        }

        // Alt kategorileri isle
        foreach (var childRequest in importRequest.Children)
        {
            await ImportCategoryInternalAsync(dbContext, childRequest, category, cancellationToken);
        }

        // Leaf kategori ise ozelliklerini cek
        if (importRequest.IsLeaf)
        {
            await ImportCategoryAttributesAsync(dbContext, category, isNew, cancellationToken);
        }

        return category;
    }

    /// <summary>
    /// Marketplace eslesmesi olusturur
    /// </summary>
    protected virtual async Task CreateMarketplaceLinkAsync(
        IntegrationDbContext dbContext,
        Category category,
        ExternalCategoryImportRequest importRequest,
        CancellationToken cancellationToken = default)
    {
        if (MarketPlace == null) return;

        var link = new CategoryMarketplace
        {
            Category = category,
            MarketPlace = MarketPlace,
            MarketPlaceCategoryId = int.TryParse(importRequest.ExternalId, out var id) ? id : 0,
            ExternalCategoryId = importRequest.ExternalId,
            MarketPlaceCategoryName = importRequest.Name,
            LastSyncedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.CategoryMarketplaces.AddAsync(link, cancellationToken);
    }

    /// <summary>
    /// Kategori ozelliklerini import eder - marketplace-specific
    /// </summary>
    protected virtual Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        // Alt siniflar override edebilir
        return Task.CompletedTask;
    }
}
