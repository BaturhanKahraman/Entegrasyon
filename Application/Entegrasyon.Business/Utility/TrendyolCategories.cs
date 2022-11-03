using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Entegrasyon.Business.Utility;

public class TrendyolCategories
{
    private readonly IntegrationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolCategories> _logger;
    private const string CategoryUrl = "https://api.trendyol.com/sapigw/product-categories";
    public TrendyolCategories(IntegrationDbContext dbContext,HttpClient httpClient,ILogger<TrendyolCategories> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
    }
    //parallel yapılacak
    public async Task AddCategories()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var marketPlace = new MarketPlace { Name = "Trendyol" };
        if(!await _dbContext.MarketPlaces.AnyAsync(x=>x.Name==marketPlace.Name))
            _dbContext.MarketPlaces.Add(marketPlace);
        else
        {
            marketPlace = await _dbContext.MarketPlaces.FindAsync(1);
        }
        HashSet<CategoryAttribute> AddedCategoryAttrs = new();
        HashSet<CategoryAttributeValue> AddedCategoryAttrValues = new();

        if(await _dbContext.Categories.CountAsync() < 5)
        {
            _httpClient.BaseAddress = new Uri(CategoryUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept","application/json");
            var result = await _httpClient.GetAsync("");
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<Rootobject>(content);
            if(categories is null)
                return;


            foreach(var subCategory in categories.categories)
            {
                var sysCat = new Entity.Categories.Category
                {
                    CreatedAt = DateTimeOffset.Now,
                    Name = subCategory.name,
                    CategoryAttributes = new List<CategoryAttribute>()
                };

                if(subCategory.subCategories != null)
                {
                    await AddAllCategory(sysCat,subCategory.subCategories);
                }
                await _dbContext.Categories.AddAsync(sysCat);
            }
            await _dbContext.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation($"Trendyol Kategorileri Eklendi. Süre: {stopwatch.ElapsedMilliseconds} ms");
        }

        async Task AddAllCategory(Entity.Categories.Category sysCat,IEnumerable<Category> children)
        {
            sysCat.SubCategories = new List<Entity.Categories.Category>();

            foreach(var category in children)
            {
                Debug.WriteLine("subcategory id : "+ category.id);

                var subCat = new Entity.Categories.Category
                {
                    CreatedAt = DateTimeOffset.Now,
                    Name = category.name,
                    CategoryAttributes = new List<CategoryAttribute>()
                };
                sysCat.SubCategories.Add(subCat);
                var match = new CategoryMarketPlaceMatch()
                    { ApplicationCategory = subCat, MarketPlace = marketPlace, MarketPlaceCategoryId = category.id };
                await _dbContext.CategoryMarketPlaceMatches.AddAsync(match);
                if(!category.subCategories.IsNullOrEmpty())
                    await AddAllCategory(subCat,category.subCategories);
                else
                {
                    var attrResult =await _httpClient.GetAsync(new Uri($"{CategoryUrl}/{category.id}/attributes"));
                    if (!attrResult.IsSuccessStatusCode)
                    {
                        int tryCount = 10;
                        while(tryCount > 0)
                        {
                            Debug.WriteLine("tekrar deneniyor : " + $"{CategoryUrl}/{category.id}/attributes");
                            await Task.Delay(500);
                            attrResult = await _httpClient.GetAsync(new Uri($"{CategoryUrl}/{category.id}/attributes"));
                            if(attrResult.IsSuccessStatusCode)
                                break;
                            tryCount--;
                        }

                        if (tryCount == 0)
                        {
                            Debug.WriteLine("Kategori Attribute Getirilemedi. Kategori Id: " + category.id + " " + attrResult.StatusCode);
                            continue;
                        }
                    }
                    var attrContent = await attrResult.Content.ReadAsStringAsync();
                    RootCategoryAttr trendyolCategoryAttrResult = JsonSerializer.Deserialize<RootCategoryAttr>(attrContent);
                    foreach (var trendyolCategoryAttr in trendyolCategoryAttrResult?.categoryAttributes!)
                    {
                        Debug.WriteLine("attr: " + trendyolCategoryAttr.attribute.name);
                        //eğer daha önce eklenmiş trendyol kategori attr varsa
                        if (AddedCategoryAttrs.Any(x => x.TempMappingId == trendyolCategoryAttr.attribute.id))
                        {
                            subCat.CategoryAttributes.Add(AddedCategoryAttrs.First(x => x.TempMappingId == trendyolCategoryAttr.attribute.id));
                        }
                        else
                        {
                            var categoryAttr = new CategoryAttribute
                            {
                                //Id = trendyolCategoryAttr.attribute.id,
                                TempMappingId = trendyolCategoryAttr.attribute.id,
                                AllowCustom = trendyolCategoryAttr.allowCustom,
                                CategoryAttributeKey = trendyolCategoryAttr.attribute.name,
                                Required = trendyolCategoryAttr.required,
                                Slicer = trendyolCategoryAttr.slicer,
                                Varianter = trendyolCategoryAttr.varianter,
                                CategoryAttributeValues = new List<CategoryAttributeValue>()
                            };
                            AddedCategoryAttrs.Add(categoryAttr);
                            await _dbContext.CategoryAttributes.AddAsync(categoryAttr);
                            var categoryAttrMatch = new CategoryAttributeMarketPlaceMatch
                            {
                                MarketPlace = marketPlace,
                                MarketPlaceCategoryAttributeId = trendyolCategoryAttr.attribute.id,
                                ApplicationCategoryAttribute = categoryAttr
                            };
                            await _dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(categoryAttrMatch);
                            if(!trendyolCategoryAttr.allowCustom)
                            {
                                if(trendyolCategoryAttr.attributeValues.Length >= 10_000)
                                {
                                    Debug.WriteLine("attr allow custom +10000: " + trendyolCategoryAttr.attribute.name + " attr value sayısı " + trendyolCategoryAttr.attributeValues.Length);

                                    var tempAddedCategoryConcurrentBag = new ConcurrentBag<CategoryAttributeValue>(AddedCategoryAttrValues); 
                                    var attrValueMatchConcurrentBag = new ConcurrentBag<CategoryAttributeValueMarketPlaceMatch>();
                                    var appAttrValues = new ConcurrentBag<CategoryAttributeValue>();
                                    Parallel.ForEach(trendyolCategoryAttr.attributeValues,trendyolAttrValue =>
                                    {
                                        if(tempAddedCategoryConcurrentBag.Any(x => x.TempMappingId == trendyolAttrValue.id))
                                        {
                                            categoryAttr.CategoryAttributeValues.Add(tempAddedCategoryConcurrentBag.First(x => x.TempMappingId == trendyolAttrValue.id));
                                        }
                                        else
                                        {
                                            var attrValue = new CategoryAttributeValue()
                                            {
                                                Name = trendyolAttrValue.name,
                                                TempMappingId = trendyolAttrValue.id
                                            };
                                            appAttrValues.Add(attrValue);
                                            var attrValueMatch = new CategoryAttributeValueMarketPlaceMatch
                                            {
                                                MarketPlace = marketPlace,
                                                MarketPlaceCategoryAttributeValueId = trendyolAttrValue.id,
                                                ApplicationCategoryAttributeValue = attrValue
                                            };
                                            attrValueMatchConcurrentBag.Add(attrValueMatch);
                                            tempAddedCategoryConcurrentBag.Add(attrValue);
                                            categoryAttr.CategoryAttributeValues.Add(attrValue);
                                        }

                                    });
                                    AddedCategoryAttrValues = tempAddedCategoryConcurrentBag.ToHashSet();
                                    await _dbContext.CategoryAttributeValues.AddRangeAsync(appAttrValues);
                                    await _dbContext.CategoryAttributeValueMarketPlaceMatches.AddRangeAsync(attrValueMatchConcurrentBag);
                                }
                                else
                                {
                                    foreach(var trendyolAttrValue in trendyolCategoryAttr.attributeValues)
                                    {
                                        Debug.WriteLine("attr allow custom: " + trendyolAttrValue.name + " attr value sayısı " + trendyolCategoryAttr.attributeValues.Length);

                                        if(AddedCategoryAttrValues.Any(x => x.TempMappingId == trendyolAttrValue.id))
                                        {
                                            categoryAttr.CategoryAttributeValues.Add(AddedCategoryAttrValues.First(x => x.TempMappingId == trendyolAttrValue.id));
                                        }
                                        else
                                        {
                                            var attrValue = new CategoryAttributeValue()
                                            {
                                                Name = trendyolAttrValue.name,
                                                TempMappingId = trendyolAttrValue.id
                                            };
                                            await _dbContext.CategoryAttributeValues.AddAsync(attrValue);
                                            var attrValueMatch = new CategoryAttributeValueMarketPlaceMatch
                                            {
                                                MarketPlace = marketPlace,
                                                MarketPlaceCategoryAttributeValueId = trendyolAttrValue.id,
                                                ApplicationCategoryAttributeValue = attrValue
                                            };
                                            await _dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(attrValueMatch);
                                            AddedCategoryAttrValues.Add(attrValue);
                                            categoryAttr.CategoryAttributeValues.Add(attrValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

public class RootCategoryAttr
{
    public int id { get; set; }
    public string name { get; set; }
    public string displayName { get; set; }
    public Categoryattribute[] categoryAttributes { get; set; }
}

public class Categoryattribute
{
    public bool allowCustom { get; set; }
    public Attribute attribute { get; set; }
    public Attributevalue[] attributeValues { get; set; }
    public int categoryId { get; set; }
    public bool required { get; set; }
    public bool varianter { get; set; }
    public bool slicer { get; set; }
}

public class Attribute
{
    public int id { get; set; }
    public string name { get; set; }
}

public class Attributevalue
{
    public int id { get; set; }
    public string name { get; set; }
}


public class Rootobject
{
    public Category[] categories { get; set; }
}

public class Category
{
    public int id { get; set; }
    public string name { get; set; }
    public object parentId { get; set; }
    public Category[] subCategories { get; set; }
}