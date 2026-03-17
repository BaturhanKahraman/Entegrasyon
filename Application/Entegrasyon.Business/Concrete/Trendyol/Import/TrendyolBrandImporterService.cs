using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.Trendyol.Import;

public class TrendyolBrandImporterService : ITrendyolBrandImporterService
{
    private const string BrandUrlSuffix = "brands";
    private readonly ILogger<TrendyolBrandImporterService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IBrandMatchService _brandMatchService;
    private readonly IApplicationLogManager _logService;
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private const int TrendyolId = 1;

    public TrendyolBrandImporterService(IHttpClientFactory httpClientFactory,IBrandMatchService brandMatchService,IApplicationLogManager logService,IDbContextFactory<IntegrationDbContext> contextFactory,ILogger<TrendyolBrandImporterService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
        _brandMatchService = brandMatchService;
        _logService = logService;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<IResult> QueueImporting()
    {
        await _logService.AddLog("Trendyoldan tum markalari iceri cekme istegi geldi.",LogType.Brand);
        return new SuccessResult(Messages.BrandsImportingQueued);
    }
    public async Task<IResult> ImportAll()
    {
        int page = -1;
        HashSet<BrandMarketPlaceMatch> brandMatchList = new();
        await _logService.AddLog("Trendyoldan tum markalari iceri cekme islemine baslandi.",LogType.Brand);

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var trendyolMarketPlace = await dbContext.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(x => string.Equals(x.Name, "Trendyol"));

        var existedEntities = await _brandMatchService.GetMarketPlaceBrandIdsByMarketPlaceId(trendyolMarketPlace?.Id ?? TrendyolId);
        while(true)
        {
            string fullUrl = BrandUrlSuffix + $"?page={page++}&size=1000";
            var response = await _httpClient.GetAsync(fullUrl);
            while(response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000);
                response = await _httpClient.GetAsync(fullUrl);
            }
            if(!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Brand importta gelen response hatali. {0}",JsonConvert.SerializeObject(response));
                return new ErrorResult(Messages.BrandsImportingInterruptedNull);
            }
            var trendyolBrandRoot = await response.Content.ReadFromJsonAsync<TrendyolBrandRoot>();
            var brands = trendyolBrandRoot!.Brands;
            if(brands.Count == 0)
                break;
            var brandMatches = brands.Where(x => !existedEntities.Contains(x.Id) && !x.Name.All(char.IsDigit)).AsParallel().Select(x => new BrandMarketPlaceMatch
            {
                MarketPlaceId = TrendyolId,
                ApplicationBrand = new Brand { Name = x.Name,CreatedAt = DateTimeOffset.Now },
                MarketPlaceBrandId = x.Id
            }).ToHashSet();
            brandMatchList.UnionWith(brandMatches);
        }
        await _brandMatchService.AddRange(brandMatchList.ToList());
        await _logService.AddLog("Trendyoldan tum markalar iceri cekildi.",LogType.Brand);
        return new SuccessResult(Messages.BrandsImportingSuccess);
    }

}
