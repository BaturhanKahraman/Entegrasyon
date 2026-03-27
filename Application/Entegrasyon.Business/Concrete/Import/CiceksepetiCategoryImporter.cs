using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Çiçeksepeti pazaryerinden kategori ve kategori özelliklerini import eder.
/// API recursive ağaç yapısında döner (SubCategories listeli).
/// Özellik tipleri: "Variant Ozellik" → IsVarianter, "Kisisellestirilebilir Ozellik" → AllowCustom.
/// </summary>
public class CiceksepetiCategoryImporter : BaseCategoryImporterService, ICiceksepetiCategoryImporter
{
    private readonly ICiceksepetiCategoryService _categoryService;
    private const string MarketplaceName = "Çiçeksepeti";

    // ── Attribute type constants (public for testability) ─────────────────────────
    public const string AttributeTypeVariant = "Variant Ozellik";
    public const string AttributeTypeProduct = "Urun Ozellik";
    public const string AttributeTypePersonalized = "Kisisellestirilebilir Ozellik";

    public override ImportSource Source => ImportSource.Ciceksepeti;

    public CiceksepetiCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        ICiceksepetiCategoryService categoryService,
        ILogger<CiceksepetiCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Çiçeksepeti API'sinden recursive kategori ağacını çeker ve ExternalCategoryDto listesine düzleştirir.
    /// API zaten ağaç döndürdüğünden recursive flatten ile Children korunarak döndürülür.
    /// Leaf kategoriler: SubCategories listesi boş olanlardır.
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _categoryService.GetCategoriesAsync(cancellationToken);
            if (!result.Success || result.Data is null)
            {
                Logger.LogError("Çiçeksepeti kategori listesi alınamadı: {Message}", result.Message);
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!,
                    result.Message ?? "Kategori listesi alınamadı");
            }

            var roots = result.Data.Categories
                .Select(MapToExternalCategory)
                .ToList();

            Logger.LogInformation(
                "Çiçeksepeti'nden {Count} kök kategori oluşturuldu",
                roots.Count);

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(roots);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Çiçeksepeti kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Import işleminden önce Çiçeksepeti marketplace kaydını yükler, ardından temel sınıfın
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

    /// <summary>
    /// Çiçeksepeti'ye özgü kategori özellik import işlemi.
    /// GET /api/v1/Categories/{id}/attributes üzerinden özellikler çekilir.
    /// Tip eşlemesi:
    ///   "Variant Ozellik"             → IsVarianter=true,  AllowCustom=false
    ///   "Urun Ozellik"                → IsVarianter=false, AllowCustom=false
    ///   "Kisisellestirilebilir Ozellik" → IsVarianter=false, AllowCustom=true
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;
        if (MarketPlace == null) return;

        if (!int.TryParse(category.ExternalCategoryId, out var categoryId))
        {
            Logger.LogWarning(
                "Çiçeksepeti kategori ExternalId sayıya çevrilemedi: {ExternalId}",
                category.ExternalCategoryId);
            return;
        }

        try
        {
            var attrResult = await _categoryService.GetCategoryAttributesAsync(categoryId, cancellationToken);
            if (!attrResult.Success || attrResult.Data is null)
            {
                Logger.LogWarning(
                    "Çiçeksepeti kategori özellikleri alınamadı: {CategoryId} — {Message}",
                    categoryId, attrResult.Message);
                return;
            }

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in attrResult.Data.CategoryAttributes)
            {
                var (isVarianter, allowCustom) = MapAttributeType(attr.Type);

                var dbCatAttr = await GetOrCreateAttributeAsync(
                    dbContext, attr, allowCustom, cancellationToken);

                foreach (var val in attr.AttributeValues)
                {
                    await AddAttributeValueAsync(dbContext, val, dbCatAttr, cancellationToken);
                }

                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.Required,
                    IsVarianter = isVarianter,
                    IsSlicer = false
                });
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(
                categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Çiçeksepeti kategori özellikleri import edilirken hata: {CategoryId}", categoryId);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Çiçeksepeti CiceksepetiCategoryDto'yu ExternalCategoryDto'ya recursive olarak dönüştürür.
    /// Leaf: SubCategories listesi boş olanlardır.
    /// </summary>
    private static ExternalCategoryDto MapToExternalCategory(CiceksepetiCategoryDto dto)
    {
        var children = dto.SubCategories
            .Select(MapToExternalCategory)
            .ToList();

        return new ExternalCategoryDto
        {
            ExternalId = dto.Id.ToString(),
            Name = dto.Name,
            ParentExternalId = dto.ParentCategoryId?.ToString(),
            HasChildren = children.Count > 0,
            Children = children
        };
    }

    /// <summary>
    /// Attribute tipine göre (IsVarianter, AllowCustom) flag'lerini döndürür.
    /// </summary>
    private static (bool IsVarianter, bool AllowCustom) MapAttributeType(string type) =>
        type switch
        {
            AttributeTypeVariant => (true, false),
            AttributeTypePersonalized => (false, true),
            _ => (false, false)   // AttributeTypeProduct ve bilinmeyen tipler
        };

    /// <summary>
    /// Çiçeksepeti AttributeId'sine göre mevcut attribute'u bulur ya da yeni oluşturur.
    /// MarketPlaceCategoryAttributeId ile eşleştirilir (int ID).
    /// </summary>
    private async Task<CategoryAttribute> GetOrCreateAttributeAsync(
        IntegrationDbContext dbContext,
        CiceksepetiAttributeDto attr,
        bool allowCustom,
        CancellationToken cancellationToken)
    {
        var attrIdString = attr.AttributeId.ToString();

        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(m =>
                m.MarketPlaceCategoryAttributeId == attr.AttributeId &&
                m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existingMatch != null)
            return existingMatch.ApplicationCategoryAttribute;

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attrIdString,
            CategoryAttributeHumanized = attr.AttributeName,
            AllowCustom = allowCustom,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var match = new CategoryAttributeMarketPlaceMatch
        {
            MarketPlace = MarketPlace!,
            ApplicationCategoryAttribute = newAttr,
            MarketPlaceCategoryAttributeId = attr.AttributeId,
            MarketPlaceCategoryAttributeExternalId = attrIdString
        };

        await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);
        return newAttr;
    }

    /// <summary>
    /// CategoryAttributeValue ve CategoryAttributeValueMarketPlaceMatch kaydını ekler.
    /// </summary>
    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        CiceksepetiAttributeValueDto val,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = val.Name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace != null)
        {
            var valueMatch = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = val.Id,
                MarketPlaceCategoryAttributeValueExternalId = val.Id.ToString()
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(valueMatch, cancellationToken);
        }
    }
}
