using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 stok/fiyat güncelleme servisi.
/// UpdateProductPriceByProductId + UpdateStockByStockId SOAP çağrıları.
/// </summary>
public interface IN11StockPriceService
{
    Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice);
    Task<IResult> UpdateStockAsync(Guid productId, int quantity);
}
