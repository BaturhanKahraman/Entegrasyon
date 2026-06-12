using System.Collections.Immutable;
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

namespace Entegrasyon.Business.Concrete.Trendyol.Import;

public class TrendyolCategoryImporterService:ITrendyolCategoryImportService
{
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly ILogger<TrendyolCategoryImporterService> _logger;
    private readonly List<CategoryAttribute> _savedCategoryAttributes = [];
    private const string CategoryUrlPostfix = @"product/product-categories";

    public TrendyolCategoryImporterService(IHttpClientFactory httpClientFactory,IDbContextFactory<IntegrationDbContext> contextFactory,ILogger<TrendyolCategoryImporterService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<IDataResult<IEnumerable<ImportedTrendyolCategory>>> GetTrendyolCategories()
    {
        var result = await _httpClient.GetFromJsonAsync<RootTrendyolCategory>(CategoryUrlPostfix);
        if (result == null)
        {
            _logger.LogCritical("Trendyol kategorilerini bos cekti");
            return new ErrorDataResult<IEnumerable<ImportedTrendyolCategory>>
                (null!, Messages.TrendyolCategoryApiError);
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
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var trendyolMarketPlace = await dbContext.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(x => string.Equals(x.Name, "Trendyol"));

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var trendyolSelectedCategory in rootCategories)
            {
                await AddDb(dbContext, trendyolMarketPlace, trendyolSelectedCategory, null!);
            }
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return new SuccessResult();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, "Trendol kategorileri eklenirken hata alindi.");
            return new ErrorResult("Hata alindi. " + e.Message);
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private async Task AddDb(IntegrationDbContext dbContext, MarketPlace? trendyolMarketPlace, TrendyolSelectedCategory importObj, Category superCategory)
    {
        var category = await dbContext
            .Categories
            .AsTracking()
            .Include(c => c.CategoryAttributes)
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == importObj.Id.ToString());
        bool isNew = false;
        if (category != null)
        {
            category.Name = importObj.Name;
            category.ExternalCategoryId = importObj.Id.ToString();
            category.IsImported = true;
            category.SuperCategory = superCategory;
        }
        else
        {
            category = new Category
            {
                Name = importObj.Name,
                IsImported = true,
                ExternalCategoryId = importObj.Id.ToString(),
                SuperCategory = superCategory
            };
            await AddTrendyolCategoryMatch(dbContext, trendyolMarketPlace, category);
            isNew = true;
            await dbContext.Categories.AddAsync(category);
        }

        if (!importObj.IsParent)
        {
            await AddAttributesForCategory(dbContext, trendyolMarketPlace, category, isNew);
            return;
        }

        foreach (var sc in importObj.SubCategories)
            await AddDb(dbContext, trendyolMarketPlace, sc, category);
    }

    private static async Task AddTrendyolCategoryMatch(IntegrationDbContext dbContext, MarketPlace? trendyolMarketPlace, Category category)
    {
        if (trendyolMarketPlace == null)
            return;
        var categoryMarketPlaceMatch = new CategoryMarketPlaceMatch
        {
            MarketPlace = trendyolMarketPlace,
            ApplicationCategory = category,
            MarketPlaceCategoryId = int.Parse(category.ExternalCategoryId!)
        };
        await dbContext.CategoryMarketPlaceMatches.AddAsync(categoryMarketPlaceMatch);
    }

    private async Task AddAttributesForCategory(IntegrationDbContext dbContext, MarketPlace? trendyolMarketPlace, Category category, bool newEntity)
    {
        string attrUrl = $"{CategoryUrlPostfix}/{category.ExternalCategoryId}/attributes";
        var trendyolCategory = await _httpClient.GetFromJsonAsync<TrendyolCategory>(attrUrl);
        if (trendyolCategory == null || !trendyolCategory.categoryAttributes.Any())
            return;
        if (newEntity)
        {
            List<CategoryAttributeCategory> categoryAttributes = new();

            foreach (var categoryAttribute in trendyolCategory.categoryAttributes)
            {
                var dbCatAttr = await AddAttributes(dbContext, trendyolMarketPlace, categoryAttribute);
                if (!await dbContext.CategoryAttributes.AnyAsync(x => x.ImportId == dbCatAttr.ImportId) && !_savedCategoryAttributes.Contains(dbCatAttr))
                {
                    foreach (var categoryAttributeAttributeValue in categoryAttribute.AttributeValues)
                    {
                        await AddAttrValues(dbContext, trendyolMarketPlace, categoryAttributeAttributeValue, dbCatAttr);
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
            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributes);
        }


        static async Task AddAttrValues(IntegrationDbContext dbContext, MarketPlace? marketPlace,
            TrendyolAttributeValue categoryAttributeAttributeValue,
            CategoryAttribute dbCatAttr)
        {
            var dbCatAttrValue = new CategoryAttributeValue { Name = categoryAttributeAttributeValue.Name };
            if (marketPlace != null)
            {
                var dbCatAttrValueMp = new CategoryAttributeValueMarketPlaceMatch
                {
                    MarketPlace = marketPlace,
                    ApplicationCategoryAttributeValue = dbCatAttrValue,
                    MarketPlaceCategoryAttributeValueId = categoryAttributeAttributeValue.Id
                };
                await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(dbCatAttrValueMp);
            }
            dbCatAttr.CategoryAttributeValues.Add(dbCatAttrValue);
        }

        static async Task<CategoryAttribute> AddAttributes(IntegrationDbContext dbContext, MarketPlace? marketPlace, TrendyolCategoryAttribute categoryAttribute)
        {
            var dbCatAttr = await dbContext.CategoryAttributes.FirstOrDefaultAsync(x =>
                x.ImportId == categoryAttribute.Attribute.Id) ?? new CategoryAttribute
                {
                    CategoryAttributeKey = categoryAttribute.Attribute.Name,
                    CategoryAttributeHumanized = categoryAttribute.Attribute.Name,
                    ImportId = categoryAttribute.Attribute.Id,
                    CategoryAttributeValues = new List<CategoryAttributeValue>()
                };
            if (!await dbContext.CategoryAttributes.AnyAsync(x =>
                    x.ImportId == categoryAttribute.Attribute.Id) && marketPlace != null)
            {
                var dbCatAttrMarketPlaceMatch = new CategoryAttributeMarketPlaceMatch()
                {
                    MarketPlace = marketPlace,
                    ApplicationCategoryAttribute = dbCatAttr,
                    MarketPlaceCategoryAttributeId = categoryAttribute.Attribute.Id
                };
                await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(dbCatAttrMarketPlaceMatch);
            }
            return dbCatAttr;
        }
    }
}
