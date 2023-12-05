using System.Net;
using System.Net.Http.Json;
using AutoMapper;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Shared.Results;

namespace Entegrasyon.Business.Concrete.Trendyol.Import;

public class TrendyolBrandImporterService
{
    private const string UrlAddress = @"https://api.trendyol.com/sapigw/brands";
    private readonly HttpClient _httpClient;
    private readonly BrandMatchService _brandMatchService;
    private readonly RabbitMqPublisherService _brokerHelper;
    private readonly ApplicationLogManager _logService;
    private const int TrendyolId = 1;
    public TrendyolBrandImporterService(HttpClient httpClient, BrandMatchService brandMatchService, RabbitMqPublisherService brokerHelper, ApplicationLogManager logService)
    {
        _httpClient = httpClient;
        _brandMatchService = brandMatchService;
        _brokerHelper = brokerHelper;
        _logService = logService;
    }

    public async Task<IResult> QueueImporting()
    {
        _brokerHelper.PublishToQueue(MessageBrokerNames.TrendyolBrandImportQueueName, null);
        await _logService.AddLog("Trendyoldan tüm markaları içeri çekme isteği geldi.", LogType.Brand);
        return new SuccessResult(Messages.BrandsImportingQueued);
    }
    public async Task<IResult> ImportAll()
    {
        int page = -1;
        HashSet<BrandMarketPlaceMatch> brandList = new();
        await _logService.AddLog("Trendyoldan tüm markaları içeri çekme işlemine başlandı.", LogType.Brand);
        var existedEntities = await _brandMatchService.GetMarketPlaceBrandIdsByMarketPlaceId(TrendyolId);
        while (true)
        {
            string fullUrl = UrlAddress + $"?page={page++}&size=1000";
            var response = await _httpClient.GetAsync(fullUrl);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                while (response.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(1000);
                    response = await _httpClient.GetAsync(fullUrl);
                }
            }
            if (!response.IsSuccessStatusCode)
                return new ErrorResult(Messages.BrandsImportingInterruptedNull);
            var trendyolBrandRoot = await response.Content.ReadFromJsonAsync<TrendyolBrandRoot>();
            var brands = trendyolBrandRoot!.Brands;
            if (brands.Count == 0)
                break;
            var brandMatches = brands.Where(x => !existedEntities.Contains(x.Id) && !x.Name.All(char.IsDigit)).Select(x => new BrandMarketPlaceMatch
            {
                MarketPlaceId = TrendyolId,
                ApplicationBrand = new Brand { Name = x.Name, CreatedAt = DateTimeOffset.Now },
                MarketPlaceBrandId = x.Id
            }).ToHashSet();
            brandList.UnionWith(brandMatches);
        }
        await _brandMatchService.AddRange(brandList.ToList());
        await _logService.AddLog("Trendyoldan tüm markalar içeri çekildi.", LogType.Brand);
        return new SuccessResult(Messages.BrandsImportingSuccess);
    }

}