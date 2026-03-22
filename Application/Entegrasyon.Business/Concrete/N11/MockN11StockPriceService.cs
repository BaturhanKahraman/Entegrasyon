using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

public sealed class MockN11StockPriceService(
    ILogger<MockN11StockPriceService> logger) : IN11StockPriceService
{
    public Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice)
    {
        logger.LogInformation("Mock: N11 UpdatePrice — ProductId={ProductId}, Price={Price}",
            productId, newPrice);
        return Task.FromResult<IResult>(
            new SuccessResult($"Fiyat güncellendi: {newPrice:C} (mock)."));
    }

    public Task<IResult> UpdateStockAsync(Guid productId, int quantity)
    {
        logger.LogInformation("Mock: N11 UpdateStock — ProductId={ProductId}, Qty={Quantity}",
            productId, quantity);
        return Task.FromResult<IResult>(
            new SuccessResult($"Stok güncellendi: {quantity} adet (mock)."));
    }
}
