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
            foreach (var trendyolSelectedCategory in rootCategories)
            {
                await AddDb(trendyolSelectedCategory, null);
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

    private async Task AddDb(TrendyolSelectedCategory importObj, Category superCategory)
    {
        var category = await _dbContext
            .Categories
            .AsTracking()
            .Include(c => c.CategoryAttributes)
            .FirstOrDefaultAsync(c => c.ImportId.Value == importObj.Id); //toplu çekilebilir
        bool isNew = false;
        if (category != null)
        {
            category.Name = importObj.Name;
            category.ImportId = importObj.Id;
            category.IsImported = true;
            category.SuperCategory = superCategory;
        }
        else
        {
            category = new Category
            {
                Name = importObj.Name,
                IsImported = true,
                ImportId = importObj.Id,
                SuperCategory = superCategory
            };
            await AddTrendyolCategoryMatch(category);
            isNew = true;
            await _dbContext.Categories.AddAsync(category);
        }

        if (!importObj.IsParent)
        {
            await AddAttributesForCategory(category, isNew);
            return;
        }

        foreach (var sc in importObj.SubCategories)
            await AddDb(sc, category);
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
        if (newEntity)
        {
            List<CategoryAttributeCategory> categoryAttributes = new();

            foreach (var categoryAttribute in trendyolCategory.categoryAttributes)
            {
                var dbCatAttr = await AddAttributes(categoryAttribute, _trendyolMarketPlace);
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
        }


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

        async Task<CategoryAttribute> AddAttributes(TrendyolCategoryAttribute categoryAttribute, MarketPlace marketPlace)
        {
            var dbCatAttr = (await _dbContext.CategoryAttributes.FirstOrDefaultAsync(x =>
                x.ImportId == categoryAttribute.Attribute.Id) ?? _savedCategoryAttributes.FirstOrDefault(x =>
                x.ImportId == categoryAttribute.Attribute.Id)) ?? new CategoryAttribute
                {
                    CategoryAttributeKey = categoryAttribute.Attribute.Name,
                    CategoryAttributeHumanized = categoryAttribute.Attribute.Name,
                    ImportId = categoryAttribute.Attribute.Id,
                    AllowCustom = categoryAttribute.AllowCustom,
                    CategoryAttributeValues = new List<CategoryAttributeValue>()
                };
            if (!await _dbContext.CategoryAttributes.AnyAsync(x =>
                    x.ImportId == categoryAttribute.Attribute.Id) && !_savedCategoryAttributes.Contains(dbCatAttr))
            {
                var dbCatAttrMarketPlaceMatch = new CategoryAttributeMarketPlaceMatch()
                {
                    MarketPlace = marketPlace,
                    ApplicationCategoryAttribute = dbCatAttr,
                    MarketPlaceCategoryAttributeId = categoryAttribute.Attribute.Id
                };
                await _dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(dbCatAttrMarketPlaceMatch);
            }
            return dbCatAttr;
        }
    }
}