using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti ürün mapper — development ve test için.
/// Gerçek mapping yapmaz; boş ürün listesiyle başarılı sonuç döndürür.
/// </summary>
public sealed class MockCiceksepetiProductMapper(
    ILogger<MockCiceksepetiProductMapper> logger) : ICiceksepetiProductMapper
{
    public Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToCreateRequestAsync(Guid productId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti map to create: ProductId={ProductId}", productId);
        var request = new CiceksepetiCreateProductsRequest(Products: []);
        return Task.FromResult<IDataResult<CiceksepetiCreateProductsRequest>>(
            new SuccessDataResult<CiceksepetiCreateProductsRequest>(request));
    }

    public Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToUpdateRequestAsync(Guid productId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti map to update: ProductId={ProductId}", productId);
        var request = new CiceksepetiCreateProductsRequest(Products: []);
        return Task.FromResult<IDataResult<CiceksepetiCreateProductsRequest>>(
            new SuccessDataResult<CiceksepetiCreateProductsRequest>(request));
    }
}
