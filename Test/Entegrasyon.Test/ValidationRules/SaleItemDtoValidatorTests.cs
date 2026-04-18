using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.ValidationRules;

public class SaleItemDtoValidatorTests
{
    private readonly SaleItemDtoValidator _validator = new();

    private static SaleItemDto Base(
        double percent = 0, decimal? amount = null, int qty = 2, decimal unit = 100m) =>
        new(
            ProductVariantId: Guid.NewGuid(),
            TaxPercentage: 18,
            DiscountPercent: percent,
            UnitPrice: unit,
            Quantity: qty,
            DiscountVoucherCode: "",
            DiscountAmount: amount);

    [Fact]
    public void Valid_WithOnlyPercent()
    {
        var result = _validator.Validate(Base(percent: 15));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_WithOnlyAmount()
    {
        var result = _validator.Validate(Base(amount: 20m));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_WhenBothPercentAndAmountSet()
    {
        var result = _validator.Validate(Base(percent: 10, amount: 15m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("aynı anda uygulanamaz"));
    }

    [Fact]
    public void Invalid_WhenAmountExceedsLineTotal()
    {
        // line = 2 × 100 = 200, indirim 250 > 200
        var result = _validator.Validate(Base(amount: 250m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("satır toplamını aşamaz"));
    }

    [Fact]
    public void Invalid_WhenPercentOutOfRange()
    {
        var result = _validator.Validate(Base(percent: 150));
        result.IsValid.Should().BeFalse();
    }
}
