using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Mock Hepsiburada listing servisi — development ve test için.
/// </summary>
public sealed class MockHepsiburadaListingService(
    ILogger<MockHepsiburadaListingService> logger) : IHepsiburadaListingService
{
    public Task<IDataResult<HepsiburadaListingResponse>> GetListingsAsync(int offset = 0, int limit = 100)
    {
        logger.LogInformation("[MOCK] HB get listings: offset={Offset}, limit={Limit}", offset, limit);
        var response = new HepsiburadaListingResponse(
            Listings: new List<HepsiburadaListingItem>(),
            TotalCount: 0, Limit: limit, Offset: offset);
        return Task.FromResult<IDataResult<HepsiburadaListingResponse>>(
            new SuccessDataResult<HepsiburadaListingResponse>(response));
    }

    public Task<IResult> UpdatePricesAsync(List<HepsiburadaPriceUpdateItem> items)
    {
        logger.LogInformation("[MOCK] HB price update: {Count} items", items.Count);
        return Task.FromResult<IResult>(new SuccessResult($"{items.Count} fiyat güncellendi (MOCK)."));
    }

    public Task<IResult> UpdateStocksAsync(List<HepsiburadaStockUpdateItem> items)
    {
        logger.LogInformation("[MOCK] HB stock update: {Count} items", items.Count);
        return Task.FromResult<IResult>(new SuccessResult($"{items.Count} stok güncellendi (MOCK)."));
    }

    public Task<IResult> UpdateShippingInfoAsync(List<HepsiburadaShippingInfoUpdateItem> items)
    {
        logger.LogInformation("[MOCK] HB shipping info update: {Count} items", items.Count);
        return Task.FromResult<IResult>(new SuccessResult($"{items.Count} teslimat bilgisi güncellendi (MOCK)."));
    }

    public Task<IResult> ActivateListingAsync(string hepsiburadaSku, string merchantSku)
    {
        logger.LogInformation("[MOCK] HB listing activated: {Sku}", hepsiburadaSku);
        return Task.FromResult<IResult>(new SuccessResult("Listing satışa açıldı (MOCK)."));
    }

    public Task<IResult> DeactivateListingAsync(string hepsiburadaSku, string merchantSku)
    {
        logger.LogInformation("[MOCK] HB listing deactivated: {Sku}", hepsiburadaSku);
        return Task.FromResult<IResult>(new SuccessResult("Listing satıştan kapatıldı (MOCK)."));
    }

    public Task<IResult> SyncProductStockAndPriceAsync(Guid productId)
    {
        logger.LogInformation("[MOCK] HB sync product stock/price: {ProductId}", productId);
        return Task.FromResult<IResult>(new SuccessResult("Fiyat ve stok senkronize edildi (MOCK)."));
    }
}
