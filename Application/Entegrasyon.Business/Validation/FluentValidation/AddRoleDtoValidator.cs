using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;
namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class AddRoleDtoValidator:AbstractValidator<AddRoleDto>
    {
        public AddRoleDtoValidator()
        {
            RuleFor(x=>x.Name).NotEmpty().WithMessage(Messages.NoRoleName);
            RuleFor(x => x.PermissionNames).NotEmpty().WithMessage(Messages.NoClaim);
        }
    }
}
