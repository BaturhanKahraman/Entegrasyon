using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.UnitTest.Business.ValidationRules
{
    public  class EditRoleDtoValidatorTests
    {
        private IValidator<EditRoleDto> _validator;

        public EditRoleDtoValidatorTests()
        {
            _validator = new EditRoleDtoValidator();
        }

        [Fact]
        public async Task Should_have_error_when_Id_is_empty()
        {
            // Arrange
            var dto = new EditRoleDto(default,default,default)
            {
                Id = 0,  // Geçersiz Id değeri
                Name = "Rol Adı",
                Claims = [1]
            };

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => string.Equals(x.ErrorMessage,Messages.NotNullId));
        }

        [Fact]
        public async Task Should_have_error_when_Name_is_empty()
        {
            // Arrange
            var dto = new EditRoleDto(default,default,default)
            {
                Id = 1,
                Name = "", // Geçersiz Name değeri
                Claims = [1]
            };

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Name" && x.ErrorMessage == Messages.NoRoleName);
        }

        [Fact]
        public async Task Should_have_error_when_Claims_is_empty()
        {
            // Arrange
            var dto = new EditRoleDto(default,default,default)
            {
                Id = 1,
                Name = "Rol Adı",
                Claims = [] // Geçersiz Claims değeri
            };

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Claims" && x.ErrorMessage == Messages.NoClaim);
        }

        [Fact]
        public async Task Should_not_have_error_when_dto_is_valid()
        {
            // Arrange
            var dto = new EditRoleDto(default,default,default)
            {
                Id = 1,
                Name = "Rol Adı",
                Claims = [1,2]
            };

            // Act
            var result =await _validator.TestValidateAsync(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
