namespace Entegrasyon.Entity.Dtos.Product;

public sealed record AddBranchOfficeStockDto
{
    public int BranchOfficeId { get; init; }
    public int FirstTotalStock { get; init; }
}