using System.Collections.Immutable;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Matches;
using Entegrasyon.MessageQueue.Commands.Trendyol.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Results;

namespace Entegrasyon.Business.Concrete.Trendyol.Import;

public class TrendyolCategoryImporterService:ITrendyolCategoryImportService
{
    private readonly HttpClient _httpClient;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<TrendyolCategoryImporterService> _logger;
    private readonly List<CategoryAttribute> _savedCategoryAttributes = [];
    private readonly MarketPlace _trendyolMarketPlace;
    private const string CategoryUrlPostfix = @"product/product-categories";

    public TrendyolCategoryImporterService(IHttpClientFactory httpClientFactory,IntegrationDbContext dbContext,ILogger<TrendyolCategoryImporterService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
        _dbContext = dbContext;
        _logger = logger;
        _trendyolMarketPlace = _dbContext
            .MarketPlaces
            .AsTracking()
            .FirstOrDefault(x => string.Equals(x.Name,"Trendyol"));
    }

    public async Task<IDataResult<IEnumerable<ImportedTrendyolCategory>>> GetTrendyolCategories()
    {
        var result = await _httpClient.GetFromJsonAsync<RootTrendyolCategory>(CategoryUrlPostfix);
        if (result == null)
        {
            _logger.LogCritical("Trendyol kategorilerini boş çekti");
            return new ErrorDataResult<IEnumerable<ImportedTrendyolCategory>>
                (null, Messages.TrendyolCategoryApiError);
        }
        return new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(result.Categories);
    }

    public async ValueTask QueueImporting(ImmutableList<TrendyolSelectedCategory> rootCategories)
    {
        if (rootCategories is null || rootCategories.Count == 0)
            return;
        // Direct import since we're not using RabbitMQ anymore
        await Import(rootCategories);
    }

    public async Task<IResult> Import(ImmutableList<TrendyolSelectedCategory> rootCategories)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Tüm import edilecek kategorilerin ID'lerini topla
            var allCategoryIds = new HashSet<long>();
            CollectCategoryIds(rootCategories, allCategoryIds);

            // Var olan kategorileri tek sorguda çek
            var existingCategories = await _dbContext.Categories
                .AsTracking()
                .Include(c => c.CategoryAttributes)
                .Where(c => c.ImportId.HasValue && allCategoryIds.Contains((long)c.ImportId.Value))
                .ToDictionaryAsync(c => (long)c.ImportId!.Value, c => c);

            var leafCategories = new List<Category>();

            foreach (var trendyolSelectedCategory in rootCategories)
            {
                await AddDb(trendyolSelectedCategory, null, leafCategories, existingCategories);
            }

            await _dbContext.SaveChangesAsync();

            // Attributes'ları sırayla çek (paralel DbContext sorununu önlemek için)
            foreach (var category in leafCategories)
            {
                await AddAttributesForCategory(category, true);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return new SuccessResult();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, "Trendol kategorileri eklenirken hata alındı.");
            return new ErrorResult("Hata alındı. " + e.Message);
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private void CollectCategoryIds(IReadOnlyCollection<TrendyolSelectedCategory> categories, HashSet<long> ids)
    {
        foreach (var category in categories)
        {
            ids.Add(category.Id);
            if (category.SubCategories.Any())
            {
                CollectCategoryIds(category.SubCategories, ids);
            }
        }
    }

    private async Task AddDb(TrendyolSelectedCategory importObj, Category superCategory, List<Category> leafCategories, Dictionary<long, Category> existingCategories)
    {
        if (!existingCategories.TryGetValue(importObj.Id, out var category))
        {
            category = new Category
            {
                Name = importObj.Name,
                IsImported = true,
                ImportId = importObj.Id,
                SuperCategory = superCategory
            };
            await AddTrendyolCategoryMatch(category);
            await _dbContext.Categories.AddAsync(category);
            existingCategories[importObj.Id] = category; // Cache'e ekle
        }
        else
        {
            // Var olan kategoriyi güncelle
            category.Name = importObj.Name;
            category.ImportId = importObj.Id;
            category.IsImported = true;
            category.SuperCategory = superCategory;
        }

        if (!importObj.IsParent)
        {
            leafCategories.Add(category);
            return;
        }

        foreach (var sc in importObj.SubCategories)
            await AddDb(sc, category, leafCategories, existingCategories);
    }

    private async Task AddTrendyolCategoryMatch(Category category)
    {
        if (_trendyolMarketPlace == null)
            return;
        var categoryMarketPlaceMatch = new CategoryMarketPlaceMatch
        {
            MarketPlace = _trendyolMarketPlace,
            ApplicationCategory = category,
            MarketPlaceCategoryId = category.ImportId!.Value
        };
        await _dbContext.CategoryMarketPlaceMatches.AddAsync(categoryMarketPlaceMatch);
    }

