using System.Numerics;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Barcode;
using Microsoft.EntityFrameworkCore;
using Shared.Results;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class TempBarcodeManager : ITempBarcodeManager
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IProductVariantManager _productVariantManager;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public TempBarcodeManager(IntegrationDbContext dbContext, IProductVariantManager productVariantManager)
    {
        _dbContext = dbContext;
        _productVariantManager = productVariantManager;
    }

    public async Task<TempBarcode> GenerateBarcode()
    {
        var lastBarcode = await _dbContext.TempBarcodes
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Barcode)
            .FirstOrDefaultAsync()
            ?? await _productVariantManager.GetLastProductVariantBarcode();

        if (string.IsNullOrEmpty(lastBarcode))
        {
            var newGeneratedBarcode = new TempBarcode
            {
                Barcode = "1".PadLeft(13, '0'),
                IsAddable = false,
                IsAdded = false,
                ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            _dbContext.TempBarcodes.Add(newGeneratedBarcode);
            await _dbContext.SaveChangesAsync();
            return newGeneratedBarcode;
        }

        var barcodeNumber = BigInteger.Parse(lastBarcode);
        barcodeNumber++;
        var newBarcode = new TempBarcode
        {
            IsAdded = false,
            IsAddable = false,
            ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10),
            Barcode = barcodeNumber.ToString().PadLeft(13, '0')
        };
        _dbContext.TempBarcodes.Add(newBarcode);
        await _dbContext.SaveChangesAsync();
        return newBarcode;
    }

    public async Task<TempBarcode> GetBarcode()
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            TempBarcode tempBarcode = await _dbContext.TempBarcodes.AsTracking()
                .FirstOrDefaultAsync(x => x.IsAddable == true);
            if (tempBarcode == null)
            {
                tempBarcode = await GenerateBarcode();
                return tempBarcode;
            }
            tempBarcode.IsAddable = false;
            tempBarcode.ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10);
            await _dbContext.SaveChangesAsync();
            return tempBarcode;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task MarkAddedBarcodes(IEnumerable<string> addedBarcodes)
    {
        var barcodes = await _dbContext.TempBarcodes.AsTracking()
            .Where(x => addedBarcodes.Contains(x.Barcode))
            .ToListAsync();
        barcodes.ForEach(x => x.IsAdded = true);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearAddedBarcodes()
    {
        var barcodes = await _dbContext.TempBarcodes.AsTracking().Where(x => x.IsAdded == true).ToListAsync();
        var allAddedBarcodes = await _productVariantManager.GetAllVariantsBarcodes();
        var errorBarcodes = await _dbContext.TempBarcodes.AsTracking().Where(x => allAddedBarcodes.Contains(x.Barcode)).ToListAsync();
        var toRemove = errorBarcodes.Union(barcodes).ToList();
        _dbContext.TempBarcodes.RemoveRange(toRemove);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CorrectAddables()
    {
        var barcodes = await _dbContext.TempBarcodes.AsTracking()
            .Where(x => x.IsAdded == false && x.ValidUntil < DateTimeOffset.UtcNow)
            .ToListAsync();
        barcodes.ForEach(x =>
        {
            x.IsAddable = true;
            x.ValidUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IDataResult<TempBarcode>> GetBarcodeResult() => new SuccessDataResult<TempBarcode>(await GetBarcode());
}
