using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using FluentValidation.TestHelper;

namespace Entegrasyon.UnitTest.Business.ValidationRules;

public class AddRoleDtoValidatorTests
{
    private AddRoleDtoValidator validator;

    public AddRoleDtoValidatorTests()
    {
        validator = new AddRoleDtoValidator();
    }

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveErrorMessage()
    {
        // Arrange
        var dto = new AddRoleDto(default,default) { Name = "" };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoRoleName);
    }

    [Fact]
    public async Task Validate_WhenClaimsIsNull_ShouldHaveErrorMessage()
    {
        // Arrange
        var dto = new AddRoleDto(default,default) { Name = "Admin",Claims = null };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoClaim);
    }

    [Fact]
    public async Task Validate_WhenClaimsIsEmpty_ShouldHaveErrorMessage()
    {
        // Arrange
        var dto = new AddRoleDto(default,default) { Name = "Admin",Claims = [] };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoClaim);
    }

    [Fact]
    public async Task Validate_WhenClaimsIsNotNullAndNotEmpty_ShouldNotHaveError()
    {
        // Arrange
        var dto = new AddRoleDto(default,default) { Name = "Admin",Claims = [1] };

        // Act
        var result =await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
