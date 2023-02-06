using System.Linq;
using System.Linq.Expressions;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Barcode;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfTempBarcodeDal : EfEntityRepository<TempBarcode,IntegrationDbContext>, ITempBarcodeDal
{
    private readonly IntegrationDbContext _context;
    public EfTempBarcodeDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }


    public async Task UpdateRangeAsync(List<TempBarcode> barcodes)
    {
        _context.UpdateRange(barcodes);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetLastBarcodeAsync(Expression<Func<TempBarcode, bool>> filter)
    {
        var query = Table.OrderByDescending(x => x.Id);
        return filter == null
            ? (await query.FirstOrDefaultAsync())?.Barcode
            : (await query.FirstOrDefaultAsync(filter))?.Barcode;
    }
    
}