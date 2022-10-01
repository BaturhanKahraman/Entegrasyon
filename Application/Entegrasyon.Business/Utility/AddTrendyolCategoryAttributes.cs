using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Entegrasyon.Business.Utility;

public class AddTrendyolCategoryAttributes
{
    private readonly IntegrationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AddTrendyolCategoryAttributes> _logger;
    private const string CategoryUrl = "https://api.trendyol.com/sapigw/product-categories";
    private const string CategoryAttributeUrl = "https://api.trendyol.com/sapigw/product-categories/{0}/attributes";
    private const string BrandCategoryUrl = "https://api.trendyol.com/sapigw/brands";
    private const string CargoCompanyUrl = "";

    public AddTrendyolCategoryAttributes(IntegrationDbContext dbContext,HttpClient httpClient,ILogger<AddTrendyolCategoryAttributes> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task AddCategoryAttributes()
    {

        if(await _dbContext.CategoryAttributes.CountAsync() < 5)
        {
            
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
                    Name = subCategory.name
                };
                if(subCategory.subCategories != null)
                    AddAllCategory(sysCat,subCategory.subCategories);
                await _dbContext.Categories.AddAsync(sysCat);
            }
            await _dbContext.SaveChangesAsync();
        }

        void AddAllCategory(Entity.Categories.Category sysCat,IEnumerable<Category> children)
        {
            sysCat.SubCategories = new List<Entity.Categories.Category>();
            foreach(var category in children)
            {
                var subCat = new Entity.Categories.Category
                {
                    CreatedAt = DateTimeOffset.Now,
                    Name = category.name
                };
                sysCat.SubCategories.Add(subCat);
                AddAllCategory(subCat,category.subCategories);
            }
        }
    }
}