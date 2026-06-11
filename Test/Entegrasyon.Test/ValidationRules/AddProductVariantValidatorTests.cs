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
    public async Task Should_Pass_WhenListPriceIsNull()
    {
        var variant = ValidVariant();
        variant.ListPrice = null;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenListPriceIsZero()
    {
        var variant = ValidVariant();
        variant.ListPrice = 0;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenListPriceIsNegative()
    {
        var variant = ValidVariant();
        variant.ListPrice = -1;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Liste fiyatı negatif olamaz.");
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
    public async Task Should_Fail_WhenSalePriceIsNull()
    {
        var variant = ValidVariant();
        variant.SalePrice = null;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış Fiyatı boş geçilemez");
    }

    [Fact]
    public async Task Should_Pass_WhenBranchOfficeStocksIsEmpty()
    {
        // Bug #3: Esnaf stoksuz ürün ekleyebilmeli (ön sipariş / yolda / tükenmiş).
        // Stok zorunluluğu kaldırıldı — boş stok listesi geçerli olmalı.
        var variant = ValidVariant();
        variant.BranchOfficeStocks = [];
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenAllFieldsAreValid()
    {
        var result = await _validator.ValidateAsync(ValidVariant());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenListPriceIsNull_AndSalePriceIsSet()
    {
        var variant = ValidVariant();
        variant.ListPrice = null;
        variant.SalePrice = 50;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenSalePriceExceedsListPrice()
    {
        var variant = ValidVariant();
        variant.ListPrice = 50;
        variant.SalePrice = 100;
        var result = await _validator.ValidateAsync(variant);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış fiyatı liste fiyatından büyük olamaz.");
    }
}
