using Entegrasyon.DataAccess.Abstract;
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

    public async Task<IResult> CheckIfOfficeExists(int[] officesIds)
    {
        if (await _branchOfficeManager.CheckIfOfficesExits(officesIds))
            return new SuccessResult();
        return new ErrorResult("Bir veya daha fazla ofis bulunamadı");
    }
    
}