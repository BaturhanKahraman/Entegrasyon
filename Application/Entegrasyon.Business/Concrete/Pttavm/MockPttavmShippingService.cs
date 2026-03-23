using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM kargo servisi — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmShippingService(
    ILogger<MockPttavmShippingService> logger) : IPttavmShippingService
{
    public Task<IDataResult<List<PttavmWarehouse>>> GetWarehousesAsync(CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetWarehouses");
        var result = new List<PttavmWarehouse>
        {
            new(100301619, "Ana Depo", false, "", true)
        };
        return Task.FromResult<IDataResult<List<PttavmWarehouse>>>(new SuccessDataResult<List<PttavmWarehouse>>(result));
    }

    public Task<IDataResult<PttavmBarcodeCreateResult>> CreateBarcodesAsync(
        List<PttavmBarcodeRequest> orders, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM CreateBarcodes: {Count} orders", orders.Count);
        var result = new PttavmBarcodeCreateResult(Guid.NewGuid().ToString(), orders.Count, 200, true, "", false);
        return Task.FromResult<IDataResult<PttavmBarcodeCreateResult>>(new SuccessDataResult<PttavmBarcodeCreateResult>(result));
    }

    public Task<IDataResult<PttavmBarcodeStatusResult>> CheckBarcodeStatusAsync(
        string trackingId, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM CheckBarcodeStatus: {TrackingId}", trackingId);
        var result = new PttavmBarcodeStatusResult(trackingId, "completed",
            new List<PttavmBarcodeStatusData>
            {
                new("MOCK-ORD-001", new List<string> { "MOCK-BC-001" })
            }, "");
        return Task.FromResult<IDataResult<PttavmBarcodeStatusResult>>(new SuccessDataResult<PttavmBarcodeStatusResult>(result));
    }

    public Task<IDataResult<string>> GetBarcodeTagAsync(
        string barcode, string orderId, string? type = null, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetBarcodeTag: {Barcode}, {OrderId}", barcode, orderId);
        var tag = type == "zpl"
            ? "^XA^FO50,50^ADN,36,20^FDMock ZPL Label^FS^XZ"
            : "<html><body><h1>Mock Etiket</h1></body></html>";
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(tag));
    }

    public Task<IResult> UpdateNoShippingOrderAsync(string orderId, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM UpdateNoShippingOrder: {OrderId}", orderId);
        return Task.FromResult<IResult>(new SuccessResult("Mock: Kargosuz sipariş güncellendi."));
    }
}
