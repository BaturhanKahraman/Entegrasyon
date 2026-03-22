namespace Entegrasyon.Business.Utilities;

public static class KdvCalculator
{
    /// <summary>
    /// KDV dahil fiyattan KDV haric fiyat ve KDV tutari hesaplar.
    /// </summary>
    public static (decimal NetPrice, decimal KdvAmount) FromInclusive(decimal grossPrice, decimal vatRatePercent)
    {
        if (vatRatePercent <= 0 || grossPrice <= 0)
            return (grossPrice > 0 ? grossPrice : 0, 0);

        var netPrice = Math.Round(grossPrice / (1 + vatRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
        var kdvAmount = Math.Round(grossPrice - netPrice, 2, MidpointRounding.AwayFromZero);
        return (netPrice, kdvAmount);
    }

    /// <summary>
    /// KDV haric fiyattan KDV dahil fiyat ve KDV tutari hesaplar.
    /// </summary>
    public static (decimal GrossPrice, decimal KdvAmount) FromExclusive(decimal netPrice, decimal vatRatePercent)
    {
        if (vatRatePercent <= 0 || netPrice <= 0)
            return (netPrice > 0 ? netPrice : 0, 0);

        var kdvAmount = Math.Round(netPrice * vatRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var grossPrice = Math.Round(netPrice + kdvAmount, 2, MidpointRounding.AwayFromZero);
        return (grossPrice, kdvAmount);
    }
}
