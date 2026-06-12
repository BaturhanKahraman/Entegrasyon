using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Trendyol'dan kategori import islemleri.
/// BaseCategoryImporterService'den turer ve Trendyol-specific islemleri yapar.
/// </summary>
public class TrendyolCategoryImporter : BaseCategoryImporterService
{
    private readonly HttpClient _httpClient;
    private readonly List<CategoryAttribute> _savedCategoryAttributes = new();
    private const string CategoryUrlPostfix = "product/product-categories";
    private const string MarketplaceName = "Trendyol";

    public override ImportSource Source => ImportSource.Trendyol;

    public TrendyolCategoryImporter(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<IntegrationDbContext> contextFactory,
        ILogger<TrendyolCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
    }

    /// <summary>
    /// Trendyol API'sinden kategorileri ceker
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<RootTrendyolCategory>(CategoryUrlPostfix, cancellationToken);

            if (result?.Categories == null)
            {
                Logger.LogCritical("Trendyol kategorileri bos dondu");
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, Messages.TrendyolCategoryApiError);
            }

            var categories = result.Categories.Select(MapToExternalCategory).ToList();
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Trendyol kategorileri cekilirken hata olustu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Trendyol kategori modelini genel modele cevirir
    /// </summary>
    private ExternalCategoryDto MapToExternalCategory(ImportedTrendyolCategory trendyolCategory)
    {
        return new ExternalCategoryDto
        {
            ExternalId = trendyolCategory.Id.ToString(),
            Name = trendyolCategory.Name,
            ParentExternalId = trendyolCategory.ParentId?.ToString(),
            HasChildren = trendyolCategory.SubCategories?.Any() == true,
            Children = trendyolCategory.SubCategories?.Select(MapToExternalCategory).ToList() ?? new List<ExternalCategoryDto>()
        };
    }

    /// <summary>
    /// Import işleminden once marketplace'i yukler
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);
        _savedCategoryAttributes.Clear();

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
    /// Trendyol'a ozel kategori ozellikleri import işlemi
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;

        try
        {
            string attrUrl = $"{CategoryUrlPostfix}/{category.ExternalCategoryId}/attributes";
            var trendyolCategory = await _httpClient.GetFromJsonAsync<TrendyolCategory>(attrUrl, cancellationToken);

            if (trendyolCategory?.categoryAttributes == null || !trendyolCategory.categoryAttributes.Any())
                return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in trendyolCategory.categoryAttributes)
            {
                var dbCatAttr = await GetOrCreateCategoryAttributeAsync(dbContext, attr, cancellationToken);

                // Attribute degerlerini ekle
                if (!_savedCategoryAttributes.Any(x => x.ImportId == attr.Attribute.Id))
                {
                    foreach (var attrValue in attr.AttributeValues)
                    {
                        await AddAttributeValueAsync(dbContext, attrValue, dbCatAttr, cancellationToken);
                    }
                    _savedCategoryAttributes.Add(dbCatAttr);
                }

                // Kategori-Attribute iliskisi
                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.Required,
                    IsSlicer = attr.Slicer,
                    IsVarianter = attr.Varianter
                });
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Kategori ozellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }

    /// <summary>
    /// Kategori attribute'unu bulur veya olusturur
    /// </summary>
    private async Task<CategoryAttribute> GetOrCreateCategoryAttributeAsync(
        IntegrationDbContext dbContext,
        TrendyolCategoryAttribute attr,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CategoryAttributes
            .FirstOrDefaultAsync(x => x.ImportId == attr.Attribute.Id, cancellationToken);

        if (existing != null)
            return existing;

        var cached = _savedCategoryAttributes.FirstOrDefault(x => x.ImportId == attr.Attribute.Id);
        if (cached != null)
            return cached;

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.Attribute.Name,
            CategoryAttributeHumanized = attr.Attribute.Name,
            ImportId = attr.Attribute.Id,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Marketplace Eşleşmesi
        if (MarketPlace != null)
        {
            var match = new CategoryAttributeMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttribute = newAttr,
                MarketPlaceCategoryAttributeId = attr.Attribute.Id
            };
            await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);
        }

        return newAttr;
    }

    /// <summary>
    /// Attribute degeri ekler
    /// </summary>
    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        TrendyolAttributeValue attrValue,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = attrValue.Name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace != null)
        {
            var match = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = attrValue.Id
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(match, cancellationToken);
        }
    }
}
