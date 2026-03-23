using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti sipariş servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockCiceksepetiOrderService(
    ILogger<MockCiceksepetiOrderService> logger) : ICiceksepetiOrderService
{
    public Task<IDataResult<CiceksepetiOrderListResponse>> GetOrdersAsync(CiceksepetiGetOrdersRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get orders: Page={Page}", request.Page);
        var response = new CiceksepetiOrderListResponse(
            OrderListCount: 0,
            SupplierOrderListWithBranch: []);
        return Task.FromResult<IDataResult<CiceksepetiOrderListResponse>>(
            new SuccessDataResult<CiceksepetiOrderListResponse>(response));
    }

    public Task<IResult> ReadyForCargoWithCsAsync(CiceksepetiCsCargoRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti ready for cargo (CS): {Count} groups", request.CargoGroups.Count);
        return Task.FromResult<IResult>(new SuccessResult("Kargo hazır (MOCK)."));
    }

    public Task<IResult> UpdateStatusWithOwnCargoAsync(CiceksepetiOwnCargoRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti own cargo update: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("Kendi kargonuzla güncellendi (MOCK)."));
    }

    public Task<IResult> ChangeCargoCompanyAsync(CiceksepetiChangeCargoRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti change cargo: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("Kargo firması değiştirildi (MOCK)."));
    }

    public Task<IResult> SendCargoMeasurementAsync(CiceksepetiCargoMeasurementRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti cargo measurement: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("Kargo ölçüleri gönderildi (MOCK)."));
    }

    public Task<IResult> SendDigitalCodeAsync(CiceksepetiDigitalCodeRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti digital code: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("Dijital kodlar gönderildi (MOCK)."));
    }

    public Task<IResult> UpdateLaborCostAsync(CiceksepetiLaborCostRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti labor cost: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("İşçilik maliyeti güncellendi (MOCK)."));
    }
}
