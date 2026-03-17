using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IOfficeStockManager
{
    Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks);
    Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds);
    Task UpdateStock(int branchOfficeId, Guid productVariantId, int stock);
    IResult CheckIfProductCountZero(params AddBranchOfficeStockDto[] stocks);
    Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false);
    Task<IResult> DecreaseProductsStock(List<DecreaseStockDto> dtos, bool overrideStockStatus = false);

    /// <summary>
    /// Atomic stok düşme — stok yetersizse hata döner. POS satış vb. için.
    /// ExecuteUpdateAsync ile tek SQL, race condition imkansız.
    /// </summary>
    Task<IDataResult<StockMovement>> DecreaseStockAtomicAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null);

    /// <summary>
    /// Zorla stok düşme — stok yetersiz olsa bile düşer, negatife izin verir.
    /// Marketplace satışları için (Trendyol zaten sattıysa geri alınamaz).
    /// </summary>
    Task<IDataResult<StockMovement>> ForceDecreaseStockAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null);
}
