using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmShippingService
{
    Task<IDataResult<List<PttavmWarehouse>>> GetWarehousesAsync(CancellationToken ct = default);
    Task<IDataResult<PttavmBarcodeCreateResult>> CreateBarcodesAsync(List<PttavmBarcodeRequest> orders, CancellationToken ct = default);
    Task<IDataResult<PttavmBarcodeStatusResult>> CheckBarcodeStatusAsync(string trackingId, CancellationToken ct = default);
    Task<IDataResult<string>> GetBarcodeTagAsync(string barcode, string orderId, string? type = null, CancellationToken ct = default);
    Task<IResult> UpdateNoShippingOrderAsync(string orderId, CancellationToken ct = default);
}
