using System.Numerics;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Barcode;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class TempBarcodeManager
{
    private readonly ITempBarcodeDal _tempBarcodeDal;
    private readonly ProductVariantManager _productVariantManager;
    private readonly SemaphoreSlim _semaphoreSlim = new(1,1);
    public TempBarcodeManager(ITempBarcodeDal tempBarcodeDal,ProductVariantManager productVariantManager)
    {
        _tempBarcodeDal = tempBarcodeDal;
        _productVariantManager = productVariantManager;
    }


    public async Task<TempBarcode> GenerateBarcode()
    {
        var lastBarcode =
            await _tempBarcodeDal.GetLastBarcodeAsync() ??
            await _productVariantManager.GetLastProductVariantBarcode();
        if(string.IsNullOrEmpty(lastBarcode))
        {
            var newGeneratedBarcode = new TempBarcode
            {
                Barcode = "1".PadLeft(13,'0'),
                IsAddable = false,
                IsAdded = false,
                ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            await _tempBarcodeDal.AddAsync(newGeneratedBarcode);
            return newGeneratedBarcode;
        }

        var barcodeNumber = BigInteger.Parse(lastBarcode);
        barcodeNumber++;
        var newBarcode = new TempBarcode
        {
            IsAdded = false,
            IsAddable = false,
            ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10),
            Barcode = barcodeNumber.ToString().PadLeft(13,'0')
        };
        await _tempBarcodeDal.AddAsync(newBarcode);

        return newBarcode;
    }

    public async Task<TempBarcode> GetBarcode()
    {
        //tempbarcode tablosunda isaddable olan barkodu verip isaddable olan durumunu false'a çekecek
        // validaty time'ı güncelleyecek
        // eğer tabloda isAddable yoksa yeni bir tane oluşturacak ve onu verecek.
        await _semaphoreSlim.WaitAsync();
        try
        {
            TempBarcode tempBarcode = await _tempBarcodeDal.GetAsync(x => x.IsAddable == true);
            if(tempBarcode == null)
            {
                tempBarcode = await GenerateBarcode();
                return tempBarcode;
            }
            tempBarcode.IsAddable = false;
            tempBarcode.ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10);
            await _tempBarcodeDal.UpdateAsync(tempBarcode);
            return tempBarcode;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }



    public async Task MarkAddedBarcodes(IEnumerable<string> addedBarcodes)
    {
        var barcodes = await _tempBarcodeDal.GetAllAsync(x => addedBarcodes.Contains(x.Barcode),isTracking: true);
        barcodes.ForEach(x => x.IsAdded = true);
        await _tempBarcodeDal.UpdateRangeAsync(barcodes);
    }
    public async Task ClearAddedBarcodes()
    {
        var barcodes = await _tempBarcodeDal.GetAllAsync(x => x.IsAdded == true,true);
        var allAddedBarcodes = await _productVariantManager.GetAllVariantsBarcodes();
        var errorBarcodes = await _tempBarcodeDal.GetAllAsync(x => allAddedBarcodes.Contains(x.Barcode),true);
        var clearedBarcodeS = errorBarcodes.Union(barcodes);
        await _tempBarcodeDal.RemoveRangeAsync(clearedBarcodeS);
    }
    public async Task CorrectAddables()
    {
        var barcodes = await _tempBarcodeDal.GetAllAsync(x => x.IsAdded == false && x.ValidUntil < DateTimeOffset.UtcNow,true);
        barcodes.ForEach(x =>
        {
            x.IsAddable = true;
            x.ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        });
        await _tempBarcodeDal.UpdateRangeAsync(barcodes);
    }

    public async Task<IDataResult<TempBarcode>> GetBarcodeResult() => new SuccessDataResult<TempBarcode>(await GetBarcode());


}