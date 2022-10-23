using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class OfficeStockManager
{
    private readonly IBranchOfficeStockDal _branchOfficeStockDal;
    private readonly BranchOfficeManager _branchOfficeManager;

    public OfficeStockManager(IBranchOfficeStockDal branchOfficeStockDal, BranchOfficeManager branchOfficeManager)
    {
        _branchOfficeStockDal = branchOfficeStockDal;
        _branchOfficeManager = branchOfficeManager;
    }

    public async Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks)
    {
        var enumeratedStocks = stocks.ToList();
        var result = LogicRunner.Run(await CheckIfOfficeExists(enumeratedStocks.Select(x => x.BranchOfficeId).ToArray()));
        if(result != null)
            return result;
        await _branchOfficeStockDal.AddRangeAsync(enumeratedStocks);
        return new SuccessResult();
    }

    public async Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds)
    {
        var result = await _branchOfficeManager.CheckIfOfficesExits(officesIds);
        if (result)
            return new SuccessResult();
        return new ErrorResult("Bir veya daha fazla ofis bulunamadı");
    }

    public async Task UpdateStock(int branchOfficeId,Guid productVariantId,int stock)
    {
        var stockToUpdate = await _branchOfficeStockDal.GetAsync(x => x.BranchOfficeId == branchOfficeId && x.ProductVariantId == productVariantId);
        stockToUpdate.FirstTotalStock = stock;
        await _branchOfficeStockDal.UpdateAsync(stockToUpdate);
    }

    public IResult CheckIfProductCountZero(params AddBranchOfficeStockDto[] stocks)
    {
        var stocksList = stocks.ToList();
        if(stocksList.Any(x=>x.FirstTotalStock==0))
            return new ErrorResult("Bir veya daha fazla ürün stokta bulunmamaktadır");
        return new SuccessResult();
    }
}