using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;
namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class AddRoleDtoValidator:AbstractValidator<AddRoleDto>
    {
        public AddRoleDtoValidator()
        {
            RuleFor(x=>x.Name).NotEmpty().WithMessage("Lütfen rol ismini boş bırakmayın.");
            RuleFor(x => x.Claims).NotNull().WithMessage("Lütfen yetki ekleyin.")
                .Must(x => x.Any()).When(x=>x!=null).WithMessage("Lütfen yetki ekleyin.");
        }
    }
}
