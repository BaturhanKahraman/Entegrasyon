using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class OfficeStockManager
{
    private readonly IBranchOfficeStockDal _branchOfficeStockDal;
    private readonly BranchOfficeManager _branchOfficeManager;
    private readonly ProductVariantManager _productVariantManager;

    public OfficeStockManager(IBranchOfficeStockDal branchOfficeStockDal, BranchOfficeManager branchOfficeManager, ProductVariantManager productVariantManager)
    {
        _branchOfficeStockDal = branchOfficeStockDal;
        _branchOfficeManager = branchOfficeManager;
        _productVariantManager = productVariantManager;
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

    public async Task<IResult> IncreaseProductStock()
    {
        return new SuccessResult();
    }
    public async Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false)
    {
        var productVariant = await _productVariantManager.GetById(id);
        if (productVariant == null)
            return new ErrorResult("İlettiğiniz ürün bulunamamıştır.");
        BranchOfficeStock stockStatus= await _branchOfficeStockDal.GetAsync(b=>b.ProductVariantId==id && b.BranchOfficeId == branchId);
        if (stockStatus==null)
            return new ErrorResult("İlettiğiniz ürünün bu ofis/depoda stoğu bulunamamıştır.");
        if (!overrideStockStatus) {
            if(stockStatus.CurrentStock < stockNumber)
                return new ErrorResult("İlettiğiniz ofiste/depoda yeterli stok bulunmamaktadır.");
        }
        stockStatus.SoldQuantity+=stockNumber;
        await _branchOfficeStockDal.UpdateAsync(stockStatus);
        return new SuccessResult("Stok başarı ile düşmüştür.");
    }
    public async Task<IResult> DecreaseProductsStock(IEnumerable<DecreaseStockDto> dtos, bool overrideStockStatus = false)
    {
        var stocksToDecrease = new List<BranchOfficeStock>();
        var dbStocks = await _branchOfficeStockDal.GetAllAsync(x => dtos.Any(z => z.ProductId == x.ProductVariantId && z.OfficeId == x.BranchOfficeId));
        foreach (var dto in dtos)
        {
            var officeStock = dbStocks.First(s => dto.ProductId ==s.ProductVariantId && dto.OfficeId == s.BranchOfficeId);
            if (officeStock.CurrentStock < dto.StockNumber)
                return new ErrorResult(dto.ProductId + " id li üründe stok yetersiz.");
            officeStock.SoldQuantity += dto.StockNumber;
            stocksToDecrease.Add(officeStock);
        }

        await _branchOfficeStockDal.UpdateRangeAsync(stocksToDecrease);
        return new SuccessResult("Başarıyla stoktan düşüldü.");
    }
}