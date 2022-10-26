namespace Entegrasyon.Entity.Dtos.Sale;

public record DecreaseStockDto(Guid ProductId,int OfficeId,int StockNumber)
{
    public DecreaseStockDto(Guid productId,int stockNumber) : this(productId, 0, stockNumber)
    {
    }
}
