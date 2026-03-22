using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

public sealed class MockN11ProductService(
    ILogger<MockN11ProductService> logger) : IN11ProductService
{
    private long _mockIdCounter = 1000;

    public Task<IDataResult<long>> SaveProductAsync(Guid productId)
    {
        var n11ProductId = ++_mockIdCounter;
        logger.LogInformation("Mock: N11 SaveProduct — ProductId={ProductId}, N11Id={N11Id}",
            productId, n11ProductId);
        return Task.FromResult<IDataResult<long>>(
            new SuccessDataResult<long>(n11ProductId, "Ürün N11'e kaydedildi (mock)."));
    }

    public Task<IResult> DeleteProductAsync(Guid productId)
    {
        logger.LogInformation("Mock: N11 DeleteProduct — ProductId={ProductId}", productId);
        return Task.FromResult<IResult>(
            new SuccessResult("Ürün N11'den silindi (mock)."));
    }

    public Task<IResult> UpdateProductBasicAsync(Guid productId)
    {
        logger.LogInformation("Mock: N11 UpdateProductBasic — ProductId={ProductId}", productId);
        return Task.FromResult<IResult>(new SuccessResult("Ürün güncellendi (mock)."));
    }

    public Task<IResult> StartSellingAsync(Guid productId)
    {
        logger.LogInformation("Mock: N11 StartSelling — ProductId={ProductId}", productId);
        return Task.FromResult<IResult>(new SuccessResult("Satış başlatıldı (mock)."));
    }

    public Task<IResult> StopSellingAsync(Guid productId)
    {
        logger.LogInformation("Mock: N11 StopSelling — ProductId={ProductId}", productId);
        return Task.FromResult<IResult>(new SuccessResult("Satış durduruldu (mock)."));
    }
}
