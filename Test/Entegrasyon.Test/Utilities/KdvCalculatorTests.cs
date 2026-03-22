using Entegrasyon.Business.Utilities;

namespace Entegrasyon.UnitTest.Utilities;

public class KdvCalculatorTests
{
    // FromInclusive tests
    [Theory]
    [InlineData(120, 20, 100, 20)]       // %20: 120 -> 100 + 20
    [InlineData(110, 10, 100, 10)]       // %10: 110 -> 100 + 10
    [InlineData(101, 1, 100, 1)]         // %1:  101 -> 100 + 1
    public void FromInclusive_StandardRates_ReturnsCorrectValues(
        decimal gross, decimal rate, decimal expectedNet, decimal expectedKdv)
    {
        var (net, kdv) = KdvCalculator.FromInclusive(gross, rate);
        net.Should().Be(expectedNet);
        kdv.Should().Be(expectedKdv);
    }

    [Fact]
    public void FromInclusive_ZeroRate_ReturnsGrossPriceAsNet()
    {
        var (net, kdv) = KdvCalculator.FromInclusive(100m, 0);
        net.Should().Be(100m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromInclusive_ZeroPrice_ReturnsZeros()
    {
        var (net, kdv) = KdvCalculator.FromInclusive(0m, 20);
        net.Should().Be(0m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromInclusive_NegativeRate_ReturnsGrossPriceAsNet()
    {
        var (net, kdv) = KdvCalculator.FromInclusive(100m, -5);
        net.Should().Be(100m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromInclusive_NegativePrice_ReturnsZeros()
    {
        var (net, kdv) = KdvCalculator.FromInclusive(-50m, 20);
        net.Should().Be(0m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromInclusive_Rounding_99_99_At20Percent()
    {
        var (net, kdv) = KdvCalculator.FromInclusive(99.99m, 20);
        net.Should().Be(83.33m);
        kdv.Should().Be(16.66m);
    }

    // FromExclusive tests
    [Theory]
    [InlineData(100, 20, 120, 20)]       // %20: 100 -> 120 (20 KDV)
    [InlineData(100, 10, 110, 10)]       // %10: 100 -> 110 (10 KDV)
    [InlineData(100, 1, 101, 1)]         // %1:  100 -> 101 (1 KDV)
    public void FromExclusive_StandardRates_ReturnsCorrectValues(
        decimal net, decimal rate, decimal expectedGross, decimal expectedKdv)
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(net, rate);
        gross.Should().Be(expectedGross);
        kdv.Should().Be(expectedKdv);
    }

    [Fact]
    public void FromExclusive_ZeroRate_ReturnsNetPriceAsGross()
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(100m, 0);
        gross.Should().Be(100m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromExclusive_ZeroPrice_ReturnsZeros()
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(0m, 20);
        gross.Should().Be(0m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromExclusive_NegativeRate_ReturnsNetPriceAsGross()
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(100m, -10);
        gross.Should().Be(100m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromExclusive_NegativePrice_ReturnsZeros()
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(-25m, 20);
        gross.Should().Be(0m);
        kdv.Should().Be(0m);
    }

    [Fact]
    public void FromExclusive_Rounding_83_33_At20Percent()
    {
        var (gross, kdv) = KdvCalculator.FromExclusive(83.33m, 20);
        gross.Should().Be(100.00m);
        kdv.Should().Be(16.67m);
    }

    // Roundtrip tests
    [Theory]
    [InlineData(100, 20)]
    [InlineData(250.50, 10)]
    [InlineData(99.99, 1)]
    public void Roundtrip_FromExclusiveThenFromInclusive_IsConsistent(decimal originalNet, decimal rate)
    {
        var (gross, _) = KdvCalculator.FromExclusive(originalNet, rate);
        var (recoveredNet, _) = KdvCalculator.FromInclusive(gross, rate);
        // Allow +/- 0.01 rounding tolerance
        recoveredNet.Should().BeApproximately(originalNet, 0.01m);
    }
}
