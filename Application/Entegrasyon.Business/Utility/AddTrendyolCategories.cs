using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

namespace Entegrasyon.Business.Utility;

public class AddTrendyolCategories
{
    private readonly IntegrationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private const string CategoryUrl = "https://api.trendyol.com/sapigw/product-categories";
    private const string BrandCategoryUrl = "https://api.trendyol.com/sapigw/brands";
    private const string CargoCompanyUrl=""

    public AddTrendyolCategories(IntegrationDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    public async Task AddCategories()
    {
        
    }
}