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
/// Trendyol'dan kategori import işlemleri.
/// BaseCategoryImporterService'den türer ve Trendyol-specific işlemleri yapar.
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
        IntegrationDbContext dbContext,
        ILogger<TrendyolCategoryImporter> logger)
        : base(dbContext, logger)
    {
        _httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
    }

    /// <summary>
    /// Trendyol API'sinden kategorileri çeker
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<RootTrendyolCategory>(CategoryUrlPostfix, cancellationToken);

            if (result?.Categories == null)
            {
                Logger.LogCritical("Trendyol kategorileri boş döndü");
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null, Messages.TrendyolCategoryApiError);
            }

            var categories = result.Categories.Select(MapToExternalCategory).ToList();
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Trendyol kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Trendyol kategori modelini genel modele çevirir
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
    /// Import işleminden önce marketplace'i yükler
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await LoadMarketPlaceAsync(MarketplaceName, cancellationToken);
        _savedCategoryAttributes.Clear();
        return await base.ImportCategoriesAsync(categories, cancellationToken);
    }

    /// <summary>
    /// Trendyol'a özel kategori özellikleri import işlemi
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
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
                var dbCatAttr = await GetOrCreateCategoryAttributeAsync(attr, cancellationToken);

                // Attribute değerlerini ekle
                if (!_savedCategoryAttributes.Any(x => x.ImportId == attr.Attribute.Id))
                {
                    foreach (var attrValue in attr.AttributeValues)
                    {
                        await AddAttributeValueAsync(attrValue, dbCatAttr, cancellationToken);
                    }
                    _savedCategoryAttributes.Add(dbCatAttr);
                }

                // Kategori-Attribute ilişkisi
                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.Required,
                    IsSlicer = attr.Slicer,
                    IsVarianter = attr.Varianter
                });
            }

            await DbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Kategori özellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }

    /// <summary>
    /// Kategori attribute'unu bulur veya oluşturur
    /// </summary>
    private async Task<CategoryAttribute> GetOrCreateCategoryAttributeAsync(
        TrendyolCategoryAttribute attr,
        CancellationToken cancellationToken)
    {
        var existing = await DbContext.CategoryAttributes
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
            AllowCustom = attr.AllowCustom,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Marketplace eşleşmesi
        if (MarketPlace != null)
        {
            var match = new CategoryAttributeMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttribute = newAttr,
                MarketPlaceCategoryAttributeId = attr.Attribute.Id
            };
            await DbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);
        }

        return newAttr;
    }

    /// <summary>
    /// Attribute değeri ekler
    /// </summary>
    private async Task AddAttributeValueAsync(
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
            await DbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(match, cancellationToken);
        }
    }
}
