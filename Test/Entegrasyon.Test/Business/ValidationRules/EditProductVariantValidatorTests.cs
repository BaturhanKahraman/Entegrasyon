using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation.TestHelper;

namespace Entegrasyon.UnitTest.Business.ValidationRules;

public class EditProductVariantValidatorTests
{
    private readonly EditProductVariantValidator _validator = new();

    private static EditProductVariantDto ValidDto() => new(
        Id: Guid.NewGuid(),
        ListPrice: 100m,
        SalePrice: 80m,
        CostPrice: 50m,
        ECommercePrice: 85m,
        DimensionalWeight: 1.5m,
        VatRate: 18m,
        CurrencyType: "TRY"
    );

    [Fact]
    public async Task Validate_WhenValid_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(ValidDto());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenSalePriceGreaterThanListPrice_ShouldFail()
    {
        var dto = ValidDto() with { SalePrice = 200m, ListPrice = 100m };
        var result = await _validator.TestValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.SalePrice);
    }

    [Fact]
    public async Task Validate_WhenListPriceIsZero_ShouldPass()
    {
        var dto = ValidDto() with { ListPrice = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ListPrice);
    }

    [Fact]
    public async Task Validate_WhenListPriceIsNegative_ShouldFail()
    {
        var dto = ValidDto() with { ListPrice = -1 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ListPrice);
    }

    [Fact]
    public async Task Validate_WhenVatRateExceeds100_ShouldFail()
    {
        var dto = ValidDto() with { VatRate = 101 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.VatRate);
    }

    [Fact]
    public async Task Validate_WhenDimensionalWeightNegative_ShouldFail()
    {
        var dto = ValidDto() with { DimensionalWeight = -1 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.DimensionalWeight);
    }
}
