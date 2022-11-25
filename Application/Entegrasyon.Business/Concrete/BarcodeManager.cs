using Microsoft.Extensions.Caching.Distributed;
using Shared.Helpers;
using System.Numerics;

namespace Entegrasyon.Business.Concrete;

public class BarcodeManager
{
    private readonly ProductVariantManager _productVariantManager;
    private const string BarcodeCountryCode = "869";
    public BarcodeManager(ProductVariantManager productVariantManager)
    {
        _productVariantManager = productVariantManager;
    }

    public async Task<string> GenerateBarcode()
    {
        var pv = await _productVariantManager.GetLastProductVariantWithBarcode();
        if(pv == null)
            return "0000000000001";
        var barcodeNumber = BigInteger.Parse(pv.Barcode);
        barcodeNumber++;
        return barcodeNumber.ToString();
    }
}