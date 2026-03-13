using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation.TestHelper;

namespace Entegrasyon.UnitTest.ValidationRules;

public class EditProductValidatorTests
{
    private readonly EditProductValidator _validator = new();

    private static EditProductDto BuildValid() => new(
        Id: Guid.NewGuid(),
        Title: "Test Ürün",
        Description: "Açıklama",
        StockCode: "TST-001",
        Season: "İlkbahar",
        Year: "2026",
        BrandId: 1,
        CategoryId: 1,
        Variants: [new EditProductVariantDto(Guid.NewGuid(), 100, 90, 70, 80, 0.5m, 20, "TRY")],
        AttributeKeyValues: [],
        DeletedImageIds: []
    );

    [Fact]
    public async Task Title_Empty_ShouldFail()
    {
        var dto = BuildValid() with { Title = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task BrandId_Zero_ShouldFail()
    {
        var dto = BuildValid() with { BrandId = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.BrandId);
    }

    [Fact]
    public async Task CategoryId_Zero_ShouldFail()
    {
        var dto = BuildValid() with { CategoryId = 0 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task StockCode_Empty_ShouldFail()
    {
        var dto = BuildValid() with { StockCode = "" };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.StockCode);
    }

    [Fact]
    public async Task Variant_NegativeListPrice_ShouldFail()
    {
        var dto = BuildValid() with
        {
            Variants = [new EditProductVariantDto(Guid.NewGuid(), -1, 90, 70, 80, 0.5m, 20, "TRY")]
        };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public async Task ValidDto_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(BuildValid());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
