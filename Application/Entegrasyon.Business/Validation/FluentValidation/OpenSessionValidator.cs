using Entegrasyon.Entity.Dtos.POS;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class OpenSessionValidator : AbstractValidator<OpenSessionDto>
{
    public OpenSessionValidator()
    {
        RuleFor(x => x.BranchOfficeId)
            .GreaterThan(0).WithMessage("Sube secimi zorunludur.");
        RuleFor(x => x.CashierId)
            .NotEmpty().WithMessage("Kasiyer bilgisi zorunludur.");
        RuleFor(x => x.OpeningCash)
            .GreaterThanOrEqualTo(0).WithMessage("Acilis kasasi sifir veya pozitif olmalidir.");
    }
}
