using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmProductService
{
    Task<IDataResult<PttavmUpsertResult>> UpsertProductsAsync(List<PttavmProductRequest> products, CancellationToken ct = default);
    Task<IDataResult<PttavmTrackingResult>> GetTrackingResultAsync(string trackingId, CancellationToken ct = default);
    Task<IDataResult<PttavmProductInfo>> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<IDataResult<List<PttavmProductInfo>>> GetProductsByBarcodesAsync(List<string> barcodes, CancellationToken ct = default);
    Task<IResult> SetProductStatusAsync(int productId, bool isActive, CancellationToken ct = default);
    Task<IDataResult<PttavmFaultyImagesResult>> GetFaultyImagesAsync(List<string>? barcodes, int page, int pageSize, CancellationToken ct = default);
}
