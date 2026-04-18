// Application/Entegrasyon.MVC/Features/POS/CartDiscountDistributor.cs
namespace Entegrasyon.MVC.Features.POS;

public readonly record struct CartLine(
    decimal UnitPrice,
    int Quantity,
    decimal VatRate,
    decimal ExistingLineDiscountAmount);

public sealed record CartDiscountDistribution(
    decimal[] PerLineGrossShare,
    decimal[] PerLineNetShare,
    decimal AppliedGrossTotal);

public static class CartDiscountDistributor
{
    /// <summary>
    /// Sepet (genel) indirimini kalemlere pro-rata dağıtır.
    /// Girdi: her kalemin (kalem indirimi uygulanmış) gross payı baz alınır.
    /// Çıktı: her kalem için gross ve net indirim payı, uygulanan toplam gross.
    /// Yuvarlama kalanı son kaleme eklenir → toplam tam hedef tutara eşitlenir.
    /// </summary>
    public static CartDiscountDistribution Distribute(
        IReadOnlyList<CartLine> lines,
        decimal generalDiscountGross)
    {
        var count = lines.Count;
        var grossShares = new decimal[count];
        var netShares = new decimal[count];

        if (count == 0 || generalDiscountGross <= 0m)
            return new CartDiscountDistribution(grossShares, netShares, 0m);

        var lineGross = new decimal[count];
        for (int i = 0; i < count; i++)
        {
            var net = lines[i].UnitPrice * lines[i].Quantity - lines[i].ExistingLineDiscountAmount;
            if (net < 0m) net = 0m;
            lineGross[i] = Math.Round(net * (1m + lines[i].VatRate / 100m), 2);
        }

        var subtotalGross = lineGross.Sum();
        if (subtotalGross <= 0m)
            return new CartDiscountDistribution(grossShares, netShares, 0m);

        var applied = Math.Min(generalDiscountGross, subtotalGross);

        for (int i = 0; i < count; i++)
            grossShares[i] = Math.Round(lineGross[i] / subtotalGross * applied, 2);

        var distributed = grossShares.Sum();
        grossShares[count - 1] += (applied - distributed);

        for (int i = 0; i < count; i++)
        {
            if (grossShares[i] > 0m)
                netShares[i] = Math.Round(grossShares[i] / (1m + lines[i].VatRate / 100m), 2);
        }

        return new CartDiscountDistribution(grossShares, netShares, applied);
    }
}
