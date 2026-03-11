using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.UnitTest.ValidationRules;

public class AddProductVariantValidatorTests
{
    private readonly AddProductVariantValidator _validator = new();

    private static AddProductVariantDto ValidVariant() => new()
    {
        ListPrice = 100,
        SalePrice = 90,
        BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 5 }]
    };

    [Fact]
    public async Task Should_Fail_WhenListPriceIsZero()
    {
        var variant = ValidVariant();
        variant.ListPrice = 0;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Liste fiyatı boş geçilemez");
    }

    [Fact]
    public async Task Should_Fail_WhenSalePriceIsZero()
    {
        var variant = ValidVariant();
        variant.SalePrice = 0;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış Fiyatı boş geçilemez");
    }

    [Fact]
    public async Task Should_Fail_WhenBranchOfficeStocksIsEmpty()
    {
        var variant = ValidVariant();
        variant.BranchOfficeStocks = [];

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Lütfen stok değerlerini girin.");
    }

    [Fact]
    public async Task Should_Pass_WhenAllFieldsAreValid()
    {
        var result = await _validator.ValidateAsync(ValidVariant());

        result.IsValid.Should().BeTrue();
    }
}
