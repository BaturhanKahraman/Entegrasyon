using System.ComponentModel;
using System.Text.Json;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Utility;

public class TrendyolBrands
{
    private readonly IntegrationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private const string BrandCategoryUrl = "https://api.trendyol.com/sapigw/brands";

    public TrendyolBrands(IntegrationDbContext dbContext,HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }


    public async Task AddBrandsFromTrendyol()
    {
        if(await _dbContext.Brands.AnyAsync())
            return;
        var marketPlace = new MarketPlace { Name = "Trendyol" };
        if(!await _dbContext.MarketPlaces.AnyAsync(x => x.Name == marketPlace.Name))
            _dbContext.MarketPlaces.Add(marketPlace);
        else
        {
            marketPlace = await _dbContext.MarketPlaces.FindAsync(1);
        }
        _httpClient.BaseAddress = new Uri(BrandCategoryUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept","application/json");
        var result = await _httpClient.GetStringAsync("");
        var brands = JsonSerializer.Deserialize<BrandRootobject>(result);
        foreach (var brand in brands?.brands!)
        {
            if (char.IsNumber(brand.name[0]) && char.IsNumber(brand.name[1]))
                continue;
            var entityBrand = new Entity.Products.Brand()
            {
                Name = brand.name,CreatedAt = DateTimeOffset.Now
            };
            await _dbContext.Brands.AddAsync(entityBrand);
            await _dbContext.BrandMarketPlaceMatches.AddAsync(new BrandMarketPlaceMatch
            {
                MarketPlaceBrandId = brand.id,
                ApplicationBrand = entityBrand,
                MarketPlace = marketPlace
            });
        }
        await _dbContext.SaveChangesAsync();
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]

public class BrandRootobject
{
    public Brand[] brands { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public class Brand
{
    public int id { get; set; }
    public string name { get; set; }
}
