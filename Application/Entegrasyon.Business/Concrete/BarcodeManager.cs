using Microsoft.Extensions.Caching.Distributed;
using Shared.Helpers;
using System.Numerics;

namespace Entegrasyon.Business.Concrete;

public class BarcodeManager
{
    private readonly ProductVariantManager _productVariantManager;
    private const string BarcodeCountryCode = "869";
    private static readonly SemaphoreSlim SemaphoreSlim = new (1,1);
    public BarcodeManager(ProductVariantManager productVariantManager)
    {
        _productVariantManager = productVariantManager;
    }

    public async Task<string> GenerateBarcode()
    {
        //distrubuted lock needs to be used.
        await SemaphoreSlim.WaitAsync();
        var lastBarcode = await _productVariantManager.GetLastProductVariantBarcode();
        SemaphoreSlim.Release();
        if(string.IsNullOrEmpty(lastBarcode))
            return "0000000000001";
        var barcodeNumber = BigInteger.Parse(lastBarcode);
        barcodeNumber++;
        string newBarcode = barcodeNumber.ToString().PadLeft(13, '0');
        return newBarcode;
    }
}