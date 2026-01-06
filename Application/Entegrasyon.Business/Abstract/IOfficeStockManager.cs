using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IOfficeStockManager
{
    Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks);
    Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds);
    Task UpdateStock(int branchOfficeId, Guid productVariantId, int stock);
    IResult CheckIfProductCountZero(params AddBranchOfficeStockDto[] stocks);
    Task<IResult> IncreaseProductStock();
    Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false);
    Task<IResult> DecreaseProductsStock(List<DecreaseStockDto> dtos, bool overrideStockStatus = false);
}
