using System.Text.Json;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Utility;

public class TrendyolCargoCompanies
{
    private readonly HttpClient _httpClient;
    private const string CargoUrl = "https://api.trendyol.com/sapigw/shipment-providers";
    private readonly IntegrationDbContext _dbContext;
    public TrendyolCargoCompanies(HttpClient httpClient, IntegrationDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
    }

    public async Task AddCargoCompanies()
    {
        if (await _dbContext.CargoCompanies.AnyAsync())
            return;
        var marketPlace = new MarketPlace { Name = "Trendyol" };
        if(!await _dbContext.MarketPlaces.AnyAsync(x => x.Name == marketPlace.Name))
            _dbContext.MarketPlaces.Add(marketPlace);
        else
        {
            marketPlace = await _dbContext.MarketPlaces.FindAsync(1);
        }
        _httpClient.BaseAddress = new Uri(CargoUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept","application/json");
        var result = await _httpClient.GetAsync("");
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();
        var cargoCompanies = JsonSerializer.Deserialize<CargoRoot[]>(content);
        if(cargoCompanies is null)
            return;
        foreach(var cargoCompany in cargoCompanies)
        {
            if(!await _dbContext.CargoCompanies.AnyAsync(x => x.Name == cargoCompany.name))
            {
                var entityCargoCompany = new CargoCompany
                {
                    Name = cargoCompany.name,
                    Code = cargoCompany.code,
                    TaxNumber = cargoCompany.taxNumber,
                    CreatedAt = DateTimeOffset.Now
                };
                _dbContext.CargoCompanies.Add(entityCargoCompany);
                var cargoCompanyMarketPlace = new CargoCompanyMarketPlaceMatch()
                {
                    MarketPlace = marketPlace,
                    ApplicationCargoCompany = entityCargoCompany,
                    MarketPlaceCargoCompanyId = cargoCompany.id
                };
                await _dbContext.CargoCompanyMarketPlaceMatches.AddAsync(cargoCompanyMarketPlace);
            }
        }
        await _dbContext.SaveChangesAsync();
    }
}


public class CargoRoot
{
    public int id { get; set; }
    public string name { get; set; }
    public string code { get; set; }
    public string taxNumber { get; set; }
}
