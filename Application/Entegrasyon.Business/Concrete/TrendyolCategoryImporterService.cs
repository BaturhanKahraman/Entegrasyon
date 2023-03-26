using System.Collections.Immutable;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class TrendyolCategoryImporterService
{
    private readonly HttpClient _httpClient;
    private readonly IntegrationDbContext _dbContext;
    private readonly RabbitMqPublisherService _brokerHelper;
    private readonly ILogger<TrendyolCategoryImporterService> _logger;
    private readonly List<CategoryAttribute> SavedCategoryAttributes = new();

    private const string CategoryUrl = @"https://api.trendyol.com/sapigw/product-categories";
    public TrendyolCategoryImporterService(HttpClient httpClient,RabbitMqPublisherService brokerHelper,IntegrationDbContext dbContext,ILogger<TrendyolCategoryImporterService> logger)
    {
        _httpClient = httpClient;
        _brokerHelper = brokerHelper;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IDataResult<IEnumerable<ImportedTrendyolCategory>>> GetTrendyolCategories()
    {
        var result = await _httpClient.GetAsync(CategoryUrl);
        if(result.IsSuccessStatusCode)
        {
            var body = await result.Content.ReadAsStringAsync();
            var categories = JsonConvert.DeserializeObject<RootTrendyolCategory>(body);
            return new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories!.Categories);
        }
        return new ErrorDataResult<IEnumerable<ImportedTrendyolCategory>>(null);
    }

    public IResult QueueImportingTrendyolCategories(IEnumerable<TrendyolImport> trendyolImports)
    {
        //validate
        if(trendyolImports == null)
            return new ErrorResult("Boş obje gönderildi.");
        _brokerHelper.PublishToQueue(MessageBrokerNames.TrendyolCategoryImportQueueName,trendyolImports);
        return new SuccessResult(Messages.CategoryImportQueued);
    }
    public async Task<IResult> Import(List<TrendyolImport> trendyolImports)
    {
        var flat = trendyolImports.SelectMany(x => x.SelectedCategories).ToList();
        var lookup = flat.ToLookup(f => f.ParentId);

        foreach(var import in flat)
        {
            import.SubCategories = lookup[import.Id].ToList();
        }

        var rootcategories = lookup[null].ToImmutableList();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            //gelen kategoriler varolan ve olmayan olarak ayrılacak//eklerken bakılabilir
            //varolan kategorilerin önceki halleri güncellenecek
            //varolmayan kategoriler için yeni kategori oluşturulup veritabanına eklenecek.
            foreach(var trendyolSelectedCategory in rootcategories)
            {
                await AddDb(trendyolSelectedCategory,null);
            }
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return new SuccessResult();
        }
        catch(Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e,"Trendol kategorileri eklenirken hata alındı.");
            return new ErrorResult("Hata alındı. " + e.Message);
        }
        finally
        {
            await transaction.DisposeAsync();
        }

    }

    private async Task AddDb(TrendyolSelectedCategory importObj,Category? superCategory)
    {
        var category = await _dbContext.Categories.Include(c => c.CategoryAttributes).FirstOrDefaultAsync(c => c.ImportId.Value == importObj.Id);
        bool isNew = false;
        if(category != null)
        {
            category.Name = importObj.Name;
            category.ImportId = importObj.Id;
            category.IsImported = true;
            category.SuperCategory = superCategory;
            _dbContext.Categories.Update(category);
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

        if(!importObj.IsParent)
        {
            await AddAttributesForCategory(category,isNew);
            return;
        }

        //await Task.WhenAll(importObj.SubCategories.Select(sc => AddDb(sc, category)));
        //Task.WaitAll(importObj.SubCategories.Select(sc => AddDb(sc, category)).ToArray());
        foreach(var trendyolSelectedCategory in importObj.SubCategories)
        {
            await AddDb(trendyolSelectedCategory,category);
        }
        //importObj.SubCategories.ForEach(async sc => await AddDb(sc,category));
    }

    private async Task AddTrendyolCategoryMatch(Category category)
    {
        var marketPlace = await _dbContext.MarketPlaces.FirstOrDefaultAsync(x => string.Equals(x.Name,"Trendyol"));
        if(marketPlace == null)
            return;
        var categoryMarketPlaceMatch = new CategoryMarketPlaceMatch
        {
            MarketPlace = marketPlace,
            ApplicationCategory = category,
            MarketPlaceCategoryId = category.ImportId!.Value
        };
        await _dbContext.CategoryMarketPlaceMatches.AddAsync(categoryMarketPlaceMatch);
    }

    private async Task AddAttributesForCategory(Category category,bool newEntity)
    {

        string attrUrl = $"{CategoryUrl}/{category.ImportId}/attributes";
        var result = await _httpClient.GetAsync(attrUrl);
        await Task.Delay(1000);
        if(result.IsSuccessStatusCode)
        {
            var body = await result.Content.ReadAsStringAsync();
            var trendyolCategory = JsonConvert.DeserializeObject<TrendyolCategory>(body);
            if(trendyolCategory == null || !trendyolCategory.categoryAttributes.Any())
                return;
            if(newEntity)
            {
                var marketPlace = await _dbContext.MarketPlaces.FirstOrDefaultAsync(x => string.Equals(x.Name,"Trendyol"));
                List<CategoryAttributeCategory> categoryAttributes = new();

                foreach(var categoryAttribute in trendyolCategory.categoryAttributes)
                {
                    var dbCatAttr = await AddAttributes(categoryAttribute,marketPlace);
                    if(!await _dbContext.CategoryAttributes.AnyAsync(x => x.ImportId == dbCatAttr.ImportId) && !SavedCategoryAttributes.Contains(dbCatAttr))
                    {
                        foreach(var categoryAttributeAttributeValue in categoryAttribute.AttributeValues)
                        {
                            await AddAttrValues(categoryAttributeAttributeValue,marketPlace,dbCatAttr);
                        }
                    }
                    if(!SavedCategoryAttributes.Contains(dbCatAttr))
                        SavedCategoryAttributes.Add(dbCatAttr);

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

        }

        async Task AddAttrValues(TrendyolAttributeValue categoryAttributeAttributeValue,MarketPlace marketPlace,
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

        async Task<CategoryAttribute> AddAttributes(TrendyolCategoryAttribute categoryAttribute,MarketPlace marketPlace)
        {
            var dbCatAttr = (await _dbContext.CategoryAttributes.FirstOrDefaultAsync(x =>
                x.ImportId == categoryAttribute.Attribute.Id) ?? SavedCategoryAttributes.FirstOrDefault(x =>
                x.ImportId == categoryAttribute.Attribute.Id)) ?? new CategoryAttribute
                {
                    CategoryAttributeKey = categoryAttribute.Attribute.Name,
                    CategoryAttributeHumanized = categoryAttribute.Attribute.Name,
                    ImportId = categoryAttribute.Attribute.Id,
                    AllowCustom = categoryAttribute.AllowCustom,
                    CategoryAttributeValues = new List<CategoryAttributeValue>()
                };
            if(!await _dbContext.CategoryAttributes.AnyAsync(x =>
                    x.ImportId == categoryAttribute.Attribute.Id) && !SavedCategoryAttributes.Contains(dbCatAttr))
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