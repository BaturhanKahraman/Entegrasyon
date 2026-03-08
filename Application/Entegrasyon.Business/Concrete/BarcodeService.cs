using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class BarcodeService(IntegrationDbContext dbContext) : IBarcodeService
{
    public async Task<string> GenerateAsync()
    {
        var result = await dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('barcode_sequence') AS \"Value\"")
            .FirstAsync();
        return result.ToString().PadLeft(13, '0');
    }
}
