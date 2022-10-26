using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public sealed class EfBranchOfficeStockDal: EfEntityRepository<BranchOfficeStock,IntegrationDbContext>, IBranchOfficeStockDal
{
    private readonly IntegrationDbContext _context;
    public EfBranchOfficeStockDal(IntegrationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<BranchOfficeStock> stocks)
    {
        await _context.AddRangeAsync(stocks);
        await _context.SaveChangesAsync();
    }

    public async Task DecreaseStocksTransaction(IEnumerable<DecreaseStockDto> dto)
    {
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var decreaseStock in dto)
            {
                var item = await _context.BranchOfficeStocks.FirstOrDefaultAsync(x => x.ProductVariantId==decreaseStock.ProductId && x.BranchOfficeId == decreaseStock.OfficeId);
                if (item.CurrentStock < decreaseStock.StockNumber)
                    throw new Exception();
                item.SoldQuantity += decreaseStock.StockNumber;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            transaction.Rollback();
            
        }
    }

    public async Task UpdateRangeAsync(List<BranchOfficeStock> stocks)
    {
        _context.UpdateRange(stocks);
        await _context.SaveChangesAsync();
    }
}