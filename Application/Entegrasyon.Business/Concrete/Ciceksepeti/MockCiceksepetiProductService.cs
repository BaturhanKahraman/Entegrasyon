using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti ürün servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; mock batch ID'ler döndürür.
/// </summary>
public sealed class MockCiceksepetiProductService(
    ILogger<MockCiceksepetiProductService> logger) : ICiceksepetiProductService
{
    public Task<IDataResult<string>> PublishProductAsync(Guid productId, CancellationToken ct = default)
    {
        var mockBatchId = $"mock-batch-{Guid.NewGuid():N}";
        logger.LogInformation("[MOCK] Çiçeksepeti publish: ProductId={ProductId}, BatchId={BatchId}", productId, mockBatchId);
        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(mockBatchId, "Ürün gönderildi (MOCK)."));
    }

    public Task<IDataResult<string>> UpdateProductAsync(Guid productId, CancellationToken ct = default)
    {
        var mockBatchId = $"mock-batch-{Guid.NewGuid():N}";
        logger.LogInformation("[MOCK] Çiçeksepeti update: ProductId={ProductId}, BatchId={BatchId}", productId, mockBatchId);
        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(mockBatchId, "Ürün güncellendi (MOCK)."));
    }

    public Task<IDataResult<CiceksepetiBatchStatusResponse>> CheckBatchStatusAsync(string batchId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti batch status: BatchId={BatchId}", batchId);
        var response = new CiceksepetiBatchStatusResponse(
            BatchId: batchId,
            ItemCount: 0,
            Items: []);
        return Task.FromResult<IDataResult<CiceksepetiBatchStatusResponse>>(
            new SuccessDataResult<CiceksepetiBatchStatusResponse>(response));
    }

    public Task<IDataResult<CiceksepetiProductListResponse>> GetProductsAsync(int page = 1, int pageSize = 60, int? statusFilter = null, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get products: Page={Page}, PageSize={PageSize}", page, pageSize);
        var response = new CiceksepetiProductListResponse(TotalCount: 0, Products: []);
        return Task.FromResult<IDataResult<CiceksepetiProductListResponse>>(
            new SuccessDataResult<CiceksepetiProductListResponse>(response));
    }
}
