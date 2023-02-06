using System.Linq.Expressions;
using Entegrasyon.Entity.Barcode;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ITempBarcodeDal: IEntityRepository<TempBarcode>
{
    Task UpdateRangeAsync(List<TempBarcode> barcodes);
    Task<string> GetLastBarcodeAsync(Expression<Func<TempBarcode, bool>> filter=null);
}