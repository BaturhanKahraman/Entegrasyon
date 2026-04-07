using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM Sipariş servisi — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmOrderService(
    ILogger<MockPttavmOrderService> logger) : IPttavmOrderService
{
    public Task<IDataResult<List<PttavmOrder>>> SearchOrdersAsync(
        DateTime startDate, DateTime endDate, bool isActiveOrders, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM SearchOrders: {Start} - {End}", startDate, endDate);
        var orders = new List<PttavmOrder>
        {
            new("MOCK-ORD-001", "kargo_yapilmasi_bekleniyor", 150m, 10m, DateTime.UtcNow,
                new List<PttavmOrderItem>
                {
                    new(1, "Mock Urun", "MOCK-BC-001", 1, 140m, 140m, 1001)
                }, "Ali", "Veli")
        };
        return Task.FromResult<IDataResult<List<PttavmOrder>>>(new SuccessDataResult<List<PttavmOrder>>(orders));
    }

    public Task<IDataResult<PttavmOrderDetail>> GetOrderDetailAsync(
        string orderId, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetOrderDetail: {OrderId}", orderId);
        var detail = new PttavmOrderDetail(orderId, "kargo_yapilmasi_bekleniyor", 150m, 10m, DateTime.UtcNow,
            new List<PttavmOrderItem>
            {
                new(1, "Mock Urun", "MOCK-BC-001", 1, 140m, 140m, 1001)
            }, "Ali", "Veli", "Fatura Adresi", "Teslimat Adresi");
        return Task.FromResult<IDataResult<PttavmOrderDetail>>(new SuccessDataResult<PttavmOrderDetail>(detail));
    }

    public Task<IDataResult<List<PttavmCargoInfo>>> GetCargoInfosAsync(
        string orderId, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetCargoInfos: {OrderId}", orderId);
        var infos = new List<PttavmCargoInfo>
        {
            new("P1", 1, null, "REF-001", "kargo_yapilmasi_bekleniyor", null)
        };
        return Task.FromResult<IDataResult<List<PttavmCargoInfo>>>(new SuccessDataResult<List<PttavmCargoInfo>>(infos));
    }

    public Task<IDataResult<List<PttavmCargoProfile>>> GetCargoProfilesAsync(
        CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetCargoProfiles");
        var profiles = new List<PttavmCargoProfile>
        {
            new(1, "PTT Kargo", "Standart gonderim", "birincil"),
            new(2, "PTT Hizli Kargo", "Hizli gonderim", "ikincil")
        };
        return Task.FromResult<IDataResult<List<PttavmCargoProfile>>>(new SuccessDataResult<List<PttavmCargoProfile>>(profiles));
    }
}
