using Entegrasyon.Entity.Dtos.Settings;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class UpdateApplicationSettingValidator : AbstractValidator<UpdateApplicationSettingDto>
{
    public UpdateApplicationSettingValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Value).NotNull().MaximumLength(2000);
    }
}
