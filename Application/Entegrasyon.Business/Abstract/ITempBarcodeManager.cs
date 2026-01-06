using Entegrasyon.Entity.Barcode;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface ITempBarcodeManager
{
    Task<TempBarcode> GenerateBarcode();
    Task<TempBarcode> GetBarcode();
    Task MarkAddedBarcodes(IEnumerable<string> addedBarcodes);
    Task ClearAddedBarcodes();
    Task CorrectAddables();
    Task<IDataResult<TempBarcode>> GetBarcodeResult();
}
