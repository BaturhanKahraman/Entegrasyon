using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.UnitTest.ValidationRules;

public class AddProductValidatorTests
{
    private readonly AddProductValidator _validator = new();

    private static AddProductDto ValidDto() => new()
    {
        Title = "Test Ürün",
        Description = "Test açıklama",
        StockCode = "TST-001",
        CategoryId = 1,
        ProductVariants =
        [
            new AddProductVariantDto
            {
                ListPrice = 100,
                SalePrice = 90,
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 5 }]
            }
        ]
    };

    [Fact]
    public async Task Should_Fail_WhenTitleIsEmpty()
    {
        var dto = ValidDto();
        dto.Title = string.Empty;

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ürün adı boş geçilemez");
    }

    [Fact]
    public async Task Should_Pass_WhenDescriptionIsEmpty()
    {
        var dto = ValidDto();
        dto.Description = string.Empty;

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenStockCodeIsEmpty()
    {
        var dto = ValidDto();
        dto.StockCode = string.Empty;

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenCategoryIdIsZero()
    {
        var dto = ValidDto();
        dto.CategoryId = 0;

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ürün kategorisi boş geçilemez");
    }

    [Fact]
    public async Task Should_Fail_WhenProductVariantsIsEmpty()
    {
        var dto = ValidDto();
        dto.ProductVariants = [];

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ürün varyantları boş geçilemez");
    }

    [Fact]
    public async Task Should_Pass_WhenAllFieldsAreValid()
    {
        var result = await _validator.ValidateAsync(ValidDto());

        result.IsValid.Should().BeTrue();
    }
}
