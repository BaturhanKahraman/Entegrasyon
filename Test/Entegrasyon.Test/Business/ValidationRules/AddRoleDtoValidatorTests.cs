using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
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
        var dto = new AddRoleDto(default!, default!) { Name = "" };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoRoleName);
    }

    [Fact]
    public async Task Validate_WhenPermissionNamesIsNull_ShouldHaveErrorMessage()
    {
        // Arrange
        var dto = new AddRoleDto(default!, default!) { Name = "Admin", PermissionNames = null! };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoClaim);
    }

    [Fact]
    public async Task Validate_WhenPermissionNamesIsEmpty_ShouldHaveErrorMessage()
    {
        // Arrange
        var dto = new AddRoleDto(default!, default!) { Name = "Admin", PermissionNames = [] };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == Messages.NoClaim);
    }

    [Fact]
    public async Task Validate_WhenPermissionNamesIsNotNullAndNotEmpty_ShouldNotHaveError()
    {
        // Arrange
        var dto = new AddRoleDto(default!, default!) { Name = "Admin", PermissionNames = ["Products.Read"] };

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
