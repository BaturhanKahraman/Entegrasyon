using Entegrasyon.Entity.Dtos.Branches;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class BranchOfficeAddDtoValidator : AbstractValidator<BranchOfficeAddDto>
{
    public BranchOfficeAddDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Şube adı boş geçilemez.")
            .MaximumLength(200).WithMessage("Şube adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Address));
    }
}
