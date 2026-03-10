using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class OfficeStockManager : IOfficeStockManager
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IBranchOfficeManager _branchOfficeManager;
    private readonly IProductVariantManager _productVariantManager;

    public OfficeStockManager(IntegrationDbContext dbContext, IBranchOfficeManager branchOfficeManager, IProductVariantManager productVariantManager)
    {
        _dbContext = dbContext;
        _branchOfficeManager = branchOfficeManager;
        _productVariantManager = productVariantManager;
    }

    public async Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks)
    {
        var enumeratedStocks = stocks.ToList();
        var result = LogicRunner.Run(await CheckIfOfficeExists(enumeratedStocks.Select(x => x.BranchOfficeId).ToArray()));
        if (result != null)
            return result;
        _dbContext.BranchOfficeStocks.AddRange(enumeratedStocks);
        await _dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds)
    {
        var result = await _branchOfficeManager.CheckIfOfficesExits(officesIds);
        return result ? new SuccessResult() : new ErrorResult("Bir veya daha fazla ofis bulunamadı");
    }

    public async Task UpdateStock(int branchOfficeId, Guid productVariantId, int stock)
    {
        var stockToUpdate = await _dbContext.BranchOfficeStocks.AsTracking()
            .FirstOrDefaultAsync(x => x.BranchOfficeId == branchOfficeId && x.ProductVariantId == productVariantId);
        stockToUpdate.FirstTotalStock = stock;
        await _dbContext.SaveChangesAsync();
    }

    public IResult CheckIfProductCountZero(params AddBranchOfficeStockDto[] stocks)
    {
        if (stocks == null)
            return new SuccessResult();
        var stocksList = stocks.ToList();
        if (stocksList.All(x => x.FirstTotalStock == 0))
            return new ErrorResult("Lütfen en az bir stok girin.");
        return new SuccessResult();
    }

    public Task<IResult> IncreaseProductStock()
    {
        throw new NotImplementedException();
    }

    public async Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false)
    {
        var productVariant = await _productVariantManager.GetById(id);
        if (productVariant == null)
            return new ErrorResult("İlettiğiniz ürün bulunamamıştır.");
        var stockStatus = await _dbContext.BranchOfficeStocks.AsTracking()
            .FirstOrDefaultAsync(b => b.ProductVariantId == id && b.BranchOfficeId == branchId);
        if (stockStatus == null)
            return new ErrorResult("İlettiğiniz ürünün bu ofis/depoda stoğu bulunamamıştır.");
        if (!overrideStockStatus && stockStatus.CurrentStock < stockNumber)
            return new ErrorResult("İlettiğiniz ofiste/depoda yeterli stok bulunmamaktadır.");
        stockStatus.SoldQuantity += stockNumber;
        await _dbContext.SaveChangesAsync();
        return new SuccessResult("Stok başarı ile düşmüştür.");
    }

    public async Task<IResult> DecreaseProductsStock(List<DecreaseStockDto> dtos, bool overrideStockStatus = false)
    {
        var officeIds = dtos.Select(x => x.OfficeId).ToList();
        var productIds = dtos.Select(x => x.ProductId).ToList();
        var dbStocks = await _dbContext.BranchOfficeStocks.AsTracking()
            .Where(s => officeIds.Contains(s.BranchOfficeId) && s.ProductVariantId.HasValue && productIds.Contains(s.ProductVariantId.Value))
            .ToListAsync();

        foreach (var dto in dtos)
        {
            var officeStock = dbStocks.First(s => dto.ProductId == s.ProductVariantId && s.FirstTotalStock > 0);
            if (!overrideStockStatus && officeStock.CurrentStock < dto.StockNumber)
                return new ErrorResult(dto.ProductId + " id li üründe stok yetersiz.");
            officeStock.SoldQuantity += dto.StockNumber;
        }
        await _dbContext.SaveChangesAsync();
        return new SuccessResult("Başarıyla stoktan düşüldü.");
    }
}
