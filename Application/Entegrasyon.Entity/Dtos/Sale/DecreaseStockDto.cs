namespace Entegrasyon.Entity.Dtos.Sale;

public record DecreaseStockDto(Guid ProductId,int OfficeId,int StockNumber)
{
    public DecreaseStockDto(Guid productKindId,int stockNumber) : this(productKindId, 0, stockNumber)
    {
    }
}
