using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed class MockPazaramaProductService(
    ILogger<MockPazaramaProductService> logger) : IPazaramaProductService
{
    private int _counter;

    public Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        var batchId = $"mock-pazarama-batch-{Interlocked.Increment(ref _counter)}";
        logger.LogInformation("MockPazarama: PublishProduct {ProductId} -> {BatchId}", productId, batchId);
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(batchId));
    }

    public Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        var response = new PazaramaBatchStatusResponse(2, batchRequestId, null, 1, 1, false, 0, null, null);
        return Task.FromResult<IDataResult<PazaramaBatchStatusResponse>>(
            new SuccessDataResult<PazaramaBatchStatusResponse>(response));
    }
}
