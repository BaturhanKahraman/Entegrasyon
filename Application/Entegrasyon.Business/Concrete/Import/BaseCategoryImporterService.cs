using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Results;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Kategori import işlemleri için temel sınıf.
/// Ortak transaction, loglama ve veritabanı işlemlerini yönetir.
/// </summary>
public abstract class BaseCategoryImporterService : ICategoryImporterService
{
    protected readonly IntegrationDbContext DbContext;
    protected readonly ILogger Logger;
    protected MarketPlace? MarketPlace;

    public abstract ImportSource Source { get; }

    protected BaseCategoryImporterService(IntegrationDbContext dbContext, ILogger logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    /// <summary>
    /// Marketplace'i yükler
    /// </summary>
    protected async Task LoadMarketPlaceAsync(string marketplaceName, CancellationToken cancellationToken = default)
    {
        MarketPlace = await DbContext.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Name == marketplaceName, cancellationToken);
    }

    /// <summary>
    /// Harici kategorileri çeker - her marketplace kendi implementasyonunu sağlar
    /// </summary>
    public abstract Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toplu kategori import işlemi
    /// </summary>
    public virtual async Task<IResult> ImportCategoriesAsync(IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(category, null, cancellationToken);
            }

            await DbContext.SaveChangesAsync(cancellationToken);
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

    /// <summary>
    /// Tekil kategori import işlemi
    /// </summary>
    public virtual async Task<IResult> ImportCategoryAsync(ExternalCategoryImportRequest category, CancellationToken cancellationToken = default)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ImportCategoryInternalAsync(category, null, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SuccessResult($"Kategori '{category.Name}' başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Kategori import edilirken hata: {CategoryName}", category.Name);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Kategoriyi veritabanına ekler veya günceller
    /// </summary>
    protected virtual async Task<Category> ImportCategoryInternalAsync(
        ExternalCategoryImportRequest importRequest,
        Category? parentCategory,
        CancellationToken cancellationToken = default)
    {
        // Mevcut kategoriyi bul veya yeni oluştur
        var existingCategory = await DbContext.Categories
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
            // Güncelle
            existingCategory.Name = importRequest.Name;
            existingCategory.SuperCategory = parentCategory;
            existingCategory.UpdatedAt = DateTimeOffset.UtcNow;
            category = existingCategory;
        }
        else
        {
            // Yeni oluştur
            category = new Category
            {
                Name = importRequest.Name,
                IsImported = true,
                ImportSource = Source,
                ExternalCategoryId = importRequest.ExternalId,
                SuperCategory = parentCategory,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Backward compatibility için ImportId'yi de set et (Trendyol için)
            if (Source == ImportSource.Trendyol && int.TryParse(importRequest.ExternalId, out var importId))
            {
                #pragma warning disable CS0618
                category.ImportId = importId;
                #pragma warning restore CS0618
            }

            await DbContext.Categories.AddAsync(category, cancellationToken);
        }

        // Marketplace eşleşmesi oluştur
        if (isNew && MarketPlace != null)
        {
            await CreateMarketplaceLinkAsync(category, importRequest, cancellationToken);
        }

        // Alt kategorileri işle
        foreach (var childRequest in importRequest.Children)
        {
            await ImportCategoryInternalAsync(childRequest, category, cancellationToken);
        }

        // Leaf kategori ise özelliklerini çek
        if (importRequest.IsLeaf)
        {
            await ImportCategoryAttributesAsync(category, isNew, cancellationToken);
        }

        return category;
    }

    /// <summary>
    /// Marketplace eşleşmesi oluşturur
    /// </summary>
    protected virtual async Task CreateMarketplaceLinkAsync(
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

        await DbContext.CategoryMarketplaces.AddAsync(link, cancellationToken);
    }

    /// <summary>
    /// Kategori özelliklerini import eder - marketplace-specific
    /// </summary>
    protected virtual Task ImportCategoryAttributesAsync(
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        // Alt sınıflar override edebilir
        return Task.CompletedTask;
    }
}
