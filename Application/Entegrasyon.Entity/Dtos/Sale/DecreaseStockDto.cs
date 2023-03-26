namespace Entegrasyon.Entity.Dtos.Sale;

public class DecreaseStockDto
{
    public DecreaseStockDto(Guid ProductId,int OfficeId,int StockNumber)
    {
        this.ProductId = ProductId;
        this.OfficeId = OfficeId;
        this.StockNumber = StockNumber;
    }

    public Guid ProductId { get; set; }
    public int OfficeId { get; set; }
    public int StockNumber { get; set; }
}
