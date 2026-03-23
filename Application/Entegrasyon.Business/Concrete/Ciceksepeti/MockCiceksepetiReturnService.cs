using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti iade servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockCiceksepetiReturnService(
    ILogger<MockCiceksepetiReturnService> logger) : ICiceksepetiReturnService
{
    public Task<IDataResult<CiceksepetiReturnListResponse>> GetReturnOrdersAsync(CiceksepetiGetReturnsRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get return orders: Page={Page}", request.Page);
        var response = new CiceksepetiReturnListResponse(OrderItemList: []);
        return Task.FromResult<IDataResult<CiceksepetiReturnListResponse>>(
            new SuccessDataResult<CiceksepetiReturnListResponse>(response));
    }

    public Task<IResult> ConfirmReturnReceivedAsync(CiceksepetiReturnReceivedRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti confirm return: {Count} items", request.OrderItemIds.Count);
        return Task.FromResult<IResult>(new SuccessResult("İade teslim alındı (MOCK)."));
    }

    public Task<IResult> EvaluateReturnAsync(CiceksepetiReturnEvaluationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti evaluate return: OrderItemId={OrderItemId}", request.OrderItemId);
        return Task.FromResult<IResult>(new SuccessResult("İade değerlendirildi (MOCK)."));
    }
}
