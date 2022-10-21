using Shared.Helpers;

namespace Entegrasyon.Business.Concrete;

public class BarcodeManager
{
    private readonly IRandomGenerator _randomGenerator;
    private const string BarcodeCountryCode = "869";
    public BarcodeManager(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }

    public string GenerateBarcode()
    {
        
        return String.Empty;
    }
}