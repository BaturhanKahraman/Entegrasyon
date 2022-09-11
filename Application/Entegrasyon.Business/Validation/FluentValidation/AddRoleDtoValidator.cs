using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;
namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class AddRoleDtoValidator:AbstractValidator<AddRoleDto>
    {
        public AddRoleDtoValidator()
        {
            RuleFor(x=>x.RoleName).NotEmpty().WithMessage("Lütfen rol ismini boş bırakmayın.");
            RuleFor(x => x.RootClaims).NotNull().Must(x => x.Any()).WithMessage("Lütfen yetki ekleyin.");
        }
    }
}
