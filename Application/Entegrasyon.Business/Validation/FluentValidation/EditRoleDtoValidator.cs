using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditRoleDtoValidator:AbstractValidator<EditRoleDto>
{
    public EditRoleDtoValidator()
    {
        RuleFor(r => r.Id).NotEmpty().NotEqual(0).WithMessage(Messages.NotNullId);
        RuleFor(r => r.Name).NotEmpty().WithMessage(Messages.NoRoleName);
        RuleFor(r => r.Claims).NotEmpty().WithMessage(Messages.NoClaim);
    }
}
