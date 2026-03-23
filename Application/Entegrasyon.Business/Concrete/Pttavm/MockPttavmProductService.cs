using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM urun servisi — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmProductService(
    ILogger<MockPttavmProductService> logger) : IPttavmProductService
{
    public Task<IDataResult<PttavmUpsertResult>> UpsertProductsAsync(
        List<PttavmProductRequest> products, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM UpsertProducts: {Count} products", products.Count);
        var result = new PttavmUpsertResult(products.Count, Guid.NewGuid().ToString(), true, null);
        return Task.FromResult<IDataResult<PttavmUpsertResult>>(new SuccessDataResult<PttavmUpsertResult>(result));
    }

    public Task<IDataResult<PttavmTrackingResult>> GetTrackingResultAsync(
        string trackingId, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetTrackingResult: {TrackingId}", trackingId);
        var result = new PttavmTrackingResult(trackingId, "Completed", 1, DateTime.UtcNow, DateTime.UtcNow,
            new PttavmSubTrackingResult(1, 0, 0, 1, 0, new List<PttavmProductTrackingInfo>()));
        return Task.FromResult<IDataResult<PttavmTrackingResult>>(new SuccessDataResult<PttavmTrackingResult>(result));
    }

    public Task<IDataResult<PttavmProductInfo>> GetProductByBarcodeAsync(
        string barcode, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetProductByBarcode: {Barcode}", barcode);
        var result = new PttavmProductInfo(1, barcode, "Mock Product", 100, 80, 96, 20, true, true, 0, 1, 2, null, null);
        return Task.FromResult<IDataResult<PttavmProductInfo>>(new SuccessDataResult<PttavmProductInfo>(result));
    }

    public Task<IDataResult<List<PttavmProductInfo>>> GetProductsByBarcodesAsync(
        List<string> barcodes, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetProductsByBarcodes: {Count} barcodes", barcodes.Count);
        var result = barcodes.Select((b, i) =>
            new PttavmProductInfo(i + 1, b, $"Mock Product {i + 1}", 100, 80, 96, 20, true, true, 0, 1, 2, null, null))
            .ToList();
        return Task.FromResult<IDataResult<List<PttavmProductInfo>>>(new SuccessDataResult<List<PttavmProductInfo>>(result));
    }

    public Task<IResult> SetProductStatusAsync(
        int productId, bool isActive, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM SetProductStatus: {ProductId} -> {IsActive}", productId, isActive);
        return Task.FromResult<IResult>(new SuccessResult("Mock: Ürün durumu güncellendi."));
    }

    public Task<IDataResult<PttavmFaultyImagesResult>> GetFaultyImagesAsync(
        List<string>? barcodes, int page, int pageSize, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM GetFaultyImages: page={Page}, pageSize={PageSize}", page, pageSize);
        var result = new PttavmFaultyImagesResult(new List<PttavmFaultyImageProduct>(), true, null);
        return Task.FromResult<IDataResult<PttavmFaultyImagesResult>>(new SuccessDataResult<PttavmFaultyImagesResult>(result));
    }
}