    private async Task AddAttributesForCategory(Category category, bool newEntity)
    {
        string attrUrl = $"{CategoryUrlPostfix}/{category.ImportId}/attributes";
        var trendyolCategory = await _httpClient.GetFromJsonAsync<TrendyolCategory>(attrUrl);
        if (trendyolCategory == null || !trendyolCategory.categoryAttributes.Any())
            return;

        // Tüm attribute ID'lerini topla
        var attributeIds = trendyolCategory.categoryAttributes.Select(ca => (long)ca.Attribute.Id).ToHashSet();

        // Var olan attributes'ları tek sorguda çek
        var existingAttributes = await _dbContext.CategoryAttributes
            .AsTracking()
            .Where(ca => attributeIds.Contains((long)ca.ImportId))
            .ToDictionaryAsync(ca => (long)ca.ImportId, ca => ca);

        List<CategoryAttributeCategory> categoryAttributes = new();

        foreach (var categoryAttribute in trendyolCategory.categoryAttributes)
        {
            var dbCatAttr = await AddAttributes(categoryAttribute, _trendyolMarketPlace, existingAttributes);
            if (!await _dbContext.CategoryAttributes.AnyAsync(x => x.ImportId == dbCatAttr.ImportId) && !_savedCategoryAttributes.Contains(dbCatAttr))
            {
                foreach (var categoryAttributeAttributeValue in categoryAttribute.AttributeValues)
                {
                    await AddAttrValues(categoryAttributeAttributeValue, _trendyolMarketPlace, dbCatAttr);
                }
            }
            if (!_savedCategoryAttributes.Contains(dbCatAttr))
                _savedCategoryAttributes.Add(dbCatAttr);

            var manyToManyEntity = new CategoryAttributeCategory
            {
                Category = category,
                CategoryAttribute = dbCatAttr,
                IsRequired = categoryAttribute.Required,
                IsSlicer = categoryAttribute.Slicer,
                IsVarianter = categoryAttribute.Varianter
            };
            categoryAttributes.Add(manyToManyEntity);
        }
        await _dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributes);


        async Task AddAttrValues(TrendyolAttributeValue categoryAttributeAttributeValue, MarketPlace marketPlace,
            CategoryAttribute dbCatAttr)
        {
            var dbCatAttrValue = new CategoryAttributeValue { Name = categoryAttributeAttributeValue.Name };
            var dbCatAttrValueMp = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = marketPlace,
                ApplicationCategoryAttributeValue = dbCatAttrValue,
                MarketPlaceCategoryAttributeValueId = categoryAttributeAttributeValue.Id
            };
            dbCatAttr.CategoryAttributeValues.Add(dbCatAttrValue);
            await _dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(dbCatAttrValueMp);
        }

        async Task<CategoryAttribute> AddAttributes(TrendyolCategoryAttribute categoryAttribute, MarketPlace marketPlace, Dictionary<long, CategoryAttribute> existingAttrs)
        {
            if (!existingAttrs.TryGetValue(categoryAttribute.Attribute.Id, out var dbCatAttr))
            {
                dbCatAttr = _savedCategoryAttributes.FirstOrDefault(x => x.ImportId == (int)categoryAttribute.Attribute.Id) ?? new CategoryAttribute
                {
                    CategoryAttributeKey = categoryAttribute.Attribute.Name,
                    CategoryAttributeHumanized = categoryAttribute.Attribute.Name,
                    ImportId = (int)categoryAttribute.Attribute.Id,
                    AllowCustom = categoryAttribute.AllowCustom,
                    CategoryAttributeValues = new List<CategoryAttributeValue>()
                };
                existingAttrs[categoryAttribute.Attribute.Id] = dbCatAttr;
            }

            if (!await _dbContext.CategoryAttributes.AnyAsync(x =>
                    x.ImportId == (int)categoryAttribute.Attribute.Id) && !_savedCategoryAttributes.Contains(dbCatAttr))
            {
                var dbCatAttrMarketPlaceMatch = new CategoryAttributeMarketPlaceMatch()
                {
                    MarketPlace = marketPlace,
                    ApplicationCategoryAttribute = dbCatAttr,
                    MarketPlaceCategoryAttributeId = (int)categoryAttribute.Attribute.Id
                };
                await _dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(dbCatAttrMarketPlaceMatch);
            }
            return dbCatAttr;
        }
    }
}
