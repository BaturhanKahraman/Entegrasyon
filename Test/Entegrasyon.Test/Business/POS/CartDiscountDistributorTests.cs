using Entegrasyon.MVC.Features.POS;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business.POS;

public class CartDiscountDistributorTests
{
    private static CartLine Line(decimal unitPrice, int qty, decimal vatRate, decimal existingDiscountAmount = 0)
        => new(UnitPrice: unitPrice, Quantity: qty, VatRate: vatRate, ExistingLineDiscountAmount: existingDiscountAmount);

    [Fact]
    public void Distribute_ReturnsZeroShares_WhenGeneralDiscountIsZero()
    {
        var lines = new[] { Line(100m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 0m);
        result.PerLineNetShare.Should().AllSatisfy(s => s.Should().Be(0m));
        result.AppliedGrossTotal.Should().Be(0m);
    }

    [Fact]
    public void Distribute_SingleLine_AllDiscountGoesToThatLine()
    {
        var lines = new[] { Line(1000m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 250m);
        result.AppliedGrossTotal.Should().Be(250m);
        result.PerLineNetShare[0].Should().Be(208.33m);
    }

    [Fact]
    public void Distribute_TwoEqualLines_DividesEvenly()
    {
        var lines = new[] { Line(100m, 1, 20m), Line(100m, 1, 20m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 40m);
        result.PerLineNetShare[0].Should().Be(16.67m);
        result.PerLineNetShare[1].Should().Be(16.67m);
    }

    [Fact]
    public void Distribute_RoundingRemainder_GoesToLastLine()
    {
        var lines = new[] { Line(100m, 1, 0m), Line(100m, 1, 0m), Line(100m, 1, 0m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 10m);
        result.PerLineGrossShare.Sum().Should().Be(10m);
        result.PerLineGrossShare[0].Should().Be(3.33m);
        result.PerLineGrossShare[1].Should().Be(3.33m);
        result.PerLineGrossShare[2].Should().Be(3.34m);
    }

    [Fact]
    public void Distribute_MixedVatRates_KeepsPerLineVatCorrect()
    {
        var lines = new[]
        {
            Line(833.3333m, 1, 20m),
            Line(495.0495m, 1, 1m)
        };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 250m);
        result.PerLineGrossShare.Sum().Should().Be(250m);
        result.PerLineGrossShare[0].Should().Be(166.67m);
        result.PerLineGrossShare[1].Should().Be(83.33m);
        result.PerLineNetShare[0].Should().Be(Math.Round(166.67m / 1.20m, 2));
        result.PerLineNetShare[1].Should().Be(Math.Round(83.33m / 1.01m, 2));
    }

    [Fact]
    public void Distribute_InvariantHolds_GrandTotalMatchesTarget()
    {
        var lines = new[] { Line(1000m, 1, 20m), Line(500m, 1, 10m) };
        var subtotalGross = 1000m * 1.20m + 500m * 1.10m;
        var target = 1500m;
        var discount = subtotalGross - target;

        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: discount);

        var finalGross = subtotalGross - result.AppliedGrossTotal;
        finalGross.Should().Be(target);
    }

    [Fact]
    public void Distribute_ClampsWhenDiscountExceedsSubtotal()
    {
        var lines = new[] { Line(100m, 1, 0m) };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 150m);
        result.AppliedGrossTotal.Should().Be(100m);
        result.PerLineGrossShare[0].Should().Be(100m);
    }

    [Fact]
    public void Distribute_WithExistingLineDiscount_UsesDiscountedGrossAsBase()
    {
        // 2 kalem, her biri 100 TL net, %20 KDV
        // 1. kaleme 10 TL kalem indirimi uygulanmış (yani net=90, gross=108)
        // 2. kalem tam gross (net=100, gross=120)
        // subtotalGross = 108 + 120 = 228
        // genel indirim 22.80 TL → oranlı dağıtım
        var lines = new[]
        {
            Line(100m, 1, 20m, existingDiscountAmount: 10m),  // gross payı 108
            Line(100m, 1, 20m, existingDiscountAmount: 0m)    // gross payı 120
        };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 22.80m);

        result.AppliedGrossTotal.Should().Be(22.80m);
        // 1. kalem: 108/228 * 22.80 = 10.80
        result.PerLineGrossShare[0].Should().Be(10.80m);
        // 2. kalem: 120/228 * 22.80 = 12.00
        result.PerLineGrossShare[1].Should().Be(12.00m);
        // Toplam dağıtılan gross = 22.80 (invariant)
        result.PerLineGrossShare.Sum().Should().Be(22.80m);
    }

    [Fact]
    public void Distribute_ThreeDifferentVatRates_HandlesRemainderCorrectly()
    {
        // Kasten remainder oluşturan senaryo
        var lines = new[]
        {
            Line(333.33m, 1, 20m),   // gross ≈ 400.00
            Line(100m, 1, 1m),       // gross = 101.00
            Line(200m, 1, 10m)       // gross = 220.00
        };
        var result = CartDiscountDistributor.Distribute(lines, generalDiscountGross: 50m);

        result.AppliedGrossTotal.Should().Be(50m);
        result.PerLineGrossShare.Sum().Should().Be(50m);  // remainder tam dağıtılmış
        // Her kalemin net payı kendi KDV oranı ile hesaplanmalı
        for (int i = 0; i < 3; i++)
        {
            var vatRate = lines[i].VatRate;
            var expectedNet = Math.Round(result.PerLineGrossShare[i] / (1m + vatRate / 100m), 2);
            result.PerLineNetShare[i].Should().Be(expectedNet);
        }
    }
}
