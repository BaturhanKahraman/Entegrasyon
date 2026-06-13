using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Entegrasyon.UnitTest.Business.ValidationRules;

public class AddCategoryAttributeDtoValidatorTests
{
    private readonly IValidator<AddCategoryAttributeDto> _validator = new AddCategoryAttributeDtoValidator();

    private static AddCategoryAttributeDto ValidDto() => new(
        id: 0,
        isRequired: false,
        isVarianter: false,
        categoryAttributeKey: "Renk",
        isSlicer: false,
        categoryAttributeHumanized: "Renk",
        categoryAttributeValues: [new CategoryAttributeValue { Name = "Sarı" }]);

    [Fact]
    public async Task Should_pass_when_dto_is_valid()
    {
        var result = await _validator.TestValidateAsync(ValidDto());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_have_error_when_Key_is_empty(string key)
    {
        var dto = ValidDto() with { CategoryAttributeKey = key };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoryAttributeKey);
    }

    [Fact]
    public async Task Should_have_error_when_Key_exceeds_255_chars()
    {
        var dto = ValidDto() with { CategoryAttributeKey = new string('a', 256) };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoryAttributeKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_have_error_when_Humanized_is_empty(string humanized)
    {
        var dto = ValidDto() with { CategoryAttributeHumanized = humanized };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoryAttributeHumanized);
    }

    [Fact]
    public async Task Should_have_error_when_Humanized_exceeds_255_chars()
    {
        var dto = ValidDto() with { CategoryAttributeHumanized = new string('a', 256) };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoryAttributeHumanized);
    }

    [Fact]
    public async Task Should_have_error_when_Values_is_empty()
    {
        var dto = ValidDto() with { CategoryAttributeValues = [] };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoryAttributeValues);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_have_error_when_a_value_name_is_empty(string name)
    {
        var dto = ValidDto() with
        {
            CategoryAttributeValues = [new CategoryAttributeValue { Name = name }]
        };

        var result = await _validator.TestValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Should_have_error_when_a_value_name_exceeds_max_length()
    {
        var dto = ValidDto() with
        {
            CategoryAttributeValues =
                [new CategoryAttributeValue { Name = new string('a', CategoryAttributeValue.MaxNameLength + 1) }]
        };

        var result = await _validator.TestValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }
}
