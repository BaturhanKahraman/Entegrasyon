using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
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
}